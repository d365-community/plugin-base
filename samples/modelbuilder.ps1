Clear-Host

Set-Location -Path $PSScriptRoot

# latest recent pac version
$pacVersion = "2.12.2"
Write-Host "Use Pac Version $pacVersion"
pac install $pacVersion
pac use $pacVersion

Write-Host 'List of pac authentications'

pac auth list

$idx = Read-Host -Prompt 'Select an index or enter "c" to create a new one or enter "s" to skip pac auth'

# Work around a PAC modelbuilder bug in recent Microsoft builds:
# OrganizationResponse property setters are generated with this.Parameters[...] even
# though the response payload is exposed through Results. Rewrite only setter
# assignments inside ...Response classes so request classes and response getters stay untouched.
function FixResponseResultSetters {
    param(
        [string[]]$Lines
    )

    $inResponseClass = $false
    $classBraceDepth = 0
    $inSetter = $false
    $setterBraceDepth = 0

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]

        if (-not $inResponseClass -and $line -match 'public partial class .+Response : Microsoft\.Xrm\.Sdk\.OrganizationResponse') {
            $inResponseClass = $true
            $classBraceDepth = 0
            $inSetter = $false
            $setterBraceDepth = 0
        }

        if ($inResponseClass -and -not $inSetter -and $line -match '^\s*set\s*$') {
            $inSetter = $true
            $setterBraceDepth = 0
        }

        if ($inResponseClass -and $inSetter -and $line -match 'this\.Parameters\["') {
            $Lines[$i] = $line -replace 'this\.Parameters\[', 'this.Results['
            $line = $Lines[$i]
        }

        if ($inResponseClass) {
            $classBraceDepth += ([regex]::Matches($line, '\{')).Count
            $classBraceDepth -= ([regex]::Matches($line, '\}')).Count

            if ($inSetter) {
                $setterBraceDepth += ([regex]::Matches($line, '\{')).Count
                $setterBraceDepth -= ([regex]::Matches($line, '\}')).Count

                if ($setterBraceDepth -le 0 -and $line -match '^\s*\}$') {
                    $inSetter = $false
                }
            }

            if ($classBraceDepth -le 0 -and $line -match '^\s*\}$') {
                $inResponseClass = $false
            }
        }
    }

    return $Lines
}

function PacAuthCreate {
    $ini = Get-Content $env:HOMEDRIVE$env:HOMEPATH\pacauth.ini -ErrorAction Stop | ConvertFrom-StringData -ErrorAction Stop
    if (("$($ini.Tenant)" -ne "") -and ("$($ini.Certificate)" -ne "") -and (Test-Path $ini.Certificate)) {
        $secureString = Read-Host -Prompt '--certificatePassword' -AsSecureString
        $password = [System.Net.NetworkCredential]::new("", $secureString).Password
        pac auth create --name "$($ini.AuthName)" --environment "$($ini.Environment)" --applicationId "$($ini.ApplicationId)" --certificatePassword "$password" --certificateDiskPath "$($ini.Certificate)" --tenant "$($ini.Tenant)"
        if ($output -Match "Failed to connect to Dataverse" ) {
            $output
            pac auth delete --name "$($ini.AuthName)"
            PacAuthCreate
        }
    }
    elseif (("$($ini.Tenant)" -ne "") -and ("$($ini.Certificate)" -eq "")) {
        $secureString = Read-Host -Prompt '--clientSecret' -AsSecureString
        $secret = [System.Net.NetworkCredential]::new("", $secureString).Password
        pac auth create --name "$($ini.AuthName)" --environment "$($ini.Environment)" --applicationId "$($ini.ApplicationId)" --clientSecret "$secret" --tenant "$($ini.Tenant)"
        if ($output -Match "Failed to connect to Dataverse" ) {
            $output
            pac auth delete --name "$($ini.AuthName)"
            PacAuthCreate
        }
    }
    else {
        pac auth create --name "$($ini.AuthName)" --environment "$($ini.Environment)" --deviceCode
        if ($output -Match "Failed to connect to Dataverse" ) {
            $output
            pac auth delete --name "$($ini.AuthName)"
            PacAuthCreate
        }
    }    
}

if ($idx -eq "c") {
    PacAuthCreate
}
elseif ($idx -eq "s") {
    #skip
}
else {
    pac auth select --index $idx
    $output = $(pac env who 2>&1) 
    if ($output -Match "Failed to connect to Dataverse") {
        $output
        pac auth delete --index $idx
        PacAuthCreate
    }
}

$mode = Read-Host -Prompt 'Single (s) or full (f) generation?'

if ($mode -eq "f") {
    #required option sets are generated on demand
    #Write-Host "Generate Model for of GlobalOptionSets"
    #pac modelbuilder build --outdirectory $PSScriptRoot\Shared --generateGlobalOptionSets $true --entitynamesfilter "none" --messagenamesfilter "none" --settingsTemplateFile $PSScriptRoot\modelbuilder.json
    $parameters = Get-Content -Raw $PSScriptRoot\Shared\Entity.Model.json | ConvertFrom-Json
    foreach ($entity in $parameters.entityNamesFilter) {
        Write-Host "Generate Model for Entity $entity"
        pac modelbuilder build --outdirectory $PSScriptRoot\Shared --entitynamesfilter $entity --messagenamesfilter "none" --settingsTemplateFile $PSScriptRoot\modelbuilder.json
    }

    $assemblies = Get-ChildItem -Path $PSScriptRoot\Plugins, $PSScriptRoot\Bundles -Attributes Directory -Depth 0

    foreach ($assembly in $assemblies) {
        $name = $assembly.BaseName
        if (Test-Path $PSScriptRoot\Shared\$name.Model.json) {
            $parameters = Get-Content -Raw $PSScriptRoot\Shared\$name.Model.json | ConvertFrom-Json
            foreach ($message in $parameters.messageNamesFilter) {
                Write-Host "Generate Model for Message $message"
                pac modelbuilder build --outdirectory $PSScriptRoot\Shared --entitynamesfilter "none" --generateSdkMessages $true --messagenamesfilter $message --settingsTemplateFile $PSScriptRoot\modelbuilder.json
            }
        }
    }
}
elseif ($mode -eq "s") {
    $type = Read-Host -Prompt 'Entity (e) or Message (m)?'
    if ($type -eq "e") {
        $entity = Read-Host -Prompt 'Entity'
        pac modelbuilder build --outdirectory $PSScriptRoot\Shared --entitynamesfilter $entity --messagenamesfilter "none" --settingsTemplateFile $PSScriptRoot\modelbuilder.json
    }
    elseif ($type -eq "m") {
        $message = Read-Host -Prompt 'Message'
        pac modelbuilder build --outdirectory $PSScriptRoot\Shared --entitynamesfilter "none" --generateSdkMessages $true --messagenamesfilter $message --settingsTemplateFile $PSScriptRoot\modelbuilder.json
    }
}

$csharps = Get-ChildItem -Path $PSScriptRoot\Shared -Include *.cs -Depth 1
foreach ($csharp in $csharps) {
    try {
        [System.IO.File]::OpenRead($csharp.FullName).Close()
        $csharp.FullName
        $content = Get-Content $csharp.FullName | Select-String -pattern 'Runtime Version' -notmatch
        $content = FixResponseResultSetters -Lines $content
        $content | Set-Content "$($csharp.FullName).tmp"
        Move-item -Path "$($csharp.FullName).tmp" -destination $csharp.FullName -Force
    }
    catch {
        if (Test-Path "$($csharp.FullName).tmp") {
            try {
                Remove-Item -Path "$($csharp.FullName).tmp" -Force
            }
            catch {
                #ignore
            }
        }
    }
}
