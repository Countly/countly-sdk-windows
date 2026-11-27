import tempfile
import unittest
from pathlib import Path

from manifest import build_manifest, verify_manifest


class ManifestTest(unittest.TestCase):
    """The manifest of staged files."""

    def test_the_manifest_catches_missing_unexpected_and_changed_files(self):
        """The manifest catches missing unexpected and changed files."""
        folder = Path(tempfile.mkdtemp())
        (folder / "Countly.26.9.0.nupkg").write_bytes(b"package")
        (folder / "Countly.UI.WebView2.26.9.0.nupkg").write_bytes(b"ui")
        manifest = build_manifest(folder, "26.9.0", "c" * 40)
        self.assertEqual(verify_manifest(folder, manifest), [])
        (folder / "Countly.26.9.0.nupkg").write_bytes(b"changed")
        (folder / "Countly.UI.WebView2.26.9.0.nupkg").unlink()
        (folder / "extra.nupkg").write_bytes(b"x")
        self.assertEqual(verify_manifest(folder, manifest), ["missing Countly.UI.WebView2.26.9.0.nupkg", "unexpected extra.nupkg", "changed Countly.26.9.0.nupkg"])
