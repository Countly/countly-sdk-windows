"""Shared test helpers: the repository configuration, fake checkouts, and synthetic assemblies, packages and web."""

import zipfile
from pathlib import Path

from plan import load_config

NUSPEC = """<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
    <metadata>
        <id>Countly</id>
        <version>{version}</version>
        <dependencies>
          <group targetFramework="net45">
            <dependency id="Newtonsoft.Json" version="13.0.3" />
          </group>
        </dependencies>
    </metadata>
</package>
"""
COUNTLY_BASE = 'namespace CountlySDK.CountlyCommon\n{\n    public abstract class CountlyBase\n    {\n        protected const string sdkVersion = "{version}";\n    }\n}\n'
NETSTD_PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyVersion>26.1.0.0</AssemblyVersion>
    <FileVersion>{file}</FileVersion>
    <Version>{version}</Version>
  </PropertyGroup>
</Project>
"""
ASSEMBLY_INFO = '// [assembly: AssemblyVersion("1.0.*")]\n[assembly: AssemblyVersion("26.1.0.0")]\n[assembly: AssemblyFileVersion("{file}")]\n'
TEST_HELPER = 'public static class TestHelper\n{\n        public static string SDK_VERSION = "{version}";\n}\n'
REQUEST_TESTS = 'public class RequestTestCases\n{\n            Assert.Equal("{version}", baseParams["sdk_version"]);\n}\n'
UI_PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Version>{version}</Version>
    <AssemblyVersion>26.1.0.0</AssemblyVersion>
    <FileVersion>{file}</FileVersion>
  </PropertyGroup>
  <ItemGroup Condition="'$(UsePackagedCountly)' == 'true'">
    <PackageReference Include="Countly" Version="26.1.0" />
  </ItemGroup>
</Project>
"""
CONTENT_TYPES = '<?xml version="1.0" encoding="utf-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types" />'


def repository_config():
    """The real config.json of this repository."""
    return load_config(Path(__file__).resolve().parents[1] / "config.json")


def countly_public_key():
    """The Countly strong-name public key blob of config.json."""
    return bytes.fromhex(repository_config()["strongName"]["publicKey"])


def write_file(root, relative, text):
    """Writes a file of a fake checkout."""
    path = Path(root) / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(text.encode("utf-8"))


def file_version(version):
    """The four-part file version of a release version, e.g. 26.9.0.0 for 26.9.0-rc1."""
    return version.split("-")[0] + ".0"


def write_core(root, version, changelog, nuspec=None, sdk=None, file=None, test=None):
    """Writes every version source of the core SDK and its changelog into a fake checkout; the optional arguments put
    another version into one kind of source."""
    file = file or file_version(version)
    write_file(root, "nuget/Countly.nuspec", NUSPEC.replace("{version}", nuspec or version))
    write_file(root, "countlyCommon/countlyCommon/CountlyBase.cs", COUNTLY_BASE.replace("{version}", sdk or version))
    write_file(root, "netstd/Countly/Countly.csproj", NETSTD_PROJECT.replace("{version}", version).replace("{file}", file))
    for flavor in ("net35", "net45"):
        write_file(root, f"{flavor}/Countly/Properties/AssemblyInfo.cs", ASSEMBLY_INFO.replace("{file}", file))
    write_file(root, "countlyCommon/TestingRelated/TestHelper.cs", TEST_HELPER.replace("{version}", test or version))
    write_file(root, "countlyCommon/TestingRelated/RequestTestCases.cs", REQUEST_TESTS.replace("{version}", test or version))
    write_file(root, "CHANGELOG.md", changelog)


def write_ui(root, version, changelog, file=None):
    """Writes the version sources of the UI package and its changelog into a fake checkout."""
    write_file(root, "ui/Countly.UI.WebView2/Countly.UI.WebView2.csproj", UI_PROJECT.replace("{version}", version).replace("{file}", file or file_version(version)))
    write_file(root, "ui/Countly.UI.WebView2/CHANGELOG.md", changelog)


def version_entry(key, value):
    """One String structure of a Win32 version resource, padded to a 32-bit boundary like the compiler writes it."""
    name = key.encode("utf-16-le") + b"\x00\x00"
    text = value.encode("utf-16-le") + b"\x00\x00"
    padding = (-(6 + len(name))) % 4
    length = 6 + len(name) + padding + len(text)
    entry = length.to_bytes(2, "little") + (len(text) // 2).to_bytes(2, "little") + (1).to_bytes(2, "little") + name + b"\x00" * padding + text
    return entry + b"\x00" * ((-length) % 4)


def make_assembly(file="26.9.0.0", assembly="26.1.0.0", product=None, public_key=None):
    """Bytes standing in for a .NET assembly: a header, the strong-name public key blob (the Countly one unless
    another is given; b"" for none), and a version resource string table."""
    key = countly_public_key() if public_key is None else public_key
    entries = [("FileVersion", file), ("Assembly Version", assembly), ("ProductVersion", product or file)]
    return b"MZ" + b"\x00" * 62 + key + b"\x00" * ((-len(key)) % 4) + b"".join(version_entry(name, value) for name, value in entries if value is not None)


def nuspec_text(package_id, version, dependencies=None, references=None):
    """A nuspec with dependency and framework reference groups, each a dict of framework to a list of entries."""
    lines = ['<?xml version="1.0" encoding="utf-8"?>', '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">',
             "  <metadata>", f"    <id>{package_id}</id>", f"    <version>{version}</version>"]
    if dependencies is not None:
        lines.append("    <dependencies>")
        for framework, entries in dependencies.items():
            lines.append(f'      <group targetFramework="{framework}">')
            lines += [f'        <dependency id="{entry.split()[0]}" version="{entry.split()[1]}" exclude="Build,Analyzers" />' for entry in entries]
            lines.append("      </group>")
        lines.append("    </dependencies>")
    if references is not None:
        lines.append("    <frameworkReferences>")
        for framework, names in references.items():
            lines.append(f'      <group targetFramework="{framework}">')
            lines += [f'        <frameworkReference name="{name}" />' for name in names]
            lines.append("      </group>")
        lines.append("    </frameworkReferences>")
    return "\n".join(lines + ["  </metadata>", "</package>", ""])


def make_package(path, package_id, version, files, dependencies=None, references=None, signature=None):
    """Writes a .nupkg with the OPC parts NuGet writes, a nuspec and the given files (name to bytes); a signature is
    added as .signature.p7s, the way a gallery does."""
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("_rels/.rels", "<Relationships />")
        archive.writestr(f"{package_id}.nuspec", nuspec_text(package_id, version, dependencies, references))
        for name, data in files.items():
            archive.writestr(name, data)
        archive.writestr("[Content_Types].xml", CONTENT_TYPES)
        archive.writestr("package/services/metadata/core-properties/0123456789abcdef.psmdcp", "<coreProperties />")
        if signature is not None:
            archive.writestr(".signature.p7s", signature)
    return path


class FakeWeb:
    """Serves a dict of URL to bytes; anything else is a 404 (None). `after` makes a URL appear only from that request
    on, the way a gallery serves a package once it has indexed it."""

    def __init__(self, pages=None, after=0):
        """Keeps the pages, the number of requests before they appear, and every request."""
        self.pages = dict(pages or {})
        self.after = after
        self.requests = []

    def __call__(self, url):
        """Answers one request."""
        self.requests.append(url)
        return self.pages.get(url) if len(self.requests) > self.after else None
