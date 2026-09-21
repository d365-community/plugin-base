Clear-Host

Set-Location -Path $PSScriptRoot

Write-Host 'New Plugin'

$plugin = Read-Host -Prompt 'Name'
$confirmed = Read-Host -Prompt "Confirm '$plugin' [y/n]"
if ($confirmed -ne "y") {
    exit 0
}

$sharedguid = New-Guid
$pluginguid = New-Guid
$yymm = Get-Date -Format 'yyMM'
$yyyy = Get-Date -Format 'yyyy'

Write-Host "Shared Guid: $sharedguid"
if (!(Test-Path $PSScriptRoot\Shared\$plugin.Model.json)) {
    Copy-Item -Path $PSScriptRoot\Templates\Assembly.Model.json -Destination $PSScriptRoot\Shared\$plugin.Model.json -force
}
if (!(Test-Path $PSScriptRoot\Shared\$plugin.Model.projitems)) {
    Copy-Item -Path $PSScriptRoot\Templates\Assembly.Model.projitems -Destination $PSScriptRoot\Shared\$plugin.Model.projitems -force
    (Get-Content $PSScriptRoot\Shared\$plugin.Model.projitems) -replace '%GUID%', "$sharedguid" | Set-Content $PSScriptRoot\Shared\$plugin.Model.projitems
}
if (!(Test-Path $PSScriptRoot\Shared\$plugin.Model.shproj)) {
    Copy-Item -Path $PSScriptRoot\Templates\Assembly.Model.shproj -Destination $PSScriptRoot\Shared\$plugin.Model.shproj -force
    (Get-Content $PSScriptRoot\Shared\$plugin.Model.shproj) -replace '%GUID%', "$sharedguid" | Set-Content $PSScriptRoot\Shared\$plugin.Model.shproj
    (Get-Content $PSScriptRoot\Shared\$plugin.Model.shproj) -replace '%ASSEMBLY%', "$plugin" | Set-Content $PSScriptRoot\Shared\$plugin.Model.shproj
}

Write-Host "Plugin Guid: $pluginguid"
Write-Host "Version: $yymm"
if (!(Test-Path $PSScriptRoot\Plugins\$plugin)) {
    New-Item -ItemType Directory -Path $PSScriptRoot\Plugins\$plugin
}
if (!(Test-Path $PSScriptRoot\Plugins\$plugin\$plugin.csproj)) {
    Copy-Item -Path $PSScriptRoot\Templates\Plugin.csproj -Destination $PSScriptRoot\Plugins\$plugin\$plugin.csproj -force
    (Get-Content $PSScriptRoot\Plugins\$plugin\$plugin.csproj) -replace '%GUID%', "$pluginguid" | Set-Content $PSScriptRoot\Plugins\$plugin\$plugin.csproj
}

if (!(Test-Path $PSScriptRoot\Plugins\$plugin\Plugin)) {
    New-Item -ItemType Directory -Path $PSScriptRoot\Plugins\$plugin\Plugin
}
if (!(Test-Path $PSScriptRoot\Plugins\$plugin\Plugin\_name_.cs)) {
    Copy-Item -Path $PSScriptRoot\Templates\Plugin\_name_.cs -Destination $PSScriptRoot\Plugins\$plugin\Plugin\_name_.cs -force
    (Get-Content $PSScriptRoot\Plugins\$plugin\Plugin\_name_.cs) -replace '%ASSEMBLY%', "$plugin" | Set-Content $PSScriptRoot\Plugins\$plugin\Plugin\_name_.cs
}

if (!(Test-Path $PSScriptRoot\Plugins\$plugin\Properties)) {
    New-Item -ItemType Directory -Path $PSScriptRoot\Plugins\$plugin\Properties
}
if (!(Test-Path $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs)) {
    Copy-Item -Path $PSScriptRoot\Templates\Properties\AssemblyInfo.cs -Destination $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs -force
    (Get-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs) -replace '%ASSEMBLY%', "$plugin" | Set-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs
    (Get-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs) -replace '%COMPANY%', "D365.Community" | Set-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs
    (Get-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs) -replace '%YYYY%', "$yyyy" | Set-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs
    (Get-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs) -replace '%GUID%', "$pluginguid" | Set-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs
    (Get-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs) -replace '%YYMM%', "$yymm" | Set-Content $PSScriptRoot\Plugins\$plugin\Properties\AssemblyInfo.cs
}

if (!(Test-Path $PSScriptRoot\Webresources\Strings\$plugin.1031.resx)) {
    Copy-Item -Path $PSScriptRoot\Templates\Strings\Assembly.resx -Destination $PSScriptRoot\Webresources\Strings\$plugin.1031.resx -force
}
if (!(Test-Path $PSScriptRoot\Webresources\Strings\$plugin.1033.resx)) {
    Copy-Item -Path $PSScriptRoot\Templates\Strings\Assembly.resx -Destination $PSScriptRoot\Webresources\Strings\$plugin.1033.resx -force
}
