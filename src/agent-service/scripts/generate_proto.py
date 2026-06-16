"""Generate Python gRPC stubs for the Agent Service from the shared protobuf contract.

Usage (from the repository root):
    python src/agent-service/scripts/generate_proto.py

Reads ``proto/agent-service.proto`` (the single source of truth, shared with the
.NET API client) and writes generated stub modules into
``src/agent-service/app/grpc/generated/``, creating package ``__init__.py``
files as needed.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

# Repo layout: <repo_root>/src/agent-service/scripts/generate_proto.py
SCRIPT_DIR = Path(__file__).resolve().parent
AGENT_SERVICE_DIR = SCRIPT_DIR.parent
REPO_ROOT = AGENT_SERVICE_DIR.parent.parent

PROTO_DIR = REPO_ROOT / "proto"
PROTO_FILE = PROTO_DIR / "agent-service.proto"
OUTPUT_DIR = AGENT_SERVICE_DIR / "app" / "grpc" / "generated"


def ensure_package_init(directory: Path) -> None:
    """Create an empty __init__.py in directory if it doesn't already exist."""
    init_file = directory / "__init__.py"
    if not init_file.exists():
        init_file.write_text("", encoding="utf-8")
        print(f"Created package marker: {init_file.relative_to(REPO_ROOT)}")


def main() -> int:
    if not PROTO_FILE.exists():
        print(f"ERROR: proto file not found at {PROTO_FILE}", file=sys.stderr)
        return 1

    # Ensure the output package (and its parent gRPC package) exist with
    # __init__.py markers so generated modules are importable.
    grpc_pkg_dir = OUTPUT_DIR.parent
    grpc_pkg_dir.mkdir(parents=True, exist_ok=True)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    ensure_package_init(grpc_pkg_dir)
    ensure_package_init(OUTPUT_DIR)

    cmd = [
        sys.executable,
        "-m",
        "grpc_tools.protoc",
        f"-I{PROTO_DIR}",
        f"--python_out={OUTPUT_DIR}",
        f"--grpc_python_out={OUTPUT_DIR}",
        str(PROTO_FILE),
    ]

    print(f"Generating Python gRPC stubs from {PROTO_FILE.relative_to(REPO_ROOT)} ...")
    print(f"  -> {OUTPUT_DIR.relative_to(REPO_ROOT)}")
    result = subprocess.run(cmd, cwd=REPO_ROOT)
    if result.returncode != 0:
        print("ERROR: protoc generation failed", file=sys.stderr)
        return result.returncode

    generated_files = sorted(OUTPUT_DIR.glob("*_pb2*.py"))
    if not generated_files:
        print("ERROR: no generated stub files were produced", file=sys.stderr)
        return 1

    fix_grpc_module_imports(generated_files)

    print("Generated files:")
    for f in generated_files:
        print(f"  - {f.relative_to(REPO_ROOT)}")

    return 0


def fix_grpc_module_imports(generated_files: list[Path]) -> None:
    """Rewrite protoc's flat `import X_pb2` statements to package-relative imports.

    grpc_tools.protoc always emits top-level imports between generated modules
    (e.g. ``import agent_service_pb2 as agent__service__pb2``), which only work
    if the output directory is on sys.path directly. Since the stubs live in
    the ``app.grpc.generated`` package, rewrite those imports to be relative
    so the modules are importable as part of the package.
    """
    pb2_module_names = {f.stem for f in generated_files if f.stem.endswith("_pb2")}
    for f in generated_files:
        text = f.read_text(encoding="utf-8")
        original = text
        for module_name in pb2_module_names:
            text = text.replace(
                f"import {module_name} as ",
                f"from . import {module_name} as ",
            )
        if text != original:
            f.write_text(text, encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
