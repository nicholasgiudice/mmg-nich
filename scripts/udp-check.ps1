# Envia um pacote UDP para a VPS, para conferir se a porta do jogo passa pelo firewall.
# Na VPS, deixe escutando antes:  nc -u -l 7777
# No Windows:                      .\scripts\udp-check.ps1 -HostName <ip-da-vps>
# Se o texto aparecer no terminal do nc, a porta está aberta de ponta a ponta.

param(
    [Parameter(Mandatory = $true)]
    [string]$HostName,

    [int]$Port = 7777,

    [string]$Message = "udp-check $env:COMPUTERNAME $(Get-Date -Format o)"
)

$ErrorActionPreference = 'Stop'

$client = New-Object System.Net.Sockets.UdpClient
try {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Message + "`n")
    $sent = $client.Send($bytes, $bytes.Length, $HostName, $Port)
    Write-Host "Enviados $sent bytes para ${HostName}:$Port -> '$Message'"
    Write-Host "Confira se a mensagem apareceu no 'nc -u -l $Port' da VPS."
}
finally {
    $client.Close()
}
