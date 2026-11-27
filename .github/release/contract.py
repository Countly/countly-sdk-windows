"""Compares what a staged package promises integrators with its committed contract file, and checks the identity of
every assembly in it against the release."""

import zipfile

import nupkg

SCHEMA = 1


def actual_contract(path):
    """The contract a staged package fulfils, in the shape of the contract files: its id, delivered files, dependencies
    and framework references per target framework, and the assembly version its assemblies share."""
    metadata = nupkg.package_metadata(path)
    versions = set()
    with zipfile.ZipFile(path) as archive:
        for name in archive.namelist():
            if name.lower().endswith(".dll"):
                versions.add(nupkg.version_strings(archive.read(name)).get("Assembly Version"))
    return {
        "schema": SCHEMA,
        "id": metadata["id"],
        "assemblyVersion": versions.pop() if len(versions) == 1 else "differs between assemblies",
        "files": nupkg.package_files(path),
        "dependencies": metadata["dependencies"],
        "frameworkReferences": metadata["frameworkReferences"],
    }


def compare(expected, actual):
    """Differences between the committed contract and the staged package, as readable problems."""
    problems = []
    for key, label in (("id", "id"), ("assemblyVersion", "assembly version"), ("files", "files"),
                       ("dependencies", "dependencies"), ("frameworkReferences", "framework references")):
        if expected.get(key) != actual.get(key):
            problems.append(f"{label}: the package has {actual.get(key)}, the contract says {expected.get(key)}")
    return problems


def identity_problems(path, plan, public_key, commit=None):
    """Problems with the identity of each assembly in a staged package: it must carry the Countly strong-name public
    key, the release's file version, and a product version of the release, which the .NET SDK suffixes with the commit
    it built; when a commit is given, that suffix must name it."""
    problems = []
    metadata = nupkg.package_metadata(path)
    if metadata["version"] != plan.package_version:
        problems.append(f"package version is {metadata['version']}, the plan says {plan.package_version}")
    with zipfile.ZipFile(path) as archive:
        assemblies = sorted(name for name in archive.namelist() if name.lower().endswith(".dll"))
        if not assemblies:
            problems.append("the package holds no assembly")
        for name in assemblies:
            data = archive.read(name)
            versions = nupkg.version_strings(data)
            if not nupkg.has_public_key(data, public_key):
                problems.append(f"{name} does not carry the Countly strong-name public key")
            if versions.get("FileVersion") != plan.file_version:
                problems.append(f"{name}: file version is {versions.get('FileVersion')}, the release is {plan.file_version}")
            product, _, revision = (versions.get("ProductVersion") or "").partition("+")
            if product not in (plan.version, plan.file_version):
                problems.append(f"{name}: product version is {versions.get('ProductVersion')}, the release is {plan.version}")
            elif commit and revision and revision != commit:
                problems.append(f"{name}: built from commit {revision}, the release commit is {commit}")
    return problems


def summary_rows(expected, actual):
    """(property, contract, built) rows for the job summary table."""
    def listing(values):
        """One table cell: a dict as "framework: entries" pairs, a list joined, anything else as text."""
        if isinstance(values, dict):
            return "; ".join(f"{key}: {', '.join(entries) or 'none'}" for key, entries in sorted(values.items())) or "none"
        if isinstance(values, list):
            return ", ".join(values) or "none"
        return str(values)

    return [(label, listing(expected.get(key)), listing(actual.get(key))) for key, label in (
        ("assemblyVersion", "assembly version"), ("files", "files"), ("dependencies", "dependencies"),
        ("frameworkReferences", "framework references"),
    )]
