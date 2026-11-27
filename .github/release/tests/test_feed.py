import tempfile
import unittest
from pathlib import Path

import feed
from tests.support import FakeWeb, make_package

URL = "https://api.nuget.org/v3-flatcontainer/countly.ui.webview2/26.9.0-test.4/countly.ui.webview2.26.9.0-test.4.nupkg"


class FeedTest(unittest.TestCase):
    """Reading a package from a feed the way a restore does."""

    def test_the_flat_container_address_is_lowercase(self):
        """The flat container address is lowercase."""
        self.assertEqual(feed.package_url("https://api.nuget.org/v3-flatcontainer/", "Countly.UI.WebView2", "26.9.0-Test.4"), URL)

    def test_waiting_polls_until_the_package_is_served_or_the_time_is_up(self):
        """Waiting polls until the package is served or the time is up."""
        clock = [0]
        web = FakeWeb({URL: b"package"}, after=3)
        self.assertEqual(feed.wait_for(URL, 600, 30, web, lambda: clock[0], lambda seconds: clock.__setitem__(0, clock[0] + seconds)), b"package")
        self.assertEqual((len(web.requests), clock[0]), (4, 90))
        clock[0] = 0
        self.assertIsNone(feed.wait_for(URL, 60, 30, FakeWeb(), lambda: clock[0], lambda seconds: clock.__setitem__(0, clock[0] + seconds)))

    def test_the_served_package_must_be_the_pushed_one_plus_the_gallery_signature(self):
        """The served package must be the pushed one plus the gallery signature."""
        folder = Path(tempfile.mkdtemp())
        files = {"lib/net45/Countly.dll": b"dll", "docs/readme.md": b"readme"}
        pushed = make_package(folder / "pushed.nupkg", "Countly", "26.9.0", files)
        served = make_package(folder / "served.nupkg", "Countly", "26.9.0", files, signature=b"repository signature")
        self.assertEqual(feed.published_problems(pushed, served.read_bytes()), [])
        changed = make_package(folder / "changed.nupkg", "Countly", "26.9.0", {"lib/net45/Countly.dll": b"other", "extra.txt": b"x"})
        self.assertEqual(feed.published_problems(pushed, changed.read_bytes()), ["missing docs/readme.md", "unexpected extra.txt", "changed lib/net45/Countly.dll"])
