# Embedded model downloader (simplified)
# Expects environment variables:
#   MODELS_TARGET_DIR  -> directory to place models
#   MODELS_SPEC        -> JSON array: [{"name":"FaceDetector","url":"...","fileName":"retinaface.onnx"}, ...]
#   MODEL_FETCH_VERBOSE -> true/false (default true)
#   NO_PROGRESS        -> true disables progress bar

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
try { Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue } catch { }
$httpClientAvailable = ([Type]::GetType('System.Net.Http.HttpClient, System.Net.Http') -ne $null)

function Log($msg,[ConsoleColor]$color=[ConsoleColor]::DarkGray){ $ts=(Get-Date).ToString('HH:mm:ss'); Write-Host "[$ts] $msg" -ForegroundColor $color }
$verbose = $true
if ($env:MODEL_FETCH_VERBOSE) { if ($env:MODEL_FETCH_VERBOSE.ToLower() -in @('false','0','off')) { $verbose = $false } }
$noProgress = ($env:NO_PROGRESS -and $env:NO_PROGRESS.ToLower() -eq 'true')

$target = $env:MODELS_TARGET_DIR
if ([string]::IsNullOrWhiteSpace($target)) { Log 'ERROR: MODELS_TARGET_DIR not set.' Red; exit 1 }
if (-not (Test-Path $target)) { Log "Creating directory $target" Cyan; New-Item -ItemType Directory -Force -Path $target | Out-Null }

$specRaw = $env:MODELS_SPEC
if ([string]::IsNullOrWhiteSpace($specRaw)) { Log 'Nothing to download (MODELS_SPEC empty).' Yellow; exit 0 }

try { $models = $specRaw | ConvertFrom-Json } catch { Log "Invalid MODELS_SPEC JSON: $_" Red; exit 2 }
if (-not $models) { Log 'Parsed empty models list.' Yellow; exit 0 }

function Download-One($entry){
    $name = $entry.name; $url = $entry.url; $fileName = $entry.fileName
    $outPath = Join-Path $target $fileName
    Log "START $name -> $fileName" Green
    if ($verbose){ Log "URL: $url" DarkGray }
    if (-not $httpClientAvailable){ Invoke-WebRequest -Uri $url -OutFile $outPath -UseBasicParsing; Log "DONE (Invoke-WebRequest) $fileName" Cyan; return }
    $handler = New-Object System.Net.Http.HttpClientHandler
    $client = New-Object System.Net.Http.HttpClient($handler)
    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get,$url)
    $sw=[System.Diagnostics.Stopwatch]::StartNew()
    $response = $client.SendAsync($request,[System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).Result
    if (-not $response.IsSuccessStatusCode){ throw "HTTP $($response.StatusCode)" }
    $len = $response.Content.Headers.ContentLength
    $stream = $response.Content.ReadAsStreamAsync().Result
    $fs = [System.IO.File]::Open($outPath,[System.IO.FileMode]::Create,[System.IO.FileAccess]::Write,[System.IO.FileShare]::None)
    $buf = New-Object byte[] 131072
    $total=0; $lastReport=0
    try {
        while($true){ $r=$stream.Read($buf,0,$buf.Length); if($r -le 0){break}; $fs.Write($buf,0,$r); $total+=$r; if(-not $noProgress){ if($len -and $len -gt 0){ $pct=[int](($total/$len)*100); if($total - $lastReport -ge 1024*1024){ $speed=[math]::Round(($total/1024)/$sw.Elapsed.TotalSeconds,2); Write-Progress -Activity "Pobieranie $fileName" -Status "$pct% $speed KB/s" -PercentComplete $pct; $lastReport=$total } } else { if($total - $lastReport -ge 1024*1024){ $speed=[math]::Round(($total/1024)/$sw.Elapsed.TotalSeconds,2); Write-Progress -Activity "Pobieranie $fileName" -Status "$total bytes $speed KB/s" -PercentComplete 0; $lastReport=$total } } } }
    }
    finally { $fs.Flush(); $fs.Dispose(); $stream.Dispose(); $client.Dispose(); if(-not $noProgress){ Write-Progress -Activity "Pobieranie $fileName" -Completed } }
    $sw.Stop()
    $finalSize = (Get-Item $outPath).Length
    $speedAvg = if($sw.Elapsed.TotalSeconds -gt 0){ [math]::Round(($finalSize/1024)/$sw.Elapsed.TotalSeconds,2)} else {0}
    if($len -and $finalSize -ne $len){ Log "WARN size mismatch expected=$len got=$finalSize" Yellow }
    Log "DONE $fileName size=$finalSize bytes avgSpeed=${speedAvg}KB/s" Cyan
}

$failures=0
foreach($m in $models){ try { Download-One $m } catch { Log "FAIL $($m.fileName): $_" Red; $failures++ } }

# Manifest
$manifest = @()
Get-ChildItem -Path $target -Filter *.onnx | ForEach-Object { $hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(); $manifest += [pscustomobject]@{ file=$_.Name; sha256=$hash; bytes=$_.Length } }
$manifestPath = Join-Path $target 'checksums.json'
$manifest | ConvertTo-Json -Depth 4 | Out-File -FilePath $manifestPath -Encoding UTF8
Log "Manifest written: $manifestPath" Cyan
if($failures -gt 0){ Log "Completed with $failures failure(s)" Yellow } else { Log "All downloads successful" Green }
exit $failures
