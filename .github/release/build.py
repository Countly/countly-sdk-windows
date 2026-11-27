"""Command lines that compile and pack each package of a plan, delay-signed with the Countly strong-name public key."""

import re
import shutil
from pathlib import Path

REFERENCE_ASSEMBLIES_VERSION = "1.0.3"
PACK_PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <NoBuild>true</NoBuild>
  </PropertyGroup>
</Project>
"""


class BuildError(Exception):
    """A repository state the build cannot pack from."""


def write_public_key(config, path):
    """Writes the committed strong-name public key to the key file the compilers delay-sign with."""
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(bytes.fromhex(config["strongName"]["publicKey"]))
    return path


def signing_properties(public_key, prefix):
    """MSBuild properties that delay-sign with the public key, so the assemblies carry the Countly strong-name identity
    while the private key stays out of the build; the publish job signs them after approval."""
    return [
        f"{prefix}SignAssembly=true", f"{prefix}DelaySign=true", f"{prefix}AssemblyOriginatorKeyFile={public_key}",
        f"{prefix}ContinuousIntegrationBuild=true",
    ]


def built_outputs(config, key, repo_root):
    """The layout files of a package that its compile produces, removed before compiling so a stale copy cannot ship."""
    return [Path(repo_root) / entry["from"] for entry in config["packages"][key].get("layout", []) if entry.get("built")]


def compile_commands(config, key, repo_root, public_key, work):
    """Commands that compile a package's assemblies: packages.config restores and reference assembly packages for the
    classic projects, built with MSBuild, and dotnet for the SDK-style ones. An empty list for a package that dotnet
    pack compiles itself."""
    root = Path(repo_root)
    references = Path(work) / "refs"
    commands = []
    for step in config["packages"][key].get("compile", []):
        project = str(root / step["project"])
        if step["tool"] != "msbuild":
            commands.append(["dotnet", "build", project, "-c", "Release", "--nologo", "--no-incremental", *signing_properties(public_key, "-p:")])
            continue
        extra = []
        if step.get("packagesConfig"):
            commands.append(["nuget", "restore", str(root / step["packagesConfig"]), "-PackagesDirectory", str(root / step["packagesDirectory"]), "-NonInteractive"])
        if step.get("referenceAssemblies"):
            name = f"Microsoft.NETFramework.ReferenceAssemblies.{step['referenceAssemblies']}"
            commands.append(["nuget", "install", name, "-Version", REFERENCE_ASSEMBLIES_VERSION, "-ExcludeVersion", "-OutputDirectory", str(references), "-NonInteractive"])
            extra.append(f"/p:CustomAfterMicrosoftCommonTargets={references / name / 'build' / (name + '.targets')}")
        commands.append(["msbuild", project, "/t:Rebuild", "/p:Configuration=Release", "/nologo", "/v:minimal", *signing_properties(public_key, "/p:"), *extra])
    return commands


def stage_nuspec(config, key, plan, repo_root, layout_dir):
    """Copies a package's nuspec into its layout folder with the plan's package version and returns the copy."""
    source = Path(repo_root) / config["packages"][key]["nuspec"]
    text, count = re.subn(r"<version>[^<]*</version>", f"<version>{plan.package_version}</version>", source.read_text(encoding="utf-8-sig"), count=1)
    if count != 1:
        raise BuildError(f"{config['packages'][key]['nuspec']}: no <version> element")
    target = Path(layout_dir) / source.name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(text, encoding="utf-8")
    return target


def assemble_layout(config, key, repo_root, layout_dir):
    """Copies every layout file of a package from where the build put it into the layout folder; returns problems for
    files the build did not produce."""
    problems = []
    for entry in config["packages"][key].get("layout", []):
        source = Path(repo_root) / entry["from"]
        if not source.is_file():
            problems.append(f"{entry['from']} does not exist; the build did not produce it")
            continue
        target = Path(layout_dir) / entry["path"]
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    return problems


def write_pack_project(work):
    """Writes the project that packs a nuspec without building anything, outside the repository so its obj folder does
    not land in the checkout."""
    project = Path(work) / "pack" / "Pack.csproj"
    project.parent.mkdir(parents=True, exist_ok=True)
    project.write_text(PACK_PROJECT, encoding="utf-8")
    return project


def pack_nuspec_command(pack_project, nuspec, layout_dir, output_dir):
    """Packs a nuspec whose file paths are relative to the layout folder."""
    return ["dotnet", "pack", str(pack_project), "--nologo", "-o", str(output_dir), f"-p:NuspecFile={nuspec}", f"-p:NuspecBasePath={layout_dir}"]


def pack_project_command(config, key, plan, repo_root, public_key, output_dir):
    """Compiles and packs an SDK-style package project; a test run overrides only the package version, so the
    assemblies are the ones a production release would ship."""
    spec = config["packages"][key]
    command = [
        "dotnet", "pack", str(Path(repo_root) / spec["project"]), "-c", "Release", "--nologo", "-o", str(output_dir),
        "-p:EnableWindowsTargeting=true", *[f"-p:{value}" for value in spec.get("packProperties", [])],
        *signing_properties(public_key, "-p:"),
    ]
    if plan.package_version != plan.version:
        command.append(f"-p:PackageVersion={plan.package_version}")
    return command
