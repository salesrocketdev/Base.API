#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  ./scripts/rename-project.sh <new-name> [old-name] [--dry-run] [--code-only] [--skip-validation]

Examples:
  ./scripts/rename-project.sh Eradia
  ./scripts/rename-project.sh Eradia Base --dry-run --code-only
EOF
}

DRY_RUN=0
CODE_ONLY=0
SKIP_VALIDATION=0
POSITIONAL=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    -n|--dry-run)
      DRY_RUN=1
      shift
      ;;
    --code-only)
      CODE_ONLY=1
      shift
      ;;
    --skip-validation)
      SKIP_VALIDATION=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      POSITIONAL+=("$1")
      shift
      ;;
  esac
done

set -- "${POSITIONAL[@]}"

if [[ $# -lt 1 || $# -gt 2 ]]; then
  usage
  exit 1
fi

NEW_NAME="$1"
OLD_NAME="${2:-Base}"

if [[ "$NEW_NAME" == "$OLD_NAME" ]]; then
  echo "Error: new-name and old-name cannot be equal." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SELF_PATH="$SCRIPT_DIR/rename-project.sh"

cd "$REPO_ROOT"

should_skip_path() {
  local path="$1"
  case "$path" in
    */.git/*|*/.codex/*|*/.config/*|*/.vs/*|*/bin/*|*/obj/*)
      return 0
      ;;
    *)
      return 1
      ;;
  esac
}

matches_prefix() {
  local name="$1"
  if [[ "$name" == "$OLD_NAME" || "$name" == "$OLD_NAME."* || "$name" == "$OLD_NAME_"* || "$name" == "$OLD_NAME-"* ]]; then
    return 0
  fi
  return 1
}

is_text_file() {
  local file="$1"
  local base
  local ext

  base="$(basename "$file")"

  case "$base" in
    Dockerfile)
      return 0
      ;;
  esac

  ext="${base##*.}"
  if [[ "$base" == "$ext" ]]; then
    return 1
  fi

  case ".${ext}" in
    .cs|.csproj|.sln|.props|.targets|.json|.yml|.yaml|.sh|.ps1|.config|.xml|.http)
      return 0
      ;;
    .md|.txt|.env|.example|.dockerignore|.gitignore|.editorconfig)
      if [[ "$CODE_ONLY" -eq 0 ]]; then
        return 0
      fi
      return 1
      ;;
    *)
      return 1
      ;;
  esac
}

rename_item() {
  local path="$1"
  local kind="$2"
  local base
  local dir
  local suffix
  local new_base
  local target

  base="$(basename "$path")"
  dir="$(dirname "$path")"

  if ! matches_prefix "$base"; then
    return 0
  fi

  suffix="${base:${#OLD_NAME}}"
  new_base="${NEW_NAME}${suffix}"
  target="$dir/$new_base"

  if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "[dry-run] Rename $kind: $path -> $target"
  else
    mv "$path" "$target"
  fi
}

replace_in_file() {
  local file="$1"

  if [[ "$file" == "$SELF_PATH" ]]; then
    return 0
  fi

  if ! is_text_file "$file"; then
    return 0
  fi

  if ! grep -F -q -w -- "$OLD_NAME" "$file"; then
    return 0
  fi

  if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "[dry-run] Replace word '$OLD_NAME' -> '$NEW_NAME' in: $file"
  else
    OLD_NAME="$OLD_NAME" NEW_NAME="$NEW_NAME" perl -0777 -i.bak -pe 's/\b\Q$ENV{OLD_NAME}\E\b/$ENV{NEW_NAME}/g' "$file"
    rm -f "$file.bak"
  fi
}

renamed_dirs=0
renamed_files=0
updated_files=0

while IFS= read -r -d '' dir; do
  if should_skip_path "$dir"; then
    continue
  fi

  before="$dir"
  rename_item "$dir" "directory"
  after_name="$(basename "$before")"
  if matches_prefix "$after_name"; then
    renamed_dirs=$((renamed_dirs + 1))
  fi
done < <(find "$REPO_ROOT" -depth -type d -print0)

while IFS= read -r -d '' file; do
  if should_skip_path "$file"; then
    continue
  fi

  before="$file"
  rename_item "$file" "file"
  after_name="$(basename "$before")"
  if matches_prefix "$after_name"; then
    renamed_files=$((renamed_files + 1))
  fi
done < <(find "$REPO_ROOT" -type f -print0)

while IFS= read -r -d '' file; do
  if should_skip_path "$file"; then
    continue
  fi

  if [[ "$file" == "$SELF_PATH" ]]; then
    continue
  fi

  if is_text_file "$file" && grep -F -q -w -- "$OLD_NAME" "$file"; then
    replace_in_file "$file"
    updated_files=$((updated_files + 1))
  fi
done < <(find "$REPO_ROOT" -type f -print0)

echo "Rename complete."
echo "Directories renamed: $renamed_dirs"
echo "Files renamed: $renamed_files"
echo "Files updated: $updated_files"

if [[ "$DRY_RUN" -eq 0 && "$SKIP_VALIDATION" -eq 0 ]]; then
  if command -v dotnet >/dev/null 2>&1; then
    sln_file="$(find "$REPO_ROOT" -maxdepth 1 -type f -name '*.sln' | head -n 1)"
    if [[ -n "$sln_file" ]]; then
      echo "Running validation: dotnet build $(basename "$sln_file")"
      dotnet build "$sln_file" -v minimal
      echo "Running validation: dotnet test $(basename "$sln_file")"
      dotnet test "$sln_file" -v minimal
    else
      echo "No .sln found. Skipping validation."
    fi
  else
    echo "dotnet not found in PATH. Skipping validation."
  fi
fi
