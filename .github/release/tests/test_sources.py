import tempfile
import unittest
from pathlib import Path

from plan import plan_for_tag
from sources import changelog_section, check_version_sources, release_notes
from tests.support import repository_config, write_core, write_file, write_ui


class SourcesTest(unittest.TestCase):
    """Version sources and changelog notes."""

    def setUp(self):
        """The configuration and an empty fake checkout."""
        self.config = repository_config()
        self.root = Path(tempfile.mkdtemp())

    def problems(self, tag):
        """Version source problems of the fake checkout for a tag."""
        return check_version_sources(self.root, plan_for_tag(self.config, tag), self.config)

    def test_every_source_matching_the_tag_passes(self):
        """Every source matching the tag passes."""
        write_core(self.root, "26.9.0", "## 26.9.0\n* Change.\n")
        write_ui(self.root, "26.9.0", "## 26.9.0\n* UI change.\n")
        self.assertEqual(self.problems("26.9.0"), [])
        self.assertEqual(self.problems("26.9.0-ui"), [])

    def test_each_straggler_is_named_with_the_version_it_holds(self):
        """Each straggler is named with the version it holds."""
        write_core(self.root, "26.9.0", "## 26.9.0\n", nuspec="26.8.0", sdk="26.8.1", file="26.8.0.0", test="26.8.0")
        self.assertEqual(self.problems("26.9.0"), [
            "nuget/Countly.nuspec: version is 26.8.0, the tag says 26.9.0",
            "countlyCommon/countlyCommon/CountlyBase.cs: version is 26.8.1, the tag says 26.9.0",
            "netstd/Countly/Countly.csproj: version is 26.8.0.0, the tag says 26.9.0.0",
            "net35/Countly/Properties/AssemblyInfo.cs: version is 26.8.0.0, the tag says 26.9.0.0",
            "net45/Countly/Properties/AssemblyInfo.cs: version is 26.8.0.0, the tag says 26.9.0.0",
            "countlyCommon/TestingRelated/TestHelper.cs: version is 26.8.0, the tag says 26.9.0",
            "countlyCommon/TestingRelated/RequestTestCases.cs: version is 26.8.0, the tag says 26.9.0",
        ])

    def test_the_commented_out_assembly_version_line_is_not_read(self):
        """The commented out assembly version line is not read."""
        write_core(self.root, "26.9.0", "## 26.9.0\n")
        write_file(self.root, "net35/Countly/Properties/AssemblyInfo.cs", '// [assembly: AssemblyFileVersion("1.0.0.0")]\n[assembly: AssemblyFileVersion("26.9.0.0")]\n')
        self.assertEqual(self.problems("26.9.0"), [])

    def test_a_release_candidate_needs_the_suffix_in_versions_but_not_in_file_versions_nor_a_changelog_heading(self):
        """A release candidate needs the suffix in versions but not in file versions nor a changelog heading."""
        write_core(self.root, "26.9.0-rc1", "## XX.XX.XX\n* Coming.\n")
        self.assertEqual(self.problems("26.9.0-rc1"), [])

    def test_a_final_release_needs_its_changelog_heading_and_no_placeholder(self):
        """A final release needs its changelog heading and no placeholder."""
        write_core(self.root, "26.9.0", "## XX.XX.XX\n* Waiting.\n\n## 26.8.0\n* Old.\n")
        self.assertEqual(self.problems("26.9.0"), [
            "CHANGELOG.md: no '## 26.9.0' heading",
            "CHANGELOG.md: still has a '## XX.XX.XX' heading; move its lines under '## 26.9.0'",
        ])

    def test_missing_files_are_reported(self):
        """Missing files are reported."""
        self.assertIn("ui/Countly.UI.WebView2/Countly.UI.WebView2.csproj does not exist", self.problems("26.9.0-ui"))

    def test_notes_are_the_changelog_section_and_a_candidate_takes_the_placeholder_section(self):
        """Notes are the changelog section and a candidate takes the placeholder section."""
        write_core(self.root, "26.9.0", "## 26.9.0\n\n* Change.\n  more\n\n## 26.8.0\n* Old.\n")
        self.assertEqual(release_notes(self.root, plan_for_tag(self.config, "26.9.0"), self.config), ("* Change.\n  more\n", []))
        write_core(self.root, "26.9.0-rc1", "## XX.XX.XX\n* Soon.\n\n## 26.8.0\n* Old.\n")
        self.assertEqual(release_notes(self.root, plan_for_tag(self.config, "26.9.0-rc1"), self.config), ("* Soon.\n", []))

    def test_a_heading_inside_a_code_block_does_not_end_the_section(self):
        """A heading inside a code block does not end the section."""
        text = "## 26.9.0\n* Example:\n```\n## not a heading\n```\n* Last.\n## 26.8.0\n"
        self.assertEqual(changelog_section(text, "26.9.0"), "* Example:\n```\n## not a heading\n```\n* Last.\n")
        self.assertIsNone(changelog_section(text, "26.7.0"))
