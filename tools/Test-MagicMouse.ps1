[CmdletBinding()]
param(
    [switch]$Capture,
    [switch]$AcknowledgeExplicitCapture,
    [string]$DevicePath,
    [ValidateRange(1, 600)]
    [int]$DurationSeconds = 10,
    [ValidateRange(1, 10000)]
    [int]$MaxFrames = 2000,
    [ValidateLength(1, 512)]
    [string]$Scenario = 'initial-touch-smoke-test',
    [string]$Output = (Join-Path $PWD 'captures\initial-touch-smoke-test.json')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$inspectorProject = Join-Path $root 'tools\HidInspector\MagicMouseWindows.HidInspector.csproj'
$recorderProject = Join-Path $root 'tools\ReportRecorder\MagicMouseWindows.ReportRecorder.csproj'

dotnet build $inspectorProject --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    throw 'HidInspector build failed.'
}

$inspectionJson = dotnet run --project $inspectorProject --no-build -- --apple-only --json
if ($LASTEXITCODE -ne 0 -and [string]::IsNullOrWhiteSpace(($inspectionJson -join ''))) {
    throw 'HidInspector failed.'
}

$inspection = $inspectionJson | ConvertFrom-Json
$candidates = @($inspection.Devices)

if ($candidates.Count -eq 0) {
    Write-Host 'No present Apple HID interface was found.'
    Write-Host 'Connect or pair the Magic Mouse, confirm it appears in Windows Bluetooth settings, then run this command again.'
    exit 3
}

$candidates |
    Select-Object Product, Manufacturer, VendorId, ProductId, UsagePage, Usage,
        InputReportByteLength, CanOpenForRead, DevicePath |
    Format-Table -AutoSize

if (-not $Capture) {
    Write-Host ''
    Write-Host 'Inspection complete. Add -Capture to start an explicit bounded raw-report capture.'
    exit 0
}

if (-not $AcknowledgeExplicitCapture) {
    Write-Host 'Capture was not started. Add -AcknowledgeExplicitCapture to confirm bounded raw HID recording.'
    exit 4
}

if ([string]::IsNullOrWhiteSpace($DevicePath)) {
    Write-Host 'Capture requires -DevicePath with an explicitly reviewed Apple HID interface shown above.'
    exit 4
}

$matches = @($candidates | Where-Object {
    [string]::Equals($_.DevicePath, $DevicePath, [StringComparison]::OrdinalIgnoreCase)
})
if ($matches.Count -ne 1) {
    Write-Host 'The supplied -DevicePath is not one of the present Apple HID interfaces shown above.'
    exit 4
}

$selected = $matches[0]

if (-not $selected.CanOpenForRead) {
    Write-Host 'The selected Apple HID interface cannot be opened for read access. Select a readable vendor-defined collection if one is present.'
    exit 4
}

$eligibleUsage = ($selected.UsagePage -eq 1 -and
    ($selected.Usage -eq 1 -or $selected.Usage -eq 2)) -or
    $selected.UsagePage -ge 0xFF00
if (-not $selected.Candidate.IsCandidate -or -not $eligibleUsage) {
    Write-Host 'The selected interface is not an eligible Apple pointing or vendor-defined collection.'
    exit 4
}

dotnet build $recorderProject --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    throw 'ReportRecorder build failed.'
}

$resolvedOutput = [IO.Path]::GetFullPath($Output)
Write-Host "Capturing only: $($selected.DevicePath)"
Write-Host "Use the mouse for up to $DurationSeconds seconds. Press Ctrl+C to stop early."
Write-Host 'Read-only mode may expose only standard mouse reports; touch reports can require a separately validated initialization sequence.'

dotnet run --project $recorderProject --no-build -- `
    --device-path $selected.DevicePath `
    --scenario $Scenario `
    --duration-seconds $DurationSeconds `
    --max-frames $MaxFrames `
    --output $resolvedOutput `
    --acknowledge-explicit-capture

if ($LASTEXITCODE -ne 0) {
    throw "ReportRecorder failed with exit code $LASTEXITCODE."
}

Write-Host "Capture complete: $resolvedOutput"
