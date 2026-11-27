"""Signs the delay-signed assemblies of staged packages with the Countly strong-name key, using the Windows SDK's sn.exe."""

import glob
import os
import re
import subprocess
import zipfile
from pathlib import Path

import nupkg


class SigningError(Exception):
    """A signing setup that must stop the publish."""


def _tools_version(path):
    """The version in a "NETFX 4.8.1 Tools" folder name of an sn.exe path, as a tuple to sort by."""
    match = re.search(r"NETFX ([\d.]+) Tools", path)
    return tuple(int(part) for part in match.group(1).split(".")) if match else ()


def find_sn(x64=True, program_files_x86=None):
    """The sn.exe of the newest .NET Framework tools in the Windows SDK: the 64-bit one by default, which writes the
    registry view 64-bit processes read, or the 32-bit one."""
    root = program_files_x86 or os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")
    parts = [root, "Microsoft SDKs", "Windows", "*", "bin", "NETFX * Tools"] + (["x64"] if x64 else []) + ["sn.exe"]
    found = sorted(glob.glob(os.path.join(*parts)), key=_tools_version)
    if not found:
        raise SigningError(f"no {'64-bit ' if x64 else ''}sn.exe under {root}\\Microsoft SDKs\\Windows; the runner needs the Windows SDK's .NET Framework tools")
    return found[-1]


def _output(result):
    """The text a finished command printed."""
    return ((result.stdout or "") + (result.stderr or "")).strip()


def public_key_of(key_file, sn, work, runner=subprocess.run):
    """The public key blob of a strong-name key pair file, extracted with sn -p."""
    target = Path(work) / "key-public.snk"
    result = runner([sn, "-q", "-p", str(key_file), str(target)], capture_output=True, text=True)
    if result.returncode != 0 or not target.is_file():
        raise SigningError(f"sn -p could not read the strong-name key: {_output(result)}")
    return target.read_bytes()


def token_of(assembly, sn, runner=subprocess.run):
    """The public key token sn -T reports for an assembly, or None when it reports none."""
    match = re.search(r"\b([0-9a-f]{16})\b", _output(runner([sn, "-q", "-T", str(assembly)], capture_output=True, text=True)))
    return match.group(1) if match else None


def sign_package(path, key_file, public_key, token, sn, work, runner=subprocess.run):
    """Signs every assembly of a staged package with the key pair and puts the signed assemblies back into the package;
    returns problems, and leaves the package as it was when there are any. Each signed assembly must verify with sn -vf,
    keep its size, carry the Countly public key and report the Countly token."""
    problems = []
    replacements = {}
    with zipfile.ZipFile(path) as archive:
        names = [name for name in archive.namelist() if name.lower().endswith(".dll")]
        for index, name in enumerate(names):
            original = archive.read(name)
            target = Path(work) / Path(path).stem / str(index) / Path(name).name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(original)
            signed = runner([sn, "-q", "-R", str(target), str(key_file)], capture_output=True, text=True)
            if signed.returncode != 0:
                problems.append(f"{name}: sn -R failed: {_output(signed)}")
                continue
            verified = runner([sn, "-q", "-vf", str(target)], capture_output=True, text=True)
            if verified.returncode != 0:
                problems.append(f"{name}: the strong-name signature does not verify: {_output(verified)}")
            data = target.read_bytes()
            if len(data) != len(original):
                problems.append(f"{name}: signing changed its size from {len(original)} to {len(data)} bytes")
            if not nupkg.has_public_key(data, public_key):
                problems.append(f"{name}: does not carry the Countly strong-name public key")
            reported = token_of(target, sn, runner)
            if reported != token:
                problems.append(f"{name}: sn -T reports the token {reported}, the Countly token is {token}")
            replacements[name] = data
    if not names:
        problems.append(f"{Path(path).name} holds no assembly to sign")
    if not problems:
        nupkg.replace_entries(path, replacements)
    return problems


def skip_verification_commands(token, sn_paths):
    """Commands that make .NET Framework on this machine load assemblies with the Countly token without checking their
    signature, so the clean projects can run the delay-signed build; one per sn.exe, as each writes its own registry
    view."""
    return [[sn, "-q", "-Vr", f"*,{token}"] for sn in sn_paths]
