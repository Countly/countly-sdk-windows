import tempfile
import unittest
from pathlib import Path

import build
from plan import for_target, plan_for_tag
from tests.support import repository_config, write_core, write_file

KEY = "/work/countly-public.snk"
DELAY_SIGNED = ["SignAssembly=true", "DelaySign=true", f"AssemblyOriginatorKeyFile={KEY}", "ContinuousIntegrationBuild=true"]


class BuildTest(unittest.TestCase):
    """The compile and pack commands and the core layout."""

    def setUp(self):
        """The repository configuration and an empty fake checkout."""
        self.config = repository_config()
        self.root = Path(tempfile.mkdtemp())

    def test_the_core_compiles_the_classic_projects_with_msbuild_and_netstd_with_dotnet_all_delay_signed(self):
        """The core compiles the classic projects with msbuild and netstd with dotnet all delay signed."""
        commands = build.compile_commands(self.config, "core", "/repo", KEY, "/work")
        self.assertEqual([command[:2] for command in commands], [
            ["nuget", "restore"], ["msbuild", str(Path("/repo/net35/Countly/Countly.csproj"))],
            ["nuget", "restore"], ["nuget", "install"], ["msbuild", str(Path("/repo/net45/Countly/Countly.csproj"))],
            ["dotnet", "build"],
        ])
        self.assertEqual(commands[0][2:5], [str(Path("/repo/net35/Countly/packages.config")), "-PackagesDirectory", str(Path("/repo/net35/packages"))])
        for command in (commands[1], commands[4]):
            self.assertEqual([part for part in command if part.startswith("/p:") and "CustomAfter" not in part], ["/p:Configuration=Release"] + [f"/p:{value}" for value in DELAY_SIGNED])
            self.assertIn("/t:Rebuild", command)
        self.assertTrue(commands[4][-1].startswith("/p:CustomAfterMicrosoftCommonTargets="))
        self.assertTrue(commands[4][-1].endswith("Microsoft.NETFramework.ReferenceAssemblies.net45.targets"))
        self.assertEqual(commands[5][-4:], [f"-p:{value}" for value in DELAY_SIGNED])
        self.assertEqual(build.compile_commands(self.config, "ui", "/repo", KEY, "/work"), [])

    def test_the_ui_is_packed_against_the_published_core_and_a_test_run_changes_only_the_package_version(self):
        """The ui is packed against the published core and a test run changes only the package version."""
        plan = plan_for_tag(self.config, "26.9.0-ui")
        command = build.pack_project_command(self.config, "ui", plan, "/repo", KEY, "/staged")
        self.assertEqual(command[:3], ["dotnet", "pack", str(Path("/repo/ui/Countly.UI.WebView2/Countly.UI.WebView2.csproj"))])
        self.assertIn("-p:UsePackagedCountly=true", command)
        self.assertFalse([part for part in command if "PackageVersion" in part])
        tested = build.pack_project_command(self.config, "ui", for_target(plan, "test", "3"), "/repo", KEY, "/staged")
        self.assertEqual(tested[-1], "-p:PackageVersion=26.9.0-test.3")

    def test_the_staged_nuspec_carries_the_package_version(self):
        """The staged nuspec carries the package version."""
        write_core(self.root, "26.9.0", "## 26.9.0\n")
        layout = self.root / "layout"
        nuspec = build.stage_nuspec(self.config, "core", for_target(plan_for_tag(self.config, "26.9.0"), "test", "8"), self.root, layout)
        self.assertIn("<version>26.9.0-test.8</version>", nuspec.read_text(encoding="utf-8"))
        self.assertIn('<dependency id="Newtonsoft.Json" version="13.0.3" />', nuspec.read_text(encoding="utf-8"))
        write_file(self.root, "nuget/Countly.nuspec", "<package><metadata><id>Countly</id></metadata></package>")
        with self.assertRaises(build.BuildError):
            build.stage_nuspec(self.config, "core", plan_for_tag(self.config, "26.9.0"), self.root, layout)

    def test_the_layout_is_copied_from_the_build_outputs_and_a_missing_one_is_reported(self):
        """The layout is copied from the build outputs and a missing one is reported."""
        for entry in self.config["packages"]["core"]["layout"]:
            if "net45" not in entry["from"]:
                write_file(self.root, entry["from"], entry["path"])
        problems = build.assemble_layout(self.config, "core", self.root, self.root / "layout")
        self.assertEqual(problems, [
            "net45/Countly/bin/Release/Countly.dll does not exist; the build did not produce it",
            "net45/Countly/bin/Release/Countly.xml does not exist; the build did not produce it",
        ])
        self.assertEqual((self.root / "layout/lib/netstandard2.0/Countly.xml").read_text(encoding="utf-8"), "lib/netstandard2.0/Countly.xml")

    def test_only_build_outputs_are_removed_before_compiling(self):
        """Only build outputs are removed before compiling."""
        outputs = [path.relative_to("/repo").as_posix() for path in build.built_outputs(self.config, "core", Path("/repo"))]
        self.assertIn("netstd/Countly/Countly.xml", outputs)
        self.assertNotIn("nuget/docs/readme.md", outputs)
        self.assertEqual(len(outputs), 6)

    def test_the_public_key_file_holds_the_committed_key(self):
        """The public key file holds the committed key."""
        path = build.write_public_key(self.config, self.root / "key" / "countly-public.snk")
        self.assertEqual(path.read_bytes().hex(), self.config["strongName"]["publicKey"])
