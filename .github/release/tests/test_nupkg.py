import tempfile
import unittest
import zipfile
from pathlib import Path

import nupkg
from tests.support import countly_public_key, make_assembly, make_package, repository_config


class NupkgTest(unittest.TestCase):
    """Reading and rewriting packages and the assemblies in them."""

    def setUp(self):
        """A folder for the packages of a test."""
        self.folder = Path(tempfile.mkdtemp())

    def test_metadata_reads_groups_of_any_nuspec_namespace_and_ungrouped_dependencies(self):
        """Metadata reads groups of any nuspec namespace and ungrouped dependencies."""
        grouped = nupkg.read_metadata(b"""<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"><metadata>
            <id>Countly</id><version>26.9.0</version>
            <dependencies><group targetFramework=".NETFramework3.5"><dependency id="TaskParallelLibrary" version="1.0.2856" />
            <dependency id="AsyncBridge.Net35" version="0.3.1" /></group><group targetFramework=".NETStandard2.0" /></dependencies>
            </metadata></package>""")
        self.assertEqual(grouped, {"id": "Countly", "version": "26.9.0", "frameworkReferences": {},
                                   "dependencies": {".NETFramework3.5": ["AsyncBridge.Net35 0.3.1", "TaskParallelLibrary 1.0.2856"], ".NETStandard2.0": []}})
        flat = nupkg.read_metadata(b'<package><metadata><id>X</id><version>1.0.0</version><dependencies><dependency id="A" version="2.0.0" /></dependencies></metadata></package>')
        self.assertEqual(flat["dependencies"], {"any": ["A 2.0.0"]})

    def test_package_files_leave_out_the_packaging_parts_and_a_gallery_signature(self):
        """Package files leave out the packaging parts and a gallery signature."""
        path = make_package(self.folder / "Countly.26.9.0.nupkg", "Countly", "26.9.0", {"lib/net45/Countly.dll": b"x", "docs/readme.md": b"r"}, signature=b"sig")
        self.assertEqual(nupkg.package_files(path), ["docs/readme.md", "lib/net45/Countly.dll"])
        self.assertNotIn(".signature.p7s", nupkg.entry_hashes(path))

    def test_replacing_entries_keeps_every_other_entry_and_the_order(self):
        """Replacing entries keeps every other entry and the order."""
        path = make_package(self.folder / "Countly.26.9.0.nupkg", "Countly", "26.9.0", {"lib/a.dll": b"old", "lib/b.xml": b"doc"})
        before = zipfile.ZipFile(path).namelist()
        others = {name: digest for name, digest in nupkg.entry_hashes(path).items() if name != "lib/a.dll"}
        nupkg.replace_entries(path, {"lib/a.dll": b"new"})
        self.assertEqual(zipfile.ZipFile(path).namelist(), before)
        self.assertEqual(nupkg.read_entry(path, "lib/a.dll"), b"new")
        self.assertEqual({name: digest for name, digest in nupkg.entry_hashes(path).items() if name != "lib/a.dll"}, others)
        with self.assertRaises(KeyError):
            nupkg.replace_entries(path, {"lib/missing.dll": b""})

    def test_version_strings_and_the_public_key_are_read_from_the_assembly_bytes(self):
        """Version strings and the public key are read from the assembly bytes."""
        assembly = make_assembly(file="26.9.0.0", assembly="26.1.0.0", product="26.9.0+" + "c" * 40)
        self.assertEqual(nupkg.version_strings(assembly), {"FileVersion": "26.9.0.0", "Assembly Version": "26.1.0.0", "ProductVersion": "26.9.0+" + "c" * 40})
        self.assertTrue(nupkg.has_public_key(assembly, countly_public_key()))
        self.assertFalse(nupkg.has_public_key(make_assembly(public_key=b""), countly_public_key()))
        self.assertEqual(nupkg.version_strings(b"MZ" + b"\x00" * 100), {})

    def test_the_committed_public_key_has_the_committed_token(self):
        """The committed public key has the committed token."""
        config = repository_config()["strongName"]
        self.assertEqual(nupkg.strong_name_token(bytes.fromhex(config["publicKey"])), config["token"])
