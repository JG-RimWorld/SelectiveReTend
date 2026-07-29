#!/usr/bin/env python3
"""Create and validate an installable Selective ReTend release archive."""

from __future__ import annotations

import argparse
import re
import shutil
import sys
import zipfile
from pathlib import Path


MOD_FOLDER = "SelectiveReTend"
RELEASE_DIRECTORIES = ("About", "Languages", "1.6")
RELEASE_FILES = (
    "LoadFolders.xml",
    "LICENSE",
    "README.md",
    "CHANGELOG.md",
    "VERSION.txt",
)
REQUIRED_ARCHIVE_FILES = (
    f"{MOD_FOLDER}/About/About.xml",
    f"{MOD_FOLDER}/About/Preview.png",
    f"{MOD_FOLDER}/1.6/Assemblies/SelectiveReTend.dll",
    f"{MOD_FOLDER}/LoadFolders.xml",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--version",
        help="Release version. Defaults to the value in VERSION.txt.",
    )
    return parser.parse_args()


def read_version(root: Path, supplied_version: str | None) -> str:
    file_version = (root / "VERSION.txt").read_text(encoding="utf-8").strip()
    version = (supplied_version or file_version).removeprefix("v")

    if version != file_version:
        raise ValueError(
            f"Requested version {version!r} does not match VERSION.txt "
            f"({file_version!r})."
        )
    if not re.fullmatch(r"\d+\.\d+\.\d+", version):
        raise ValueError(
            f"Version {version!r} must use the MAJOR.MINOR.PATCH format."
        )
    return version


def ignore_development_files(
    _directory: str, names: list[str]
) -> set[str]:
    return {
        name
        for name in names
        if name == ".gitkeep" or name.lower().endswith(".pdb")
    }


def create_archive(root: Path, version: str) -> Path:
    artifacts = root / "artifacts"
    staging = artifacts / "staging"
    package_root = staging / MOD_FOLDER
    archive = artifacts / f"{MOD_FOLDER}-v{version}.zip"

    if artifacts.exists():
        shutil.rmtree(artifacts)
    package_root.mkdir(parents=True)

    for directory_name in RELEASE_DIRECTORIES:
        source = root / directory_name
        if not source.is_dir():
            raise FileNotFoundError(f"Required directory is missing: {source}")
        shutil.copytree(
            source,
            package_root / directory_name,
            ignore=ignore_development_files,
        )

    for file_name in RELEASE_FILES:
        source = root / file_name
        if not source.is_file():
            raise FileNotFoundError(f"Required file is missing: {source}")
        shutil.copy2(source, package_root / file_name)

    assembly = package_root / "1.6/Assemblies/SelectiveReTend.dll"
    if not assembly.is_file() or assembly.stat().st_size == 0:
        raise FileNotFoundError(
            "The compiled DLL is missing. Build the Release configuration "
            "before packaging."
        )

    with zipfile.ZipFile(
        archive, mode="w", compression=zipfile.ZIP_DEFLATED, compresslevel=9
    ) as zip_file:
        for path in sorted(package_root.rglob("*")):
            if path.is_file():
                zip_file.write(path, path.relative_to(staging))

    validate_archive(archive)
    return archive


def validate_archive(archive: Path) -> None:
    with zipfile.ZipFile(archive) as zip_file:
        bad_file = zip_file.testzip()
        if bad_file is not None:
            raise ValueError(f"Corrupt file in release archive: {bad_file}")

        archive_files = set(zip_file.namelist())
        missing = set(REQUIRED_ARCHIVE_FILES) - archive_files
        if missing:
            missing_list = ", ".join(sorted(missing))
            raise ValueError(f"Release archive is missing: {missing_list}")

        forbidden_prefixes = (
            f"{MOD_FOLDER}/Source/",
            f"{MOD_FOLDER}/Workshop/",
            f"{MOD_FOLDER}/scripts/",
            f"{MOD_FOLDER}/.github/",
        )
        forbidden = sorted(
            name
            for name in archive_files
            if name.startswith(forbidden_prefixes)
        )
        if forbidden:
            raise ValueError(
                "Development files were included in the release archive: "
                + ", ".join(forbidden)
            )


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    try:
        version = read_version(root, parse_args().version)
        archive = create_archive(root, version)
    except (OSError, ValueError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print(f"Created and validated: {archive}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
