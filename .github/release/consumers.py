"""The clean projects in .github/release/consumers that restore a release the way an integrator does, run it on each
target framework, and, for the core SDK, check that the data it sends arrives at a server."""

import base64
import gzip
import hashlib
import http.server
import json
import re
import threading
import urllib.parse
from pathlib import Path
from xml.sax.saxutils import quoteattr

RELEASE_SOURCE = "countly-release"
DEPENDENCY_SOURCE = "nuget.org"
PROBE_EVENT = "release_probe"


def framework_key(alias):
    """The key project.assets.json files a target framework under: the full name for .NET Framework, such as
    .NETFramework,Version=v4.6.2 for net462, the alias with its default platform version for a -windows framework, and
    the alias itself otherwise."""
    framework = re.fullmatch(r"net(\d)(\d)(\d?)", alias)
    if framework:
        return f".NETFramework,Version=v{framework.group(1)}.{framework.group(2)}" + (f".{framework.group(3)}" if framework.group(3) else "")
    return f"{alias}7.0" if alias.endswith("-windows") else alias


def nuget_config(release_source, dependency_source, package_id, packages_folder):
    """A NuGet.config that takes the released package only from the release source and everything else, such as its
    dependencies or the core SDK of a UI release, from the dependency source, into a packages folder of its own."""
    sources = [(RELEASE_SOURCE, release_source)]
    mapping = [(RELEASE_SOURCE, package_id)]
    if dependency_source != release_source:
        sources.append((DEPENDENCY_SOURCE, dependency_source))
        mapping.append((DEPENDENCY_SOURCE, "*"))
    else:
        mapping.append((RELEASE_SOURCE, "*"))
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<configuration>", "  <packageSources>", "    <clear />"]
    lines += [f"    <add key={quoteattr(key)} value={quoteattr(str(value))} />" for key, value in sources]
    lines += ["  </packageSources>", "  <packageSourceMapping>"]
    for key, _ in sources:
        patterns = [pattern for source, pattern in mapping if source == key]
        lines.append(f"    <packageSource key={quoteattr(key)}>")
        lines += [f"      <package pattern={quoteattr(pattern)} />" for pattern in patterns]
        lines.append("    </packageSource>")
    lines += ["  </packageSourceMapping>", "  <config>", f"    <add key=\"globalPackagesFolder\" value={quoteattr(str(packages_folder))} />", "  </config>", "</configuration>", ""]
    return "\n".join(lines)


def build_command(project, version):
    """Restores and builds a consumer project against one package version."""
    return ["dotnet", "build", str(project), "-c", "Release", "--nologo", f"-p:CountlyVersion={version}"]


def build_environment(base, frameworks, packages_folder, http_cache):
    """The environment of a consumer build: the target frameworks to build, which MSBuild reads as a property, and NuGet
    folders of its own so no package or feed answer cached by an earlier restore is reused."""
    return dict(base, ConsumerFrameworks=";".join(frameworks), NUGET_PACKAGES=str(packages_folder), NUGET_HTTP_CACHE_PATH=str(http_cache),
                DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")


def runnable(alias, windows):
    """Whether a consumer built for the framework can run here: every framework on Windows, and elsewhere only .NET
    5 or later without a Windows-only platform."""
    return windows or ("." in alias and not alias.endswith("-windows"))


def probe_command(project, alias):
    """Runs the probe a consumer project built for one framework: the .exe for .NET Framework, the .dll through dotnet
    for .NET."""
    output = Path(project).parent / "bin" / "Release" / alias / Path(project).stem
    if re.fullmatch(r"net\d{2,3}", alias):
        return [str(output) + ".exe"]
    return ["dotnet", str(output) + ".dll"]


def asset_problems(assets, package_id, version, expectations):
    """Problems unless, for each framework of the contract's consumers, project.assets.json resolved the package version
    and compiles and runs the expected library of it."""
    problems = []
    targets = assets.get("targets", {})
    for alias, expected in sorted(expectations.items()):
        libraries = targets.get(framework_key(alias))
        if libraries is None:
            problems.append(f"{alias}: the consumer was not restored for it")
            continue
        entry = next((value for key, value in libraries.items() if key.lower() == f"{package_id}/{version}".lower()), None)
        if entry is None:
            problems.append(f"{alias}: {package_id} {version} was not resolved")
            continue
        for kind in ("compile", "runtime"):
            found = sorted(entry.get(kind, {}))
            if found != [expected["lib"]]:
                problems.append(f"{alias}: {kind} assets are {found}, the contract says {[expected['lib']]}")
    return problems


def source_problems(packages_folder, package_id, version, expected_source, staged_file=None):
    """Problems unless NuGet took the package from the expected source, as its .nupkg.metadata records, and, for a
    staged package, unless it restored exactly the staged file."""
    folder = Path(packages_folder) / package_id.lower() / version.lower()
    record = folder / ".nupkg.metadata"
    if not record.is_file():
        return [f"{package_id} {version} was not restored into {packages_folder}"]
    metadata = json.loads(record.read_text(encoding="utf-8"))
    problems = []
    if str(metadata.get("source", "")).rstrip("/\\") != str(expected_source).rstrip("/\\"):
        problems.append(f"{package_id} {version} came from {metadata.get('source')}, not from {expected_source}")
    if staged_file is not None:
        digest = base64.b64encode(hashlib.sha512(Path(staged_file).read_bytes()).digest()).decode("ascii")
        if metadata.get("contentHash") != digest:
            problems.append(f"{package_id} {version} restored is not the staged {Path(staged_file).name}")
    return problems


def parse_probe(output):
    """The key=value lines a probe printed, as a dict."""
    values = {}
    for line in output.splitlines():
        key, separator, value = line.strip().partition("=")
        if separator:
            values[key] = value
    return values


def assembly_problems(label, full_name, assembly_version, token):
    """Problems unless an assembly's full name carries the contract's assembly version and the Countly public key token."""
    problems = []
    if f"Version={assembly_version}," not in (full_name or ""):
        problems.append(f"{label} is {full_name}, the contract's assembly version is {assembly_version}")
    if f"PublicKeyToken={token}" not in (full_name or ""):
        problems.append(f"{label} is {full_name}, not signed with the Countly key token {token}")
    return problems


def probe_problems(values, plan, expectation, contract, token):
    """Problems with what a probe loaded: the assembly identity, file version and, for the core SDK, the SDK name and
    the version it reports to the server; for the UI, the core SDK it bound to."""
    problems = assembly_problems("the loaded assembly", values.get("assembly"), contract["assemblyVersion"], token)
    if values.get("fileVersion") != plan.file_version:
        problems.append(f"file version is {values.get('fileVersion')}, the release is {plan.file_version}")
    if "sdkName" in expectation:
        if values.get("sdkName") != expectation["sdkName"]:
            problems.append(f"SDK name is {values.get('sdkName')}, the contract says {expectation['sdkName']}")
        if values.get("sdkVersion") != plan.version:
            problems.append(f"SDK version is {values.get('sdkVersion')}, the release is {plan.version}")
    if "core" in values and f"PublicKeyToken={token}" not in values["core"]:
        problems.append(f"bound to the core SDK {values['core']}, not signed with the Countly key token {token}")
    return problems


def request_parameters(path, body, encoding):
    """The parameters of a request: its query string and its form body, gunzipped when the SDK compressed it."""
    if encoding == "gzip" and body:
        body = gzip.decompress(body)
    parameters = dict(urllib.parse.parse_qsl(urllib.parse.urlsplit(path).query))
    parameters.update(urllib.parse.parse_qsl(body.decode("utf-8", "replace")))
    return parameters


def delivery_problems(requests, app_key, plan, expectation):
    """Problems unless the server received, for the probe's app key, a session start and the probe's event, each
    reporting the release's SDK version and the flavor's SDK name."""
    ours = [request for request in requests if request.get("app_key") == app_key]
    if not ours:
        return [f"no request with the probe's app key arrived ({len(requests)} other requests)"]
    problems = []
    for request in ours:
        if request.get("sdk_version") != plan.version or request.get("sdk_name") != expectation["sdkName"]:
            problems.append(f"a request reported SDK {request.get('sdk_name')} {request.get('sdk_version')}, expected {expectation['sdkName']} {plan.version}")
            break
    if not any(request.get("begin_session") == "1" for request in ours):
        problems.append("no session start arrived")
    if not any(PROBE_EVENT in request.get("events", "") for request in ours):
        problems.append(f"the {PROBE_EVENT} event did not arrive")
    return problems


class RecordingServer:
    """A local HTTP server that answers every request the way a Countly server accepting it does, and keeps the
    parameters of each one."""

    def __init__(self):
        """Binds a free local port; requests are answered once the server is entered."""
        self.requests = []
        recorder = self

        class Handler(http.server.BaseHTTPRequestHandler):
            """Records each request and answers {"result":"Success"}."""

            def answer(self):
                """Reads and records the request, then sends the success answer."""
                length = int(self.headers.get("Content-Length") or 0)
                body = self.rfile.read(length) if length else b""
                recorder.requests.append(request_parameters(self.path, body, self.headers.get("Content-Encoding")))
                reply = b'{"result":"Success"}'
                self.send_response(200)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(reply)))
                self.end_headers()
                self.wfile.write(reply)

            do_GET = answer
            do_POST = answer

            def log_message(self, *args):
                """Keeps the job log free of a line per request."""

        self.server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)

    @property
    def url(self):
        """The address the probe sends to."""
        return f"http://127.0.0.1:{self.server.server_address[1]}"

    def __enter__(self):
        """Starts answering requests."""
        self.thread.start()
        return self

    def __exit__(self, *exc):
        """Stops the server and frees its port."""
        self.server.shutdown()
        self.server.server_close()
