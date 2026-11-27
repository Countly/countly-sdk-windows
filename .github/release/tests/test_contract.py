import json
import tempfile
import unittest
from pathlib import Path

import contract
from plan import for_target, plan_for_tag
from tests.support import countly_public_key, make_assembly, make_package, repository_config

REPO_ROOT = Path(__file__).resolve().parents[3]
COMMIT = "c" * 40
DEPENDENCIES = {".NETFramework4.5": ["Newtonsoft.Json 13.0.3"], ".NETStandard2.0": ["Newtonsoft.Json 13.0.3"]}


class ContractTest(unittest.TestCase):
    """The contract files and the identity of the staged assemblies."""

    def setUp(self):
        """The configuration, a folder for packages and a plan for 26.9.0."""
        self.config = repository_config()
        self.folder = Path(tempfile.mkdtemp())
        self.plan = plan_for_tag(self.config, "26.9.0")

    def package(self, assemblies, dependencies=None, version="26.9.0"):
        """A staged Countly package holding the given assemblies (name to bytes) and a readme."""
        files = dict(assemblies, **{"docs/readme.md": b"readme"})
        return make_package(self.folder / f"Countly.{version}.nupkg", "Countly", version, files, dependencies or DEPENDENCIES)

    def test_the_actual_contract_lists_files_dependencies_and_the_shared_assembly_version(self):
        """The actual contract lists files dependencies and the shared assembly version."""
        path = self.package({"lib/net45/Countly.dll": make_assembly(), "lib/netstandard2.0/Countly.dll": make_assembly()})
        self.assertEqual(contract.actual_contract(path), {
            "schema": 1, "id": "Countly", "assemblyVersion": "26.1.0.0", "frameworkReferences": {}, "dependencies": DEPENDENCIES,
            "files": ["docs/readme.md", "lib/net45/Countly.dll", "lib/netstandard2.0/Countly.dll"],
        })
        mixed = self.package({"lib/net45/Countly.dll": make_assembly(assembly="26.9.0.0"), "lib/netstandard2.0/Countly.dll": make_assembly()})
        self.assertEqual(contract.actual_contract(mixed)["assemblyVersion"], "differs between assemblies")

    def test_differences_from_the_contract_are_named(self):
        """Differences from the contract are named."""
        expected = {"id": "Countly", "assemblyVersion": "26.1.0.0", "files": ["a"], "dependencies": {}, "frameworkReferences": {}}
        actual = dict(expected, assemblyVersion="26.9.0.0", files=["a", "lib/old/Countly.dll"])
        self.assertEqual(contract.compare(expected, actual), [
            "assembly version: the package has 26.9.0.0, the contract says 26.1.0.0",
            "files: the package has ['a', 'lib/old/Countly.dll'], the contract says ['a']",
        ])
        self.assertEqual(contract.compare(expected, dict(expected)), [])

    def test_assemblies_must_carry_the_countly_key_the_release_versions_and_the_release_commit(self):
        """Assemblies must carry the countly key the release versions and the release commit."""
        good = self.package({"lib/net45/Countly.dll": make_assembly(product="26.9.0.0"), "lib/netstandard2.0/Countly.dll": make_assembly(product=f"26.9.0+{COMMIT}")})
        self.assertEqual(contract.identity_problems(good, self.plan, countly_public_key(), COMMIT), [])
        bad = self.package({
            "lib/net45/Countly.dll": make_assembly(file="26.8.0.0", public_key=b""),
            "lib/netstandard2.0/Countly.dll": make_assembly(product="26.9.0+" + "d" * 40),
        })
        self.assertEqual(contract.identity_problems(bad, self.plan, countly_public_key(), COMMIT), [
            "lib/net45/Countly.dll does not carry the Countly strong-name public key",
            "lib/net45/Countly.dll: file version is 26.8.0.0, the release is 26.9.0.0",
            "lib/net45/Countly.dll: product version is 26.8.0.0, the release is 26.9.0",
            f"lib/netstandard2.0/Countly.dll: built from commit {'d' * 40}, the release commit is {COMMIT}",
        ])

    def test_a_test_package_keeps_the_release_assemblies_under_its_own_package_version(self):
        """A test package keeps the release assemblies under its own package version."""
        plan = for_target(self.plan, "test", "5")
        path = self.package({"lib/net45/Countly.dll": make_assembly()}, version="26.9.0-test.5")
        self.assertEqual(contract.identity_problems(path, plan, countly_public_key()), [])
        self.assertEqual(contract.identity_problems(path, self.plan, countly_public_key()), ["package version is 26.9.0-test.5, the plan says 26.9.0"])

    def test_every_package_has_a_contract_with_consumers_whose_libraries_it_ships(self):
        """Every package has a contract with consumers whose libraries it ships."""
        for key, spec in self.config["packages"].items():
            with self.subTest(package=key):
                promised = json.loads((REPO_ROOT / spec["contract"]).read_text(encoding="utf-8"))
                self.assertEqual((promised["schema"], promised["id"]), (1, spec["id"]))
                self.assertTrue(promised["consumers"])
                for alias, expectation in promised["consumers"].items():
                    self.assertIn(expectation["lib"], promised["files"], alias)
                self.assertTrue((REPO_ROOT / spec["consumer"]).is_dir())
