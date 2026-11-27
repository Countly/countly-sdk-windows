"""Reads NuGet packages (.nupkg zip files) and the assemblies inside them, and rewrites a package with replaced files."""

import hashlib
import shutil
import xml.etree.ElementTree as ElementTree
import zipfile
from pathlib import Path

REPOSITORY_SIGNATURE = ".signature.p7s"
VERSION_KEYS = ("FileVersion", "Assembly Version", "ProductVersion")


def is_packaging_part(name):
    """Whether a zip entry belongs to the package format rather than to what the package delivers: the nuspec, the OPC
    parts NuGet writes, and the signature a gallery adds."""
    return (
        name.startswith("_rels/")
        or name.startswith("package/services/metadata/")
        or name in ("[Content_Types].xml", REPOSITORY_SIGNATURE)
        or ("/" not in name and name.lower().endswith(".nuspec"))
    )


def _local(tag):
    """An XML tag without its namespace; nuspec files use several schema namespaces."""
    return tag.rsplit("}", 1)[-1]


def _children(element, name):
    """Direct children of an element with the given local name."""
    return [child for child in element if _local(child.tag) == name]


def _child_text(element, name):
    """Text of the first direct child with the given local name, or None."""
    found = _children(element, name)
    return (found[0].text or "").strip() if found else None


def nuspec_bytes(archive):
    """The nuspec of an open package."""
    names = [name for name in archive.namelist() if "/" not in name and name.lower().endswith(".nuspec")]
    if len(names) != 1:
        raise ValueError(f"expected one nuspec at the package root, found {len(names)}")
    return archive.read(names[0])


def _grouped(metadata, container, item, describe):
    """Entries of a nuspec list such as dependencies, per target framework: each <group> under the container, and
    entries outside any group under "any"."""
    found = {}
    for parent in _children(metadata, container):
        for child in parent:
            if _local(child.tag) == "group":
                found[child.get("targetFramework", "any")] = sorted(describe(entry) for entry in _children(child, item))
            elif _local(child.tag) == item:
                found.setdefault("any", []).append(describe(child))
    return {framework: sorted(entries) for framework, entries in found.items()}


def read_metadata(nuspec):
    """The parts of a nuspec a release promises: id, version, dependencies and framework references per target
    framework, as sorted "id version" and name lists."""
    metadata = _children(ElementTree.fromstring(nuspec), "metadata")[0]
    return {
        "id": _child_text(metadata, "id"),
        "version": _child_text(metadata, "version"),
        "dependencies": _grouped(metadata, "dependencies", "dependency", lambda entry: f"{entry.get('id')} {entry.get('version')}"),
        "frameworkReferences": _grouped(metadata, "frameworkReferences", "frameworkReference", lambda entry: entry.get("name")),
    }


def package_files(path):
    """Sorted names of the files a package delivers, without its packaging parts."""
    with zipfile.ZipFile(path) as archive:
        return sorted(name for name in archive.namelist() if not is_packaging_part(name) and not name.endswith("/"))


def package_metadata(path):
    """read_metadata of the package at path."""
    with zipfile.ZipFile(path) as archive:
        return read_metadata(nuspec_bytes(archive))


def entry_hashes(path):
    """sha256 of every entry of a package, except the signature a gallery adds."""
    with zipfile.ZipFile(path) as archive:
        return {name: hashlib.sha256(archive.read(name)).hexdigest() for name in archive.namelist() if name != REPOSITORY_SIGNATURE}


def read_entry(path, name):
    """The bytes of one entry of a package."""
    with zipfile.ZipFile(path) as archive:
        return archive.read(name)


def replace_entries(path, replacements):
    """Rewrites the package at path with the given entries replaced by new bytes, keeping every other entry, the entry
    order, timestamps and compression, so nothing but the replaced files changes."""
    path = Path(path)
    temporary = path.with_name(path.name + ".tmp")
    with zipfile.ZipFile(path) as source, zipfile.ZipFile(temporary, "w") as target:
        missing = set(replacements) - set(source.namelist())
        if missing:
            raise KeyError(f"not in {path.name}: {', '.join(sorted(missing))}")
        for info in source.infolist():
            target.writestr(info, replacements.get(info.filename, source.read(info.filename)), compress_type=info.compress_type)
    shutil.move(str(temporary), str(path))


def version_strings(assembly):
    """The FileVersion, Assembly Version and ProductVersion of a .NET assembly, read from the string table of its Win32
    version resource, which the C# compiler fills from the assembly attributes; a missing key is left out."""
    found = {}
    for key in VERSION_KEYS:
        needle = key.encode("utf-16-le") + b"\x00\x00"
        start = assembly.find(needle)
        while start != -1:
            header = start - 6
            length = int.from_bytes(assembly[header:header + 2], "little") if header >= 0 else 0
            value_words = int.from_bytes(assembly[header + 2:header + 4], "little") if header >= 0 else 0
            kind = int.from_bytes(assembly[header + 4:header + 6], "little") if header >= 0 else 0
            if kind == 1 and 0 < length < 1024:
                value = start + len(needle)
                value += (header - value) % 4
                found[key] = assembly[value:value + value_words * 2].decode("utf-16-le", "replace").rstrip("\x00")
                break
            start = assembly.find(needle, start + 1)
    return found


def has_public_key(assembly, public_key):
    """Whether the assembly carries the public key blob of a strong name, which its metadata stores as is."""
    return public_key in assembly


def strong_name_token(public_key):
    """The public key token of a strong-name public key blob: the last eight bytes of its SHA-1 hash, reversed."""
    return hashlib.sha1(public_key).digest()[-8:][::-1].hex()
