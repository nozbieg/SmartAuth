#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PLANTUML_DIR="$ROOT_DIR/docs/diagrams/plantuml"
OUT_DIR="$ROOT_DIR/docs/diagrams/out"
OUT_SVG="$OUT_DIR/svg"
OUT_PNG="$OUT_DIR/png"

if ! command -v docker >/dev/null 2>&1; then
  echo "Error: Docker is required to render PlantUML diagrams. Install Docker and ensure it is in PATH." >&2
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Error: Docker daemon is not reachable. Start Docker and retry." >&2
  exit 1
fi

mkdir -p "$OUT_SVG" "$OUT_PNG"

echo "Rendering SVG diagrams from $PLANTUML_DIR ..."
docker run --rm \
  -v "$ROOT_DIR:/workspace" \
  -w /workspace \
  plantuml/plantuml:latest \
  -tsvg -o ../out/svg docs/diagrams/plantuml/*.puml

echo "Rendering PNG diagrams from $PLANTUML_DIR ..."
docker run --rm \
  -v "$ROOT_DIR:/workspace" \
  -w /workspace \
  plantuml/plantuml:latest \
  -tpng -o ../out/png docs/diagrams/plantuml/*.puml

echo "Done. Outputs written to:"
echo "  - $OUT_SVG"
echo "  - $OUT_PNG"
