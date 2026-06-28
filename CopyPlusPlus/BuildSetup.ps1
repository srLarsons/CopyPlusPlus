param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"
$publish = Join-Path $dist "publish"
$setupWork = Join-Path $dist "setup-work"
$payload = Join-Path $setupWork "CopyPlusPlusPayload.zip"
$setupBatch = Join-Path $setupWork "SetupCopyPlusPlus.bat"
$sedPath = Join-Path $setupWork "CopyPlusPlusSetup.sed"
$setupExe = Join-Path $dist "CopyPlusPlusSetup.exe"

function Reset-Folder([string]$path) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

Reset-Folder $publish
Reset-Folder $setupWork
if (Test-Path -LiteralPath $setupExe) {
    Remove-Item -LiteralPath $setupExe -Force
}

dotnet publish (Join-Path $root "WpfMultiCopyClipboard.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    -p:PublishSingleFile=false `
    -o $publish

Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $payload -Force
Copy-Item -LiteralPath (Join-Path $root "SetupCopyPlusPlus.bat") -Destination $setupBatch -Force

$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=$setupExe
FriendlyName=copy++ Setup
AppLaunched=cmd /c SetupCopyPlusPlus.bat
PostInstallCmd=<None>
AdminQuietInstCmd=cmd /c SetupCopyPlusPlus.bat
UserQuietInstCmd=cmd /c SetupCopyPlusPlus.bat
SourceFiles=SourceFiles
[SourceFiles]
SourceFiles0=$setupWork\
[SourceFiles0]
%FILE0%=
%FILE1%=
[Strings]
FILE0="SetupCopyPlusPlus.bat"
FILE1="CopyPlusPlusPayload.zip"
"@

Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

$iexpress = Join-Path $env:WINDIR "System32\iexpress.exe"
if (-not (Test-Path -LiteralPath $iexpress)) {
    throw "IExpress was not found at $iexpress"
}

& $iexpress /N /Q $sedPath

for ($attempt = 0; $attempt -lt 20 -and -not (Test-Path -LiteralPath $setupExe); $attempt++) {
    Start-Sleep -Milliseconds 500
}

if (-not (Test-Path -LiteralPath $setupExe)) {
    throw "Setup file was not created: $setupExe"
}

Write-Host "Created $setupExe"
