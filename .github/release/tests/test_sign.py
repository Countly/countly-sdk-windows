import subprocess
import tempfile
import unittest
from pathlib import Path

import nupkg
import sign
from tests.support import countly_public_key, make_assembly, make_package

TOKEN = "30fd2b3d8bfe4882"


class FakeSn:
    """A stand-in for sn.exe: -R appends b"signed" in place of the last bytes (keeping the size), -vf verifies what -R
    signed, -T reports a token; `fail` names an option that fails."""

    def __init__(self, token=TOKEN, fail=None):
        """Remembers the token -T reports and the option that fails."""
        self.token = token
        self.fail = fail
        self.calls = []

    def __call__(self, args, **kwargs):
        """Answers one sn command."""
        option = args[2]
        self.calls.append(option)
        if option == self.fail:
            return subprocess.CompletedProcess(args, 1, stdout=f"{option} failed", stderr="")
        if option == "-R":
            path = Path(args[3])
            data = path.read_bytes()
            path.write_bytes(data[:-6] + b"signed")
        if option == "-vf" and not Path(args[3]).read_bytes().endswith(b"signed"):
            return subprocess.CompletedProcess(args, 1, stdout="not signed", stderr="")
        stdout = f"Public key token is {self.token}" if option == "-T" else ""
        return subprocess.CompletedProcess(args, 0, stdout=stdout, stderr="")


class SignTest(unittest.TestCase):
    """Signing the staged assemblies with sn."""

    def setUp(self):
        """A staged package with one assembly and its documentation."""
        self.folder = Path(tempfile.mkdtemp())
        self.path = make_package(self.folder / "Countly.26.9.0.nupkg", "Countly", "26.9.0", {
            "lib/net45/Countly.dll": make_assembly() + b"\x00" * 16, "lib/net45/Countly.xml": b"<doc />"})

    def test_signing_puts_the_signed_assemblies_back_and_leaves_every_other_entry(self):
        """Signing puts the signed assemblies back and leaves every other entry."""
        before = nupkg.entry_hashes(self.path)
        problems = sign.sign_package(self.path, "key.snk", countly_public_key(), TOKEN, "sn", self.folder / "work", FakeSn())
        self.assertEqual(problems, [])
        after = nupkg.entry_hashes(self.path)
        self.assertNotEqual(after["lib/net45/Countly.dll"], before["lib/net45/Countly.dll"])
        self.assertTrue(nupkg.read_entry(self.path, "lib/net45/Countly.dll").endswith(b"signed"))
        self.assertEqual({name: digest for name, digest in after.items() if not name.endswith(".dll")}, {name: digest for name, digest in before.items() if not name.endswith(".dll")})

    def test_a_failed_or_foreign_signature_leaves_the_package_as_it_was(self):
        """A failed or foreign signature leaves the package as it was."""
        for fake, expected in ((FakeSn(fail="-R"), "lib/net45/Countly.dll: sn -R failed: -R failed"),
                               (FakeSn(fail="-vf"), "lib/net45/Countly.dll: the strong-name signature does not verify: -vf failed"),
                               (FakeSn(token="0123456789abcdef"), "lib/net45/Countly.dll: sn -T reports the token 0123456789abcdef, the Countly token is 30fd2b3d8bfe4882")):
            with self.subTest(expected=expected):
                before = nupkg.entry_hashes(self.path)
                self.assertEqual(sign.sign_package(self.path, "key.snk", countly_public_key(), TOKEN, "sn", self.folder / "work", fake), [expected])
                self.assertEqual(nupkg.entry_hashes(self.path), before)

    def test_the_newest_tools_are_found_and_both_registry_views_skip_verification(self):
        """The newest tools are found and both registry views skip verification."""
        root = self.folder / "pf86"
        for folder in ("v10.0A/bin/NETFX 4.8 Tools", "v10.0A/bin/NETFX 4.8.1 Tools", "v8.1A/bin/NETFX 4.5.1 Tools"):
            for sub in ("", "x64"):
                path = root / "Microsoft SDKs" / "Windows" / folder / sub / "sn.exe"
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(b"")
        self.assertTrue(sign.find_sn(True, str(root)).endswith(str(Path("NETFX 4.8.1 Tools") / "x64" / "sn.exe")))
        self.assertTrue(sign.find_sn(False, str(root)).endswith(str(Path("NETFX 4.8.1 Tools") / "sn.exe")))
        with self.assertRaises(sign.SigningError):
            sign.find_sn(True, str(self.folder / "none"))
        self.assertEqual(sign.skip_verification_commands(TOKEN, ["x64", "x86"]), [["x64", "-q", "-Vr", f"*,{TOKEN}"], ["x86", "-q", "-Vr", f"*,{TOKEN}"]])
