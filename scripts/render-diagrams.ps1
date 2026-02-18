#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Resolve-Path (Join-Path $ScriptDir '..')
$PlantUmlDir = Join-Path $RootDir 'docs/diagrams/plantuml'
$OutSvg = Join-Path $RootDir 'docs/diagrams/out/svg'
$OutPng = Join-Path $RootDir 'docs/diagrams/out/png'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error 'Docker is required to render PlantUML diagrams. Install Docker and ensure it is in PATH.'
    exit 1
}

try {
    docker info *> $null
    if ($LASTEXITCODE -ne 0) { throw 'docker info failed' }
}
catch {
    Write-Error 'Docker daemon is not reachable. Start Docker and retry.'
    exit 1
}

New-Item -ItemType Directory -Force -Path $OutSvg | Out-Null
New-Item -ItemType Directory -Force -Path $OutPng | Out-Null

Write-Host "Rendering SVG diagrams from $PlantUmlDir ..."
docker run --rm `
  -v "${RootDir}:/workspace" `
  -w /workspace `
  plantuml/plantuml:latest `
  -tsvg -o ../out/svg docs/diagrams/plantuml/*.puml
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Rendering PNG diagrams from $PlantUmlDir ..."
docker run --rm `
  -v "${RootDir}:/workspace" `
  -w /workspace `
  plantuml/plantuml:latest `
  -tpng -o ../out/png docs/diagrams/plantuml/*.puml
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Done. Outputs written to:'
Write-Host "  - $OutSvg"
Write-Host "  - $OutPng"
