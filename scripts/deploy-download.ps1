# Manda o cliente exportado para a VPS e publica na área de download do site (scripts/deploy-download.sh).
# Requer o OpenSSH do Windows (ssh e scp no PATH) com acesso à VPS.
#
#   .\scripts\export-client.ps1
#   .\scripts\deploy-download.ps1 -Server root@31.97.162.124
#   .\scripts\deploy-download.ps1 -Server root@31.97.162.124 -Version 0.3.0
#
# -Zip      arquivo a publicar (padrão: build\Valdran-windows.zip, a saída do export-client.ps1)
# -Version  versão exibida na página (padrão: data e hora UTC, escolhida na VPS)
# -RepoPath pasta do repositório na VPS (padrão: /root/mmo-rpg)

param(
    [Parameter(Mandatory = $true)]
    [string]$Server,

    [string]$Zip,

    [string]$Version,

    [string]$RepoPath = '/root/mmo-rpg'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if (-not $Zip) { $Zip = Join-Path $root 'build\Valdran-windows.zip' }
if (-not (Test-Path $Zip)) {
    throw "Arquivo não encontrado: $Zip. Rode antes .\scripts\export-client.ps1."
}
if ($Version -and $Version -notmatch '^[A-Za-z0-9._-]+$') {
    throw "Versão inválida: '$Version' (use letras, números, ponto, hífen e sublinhado)."
}

$remote = "/tmp/Valdran-windows-upload-$([guid]::NewGuid().ToString('N')).zip"
$size = '{0:N1} MB' -f ((Get-Item $Zip).Length / 1MB)

Write-Host "==> enviando $Zip ($size) para ${Server}:$remote"
scp $Zip "${Server}:$remote"
if ($LASTEXITCODE -ne 0) { throw 'scp falhou.' }

$versionArg = if ($Version) { " --version '$Version'" } else { '' }
Write-Host '==> publicando na VPS'
ssh $Server "cd '$RepoPath' && ./scripts/deploy-download.sh '$remote'$versionArg; status=`$?; rm -f '$remote'; exit `$status"
if ($LASTEXITCODE -ne 0) { throw 'A publicação na VPS falhou (veja a mensagem acima).' }

Write-Host '==> Pronto: a página de download já mostra a versão nova.'
