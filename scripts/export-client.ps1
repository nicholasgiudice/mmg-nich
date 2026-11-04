# Exporta o cliente para Windows e gera um .zip pronto para enviar a quem vai jogar.
# Requer o Godot 4 .NET e os modelos de exportação da mesma versão
# (no editor: Editor > Gerenciar Modelos de Exportação > Baixar e Instalar).
#
#   .\scripts\export-client.ps1
#   .\scripts\export-client.ps1 -Godot C:\caminho\Godot_v4.7.2-stable_mono_win64_console.exe
#
# Saída: build\windows\Valdran.exe (arquivo único) e build\Valdran-windows.zip.

param(
    [string]$Godot,

    [string]$Preset = 'Windows Desktop'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$client = Join-Path $root 'client'
$outDir = Join-Path $root 'build\windows'
$exe = Join-Path $outDir 'Valdran.exe'
$zip = Join-Path $root 'build\Valdran-windows.zip'

if (-not $Godot) {
    # A versão _console espera o export terminar e mostra a saída no terminal.
    $found = Get-Command 'Godot*_console.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $found) {
        throw 'Godot não encontrado no PATH. Passe -Godot <caminho do Godot_*_console.exe da edição .NET>.'
    }
    $Godot = $found.Source
}

$version = (& $Godot --version | Select-Object -First 1).Trim()
if ($version -notmatch '^(\d+\.\d+(\.\d+)?)\.(\w+)\.mono') {
    throw "O Godot em '$Godot' ($version) não é a edição .NET."
}
$templateDir = Join-Path $env:APPDATA "Godot\export_templates\$($Matches[1]).$($Matches[3]).mono"
if (-not (Test-Path (Join-Path $templateDir 'windows_release_x86_64.exe'))) {
    throw "Modelos de exportação não instalados em '$templateDir'. No editor: Editor > Gerenciar Modelos de Exportação > Baixar e Instalar."
}

# O build do cliente copia ../data para client/data, que o export empacota.
Write-Host '==> dotnet build client/'
dotnet build $client -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet build falhou.' }

if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Force $outDir | Out-Null

Write-Host "==> Godot $version --export-release '$Preset'"
& $Godot --headless --path $client --export-release $Preset $exe
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exe)) { throw 'Export do Godot falhou.' }

if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zip

$size = '{0:N1} MB' -f ((Get-Item $zip).Length / 1MB)
Write-Host "==> Pronto: $zip ($size)"
