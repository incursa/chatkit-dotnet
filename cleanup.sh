#!/usr/bin/env sh
set -eu

repo_root="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

pwsh -NoProfile -File "$repo_root/scripts/setup-git-hooks.ps1"
python -m pre_commit run --hook-stage manual --all-files
