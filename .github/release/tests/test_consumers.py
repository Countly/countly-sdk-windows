import base64
import gzip
import hashlib
import json
import tempfile
import unittest
import urllib.request
import xml.etree.ElementTree as ElementTree
from pathlib import Path

import consumers
from plan import plan_for_tag
from tests.support import repository_config

TOKEN = "30fd2b3d8bfe4882"
CORE = {"assemblyVersion": "26.1.0.0"}
NET35 = {"lib": "lib/net35-full/Countly.dll", "sdkName": "csharp-net35"}


def assets(libraries_by_target):
    """A project.assets.json with the given Countly library entries per target key."""
    return {"targets": {target: {name: {"compile": {lib: {}}, "runtime": {lib: {}}} for name, lib in libraries.items()} for target, libraries in libraries_by_target.items()}}


class ConsumersTest(unittest.TestCase):
    """The clean projects: their NuGet setup, the libraries they resolve, what the probe loads and the data the core sends."""

    def setUp(self):
        """A plan for 26.9.0."""
        self.plan = plan_for_tag(repository_config(), "26.9.0")

    def test_frameworks_map_to_the_keys_of_project_assets(self):
        """Frameworks map to the keys of project assets."""
        self.assertEqual([consumers.framework_key(alias) for alias in ("net35", "net462", "net481", "net8.0", "net8.0-windows")],
                         [".NETFramework,Version=v3.5", ".NETFramework,Version=v4.6.2", ".NETFramework,Version=v4.8.1", "net8.0", "net8.0-windows7.0"])

    def test_the_release_package_comes_only_from_the_release_source(self):
        """The release package comes only from the release source."""
        config = ElementTree.fromstring(consumers.nuget_config("/staged", "https://api.nuget.org/v3/index.json", "Countly.UI.WebView2", "/packages"))
        mapping = {source.get("key"): [package.get("pattern") for package in source] for source in config.find("packageSourceMapping")}
        self.assertEqual(mapping, {"countly-release": ["Countly.UI.WebView2"], "nuget.org": ["*"]})
        self.assertEqual([child.tag for child in config.find("packageSources")], ["clear", "add", "add"])
        single = ElementTree.fromstring(consumers.nuget_config("https://api.nuget.org/v3/index.json", "https://api.nuget.org/v3/index.json", "Countly", "/p"))
        self.assertEqual([package.get("pattern") for package in single.find("packageSourceMapping")[0]], ["Countly", "*"])

    def test_each_framework_must_resolve_the_library_the_contract_names(self):
        """Each framework must resolve the library the contract names."""
        found = assets({".NETFramework,Version=v3.5": {"Countly/26.9.0": "lib/net35-full/Countly.dll"},
                        ".NETFramework,Version=v4.6.2": {"Countly/26.9.0": "lib/netstandard2.0/Countly.dll"}})
        expectations = {"net35": NET35, "net462": {"lib": "lib/net45/Countly.dll"}, "net8.0": {"lib": "lib/netstandard2.0/Countly.dll"}}
        self.assertEqual(consumers.asset_problems(found, "Countly", "26.9.0", expectations), [
            "net462: compile assets are ['lib/netstandard2.0/Countly.dll'], the contract says ['lib/net45/Countly.dll']",
            "net462: runtime assets are ['lib/netstandard2.0/Countly.dll'], the contract says ['lib/net45/Countly.dll']",
            "net8.0: the consumer was not restored for it",
        ])
        self.assertEqual(consumers.asset_problems(found, "Countly", "26.8.0", {"net35": NET35}), ["net35: Countly 26.8.0 was not resolved"])

    def test_the_restored_package_must_come_from_the_release_source_and_be_the_staged_file(self):
        """The restored package must come from the release source and be the staged file."""
        folder = Path(tempfile.mkdtemp())
        staged = folder / "Countly.26.9.0.nupkg"
        staged.write_bytes(b"staged package")
        record = folder / "packages" / "countly" / "26.9.0" / ".nupkg.metadata"
        record.parent.mkdir(parents=True)
        digest = base64.b64encode(hashlib.sha512(b"staged package").digest()).decode("ascii")
        record.write_text(json.dumps({"version": 2, "contentHash": digest, "source": str(folder) + "/"}), encoding="utf-8")
        self.assertEqual(consumers.source_problems(folder / "packages", "Countly", "26.9.0", str(folder), staged), [])
        record.write_text(json.dumps({"version": 2, "contentHash": "other", "source": "https://api.nuget.org/v3/index.json"}), encoding="utf-8")
        self.assertEqual(consumers.source_problems(folder / "packages", "Countly", "26.9.0", str(folder), staged), [
            f"Countly 26.9.0 came from https://api.nuget.org/v3/index.json, not from {folder}",
            "Countly 26.9.0 restored is not the staged Countly.26.9.0.nupkg",
        ])
        self.assertEqual(consumers.source_problems(folder / "none", "Countly", "26.9.0", str(folder)), [f"Countly 26.9.0 was not restored into {folder / 'none'}"])

    def test_the_probe_must_load_the_countly_identity_the_release_versions_and_the_flavor(self):
        """The probe must load the countly identity the release versions and the flavor."""
        values = consumers.parse_probe(f"assembly=Countly, Version=26.1.0.0, Culture=neutral, PublicKeyToken={TOKEN}\nfileVersion=26.9.0.0\nsdkVersion=26.9.0\nsdkName=csharp-net35\nnoise\n")
        self.assertEqual(consumers.probe_problems(values, self.plan, NET35, CORE, TOKEN), [])
        wrong = dict(values, assembly="Countly, Version=26.9.0.0, Culture=neutral, PublicKeyToken=null", sdkName="csharp-netstd", sdkVersion="26.8.0")
        self.assertEqual(consumers.probe_problems(wrong, self.plan, NET35, CORE, TOKEN), [
            "the loaded assembly is Countly, Version=26.9.0.0, Culture=neutral, PublicKeyToken=null, the contract's assembly version is 26.1.0.0",
            "the loaded assembly is Countly, Version=26.9.0.0, Culture=neutral, PublicKeyToken=null, not signed with the Countly key token 30fd2b3d8bfe4882",
            "SDK name is csharp-netstd, the contract says csharp-net35",
            "SDK version is 26.8.0, the release is 26.9.0",
        ])

    def test_the_ui_probe_must_bind_to_a_signed_core(self):
        """The ui probe must bind to a signed core."""
        values = {"assembly": f"Countly.UI.WebView2, Version=26.1.0.0, Culture=neutral, PublicKeyToken={TOKEN}", "fileVersion": "26.9.0.0",
                  "core": "Countly, Version=26.1.0.0, Culture=neutral, PublicKeyToken=null"}
        self.assertEqual(consumers.probe_problems(values, self.plan, {"lib": "x"}, CORE, TOKEN), [
            "bound to the core SDK Countly, Version=26.1.0.0, Culture=neutral, PublicKeyToken=null, not signed with the Countly key token 30fd2b3d8bfe4882"])

    def test_request_parameters_merge_the_query_and_the_form_body_gzipped_or_not(self):
        """Request parameters merge the query and the form body gzipped or not."""
        self.assertEqual(consumers.request_parameters("/i?app_key=k&hc=1", b"begin_session=1&sdk_version=26.9.0", None),
                         {"app_key": "k", "hc": "1", "begin_session": "1", "sdk_version": "26.9.0"})
        self.assertEqual(consumers.request_parameters("/i", gzip.compress(b"events=%5B%5D"), "gzip"), {"events": "[]"})

    def test_a_session_start_and_the_probe_event_must_arrive_with_the_release_identity(self):
        """A session start and the probe event must arrive with the release identity."""
        base = {"app_key": "probe", "sdk_version": "26.9.0", "sdk_name": "csharp-net35"}
        arrived = [dict(base, method="sc"), dict(base, begin_session="1"), dict(base, events='[{"key":"release_probe"}]'), {"app_key": "other"}]
        self.assertEqual(consumers.delivery_problems(arrived, "probe", self.plan, NET35), [])
        self.assertEqual(consumers.delivery_problems([dict(base, sdk_version="26.8.0")], "probe", self.plan, NET35), [
            "a request reported SDK csharp-net35 26.8.0, expected csharp-net35 26.9.0", "no session start arrived", "the release_probe event did not arrive"])
        self.assertEqual(consumers.delivery_problems([{"app_key": "other"}], "probe", self.plan, NET35), ["no request with the probe's app key arrived (1 other requests)"])

    def test_the_recording_server_answers_like_countly_and_keeps_each_request(self):
        """The recording server answers like countly and keeps each request."""
        with consumers.RecordingServer() as server:
            request = urllib.request.Request(f"{server.url}/i?app_key=probe", data=b"begin_session=1", method="POST")
            with urllib.request.urlopen(request, timeout=10) as response:
                self.assertEqual(json.loads(response.read()), {"result": "Success"})
        self.assertEqual(server.requests, [{"app_key": "probe", "begin_session": "1"}])

    def test_frameworks_run_where_their_runtime_exists_and_build_from_the_environment(self):
        """Frameworks run where their runtime exists and build from the environment."""
        self.assertEqual([consumers.runnable(alias, False) for alias in ("net35", "net462", "net8.0", "net8.0-windows")], [False, False, True, False])
        self.assertTrue(all(consumers.runnable(alias, True) for alias in ("net35", "net8.0-windows")))
        self.assertEqual(consumers.probe_command(Path("/w/core/Countly.Consumer.csproj"), "net35"), [str(Path("/w/core/bin/Release/net35/Countly.Consumer.exe"))])
        self.assertEqual(consumers.probe_command(Path("/w/core/Countly.Consumer.csproj"), "net8.0"), ["dotnet", str(Path("/w/core/bin/Release/net8.0/Countly.Consumer.dll"))])
        env = consumers.build_environment({"PATH": "/bin"}, ["net35", "net8.0"], "/w/packages", "/w/http")
        self.assertEqual((env["ConsumerFrameworks"], env["NUGET_PACKAGES"], env["PATH"]), ("net35;net8.0", "/w/packages", "/bin"))
