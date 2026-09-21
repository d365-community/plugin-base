Clear-Host

Set-Location -Path $PSScriptRoot

Write-Host 'New Bundle'

$bundle = Read-Host -Prompt 'Bundle'
$confirmed = Read-Host -Prompt "Confirm '$bundle' [y/n]"
if ($confirmed -ne "y") {
    exit 0
}

$sharedguid = New-Guid
$yymm = Get-Date -Format 'yyMM'

Write-Host "Shared Guid: $sharedguid"
if (!(Test-Path $PSScriptRoot\Shared\$bundle.Model.json)) {
    Copy-Item -Path $PSScriptRoot\Templates\Assembly.Model.json -Destination $PSScriptRoot\Shared\$bundle.Model.json -force
}
if (!(Test-Path $PSScriptRoot\Shared\$bundle.Model.projitems)) {
    Copy-Item -Path $PSScriptRoot\Templates\Assembly.Model.projitems -Destination $PSScriptRoot\Shared\$bundle.Model.projitems -force
    (Get-Content $PSScriptRoot\Shared\$bundle.Model.projitems) -replace '%GUID%', "$sharedguid" | Set-Content $PSScriptRoot\Shared\$bundle.Model.projitems
}
if (!(Test-Path $PSScriptRoot\Shared\$bundle.Model.shproj)) {
    Copy-Item -Path $PSScriptRoot\Templates\Assembly.Model.shproj -Destination $PSScriptRoot\Shared\$bundle.Model.shproj -force
    (Get-Content $PSScriptRoot\Shared\$bundle.Model.shproj) -replace '%GUID%', "$sharedguid" | Set-Content $PSScriptRoot\Shared\$bundle.Model.shproj
    (Get-Content $PSScriptRoot\Shared\$bundle.Model.shproj) -replace '%ASSEMBLY%', "$bundle" | Set-Content $PSScriptRoot\Shared\$bundle.Model.shproj
}

Write-Host "Version: $yymm"
if (!(Test-Path $PSScriptRoot\Bundles\$bundle)) {
    New-Item -ItemType Directory -Path $PSScriptRoot\Bundles\$bundle
}
if (!(Test-Path $PSScriptRoot\Bundles\$bundle\$bundle.csproj)) {
    Copy-Item -Path $PSScriptRoot\Templates\Bundle.csproj -Destination $PSScriptRoot\Bundles\$bundle\$bundle.csproj -force
    (Get-Content $PSScriptRoot\Bundles\$bundle\$bundle.csproj) -replace '%YYMM%', "$yymm" | Set-Content $PSScriptRoot\Bundles\$bundle\$bundle.csproj
}

if (!(Test-Path $PSScriptRoot\Bundles\$bundle\Plugin)) {
    New-Item -ItemType Directory -Path $PSScriptRoot\Bundles\$bundle\Plugin
}
if (!(Test-Path $PSScriptRoot\Bundles\$bundle\Plugin\_name_.cs)) {
    Copy-Item -Path $PSScriptRoot\Templates\Plugin\_name_.cs -Destination $PSScriptRoot\Bundles\$bundle\Plugin\_name_.cs -force
    (Get-Content $PSScriptRoot\Bundles\$bundle\Plugin\_name_.cs) -replace '%ASSEMBLY%', "$bundle" | Set-Content $PSScriptRoot\Bundles\$bundle\Plugin\_name_.cs
}

if (!(Test-Path $PSScriptRoot\Webresources\Strings\$bundle.1031.resx)) {
    Copy-Item -Path $PSScriptRoot\Templates\Strings\Assembly.resx -Destination $PSScriptRoot\Webresources\Strings\$bundle.1031.resx -force
}
if (!(Test-Path $PSScriptRoot\Webresources\Strings\$bundle.1033.resx)) {
    Copy-Item -Path $PSScriptRoot\Templates\Strings\Assembly.resx -Destination $PSScriptRoot\Webresources\Strings\$bundle.1033.resx -force
}
