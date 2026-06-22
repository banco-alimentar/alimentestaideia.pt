# Stamps Version, VersionPrefix, AssemblyVersion, and FileVersion on SDK-style .csproj files.
# Replaces the Manifest Versioning Build Tasks extension (VersionDotNetCoreAssemblies) in Azure Pipelines.
param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$VersionNumber,

    [int]$BuildId = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-AssemblyVersionPart {
    param([int]$Value)

    return $Value -ge 0 -and $Value -le 65535
}

function Get-AssemblyVersionFromBuildNumber {
    param(
        [string]$BuildNumber,
        [int]$BuildId
    )

    # Azure DevOps default build number: yyyyMMdd.revision (e.g. 20260622.5)
    if ($BuildNumber -match '^(\d{4})(\d{2})(\d{2})\.(\d+)$') {
        $parts = @(
            [int]$Matches[1],
            [int]$Matches[2],
            [int]$Matches[3],
            [int]$Matches[4]
        )

        if ($parts | ForEach-Object { Test-AssemblyVersionPart $_ } | Where-Object { -not $_ }) {
            throw "Build number '$BuildNumber' cannot be mapped to a valid assembly version."
        }

        return ($parts -join '.')
    }

    # Already a four-part assembly version
    if ($BuildNumber -match '^(\d+)\.(\d+)\.(\d+)\.(\d+)$') {
        $parts = @(
            [int]$Matches[1],
            [int]$Matches[2],
            [int]$Matches[3],
            [int]$Matches[4]
        )

        if ($parts | ForEach-Object { Test-AssemblyVersionPart $_ } | Where-Object { -not $_ }) {
            throw "Version '$BuildNumber' contains a component greater than 65535."
        }

        return ($parts -join '.')
    }

    if ($BuildId -gt 0) {
        $revision = $BuildId % 65536
        $build = [math]::Floor($BuildId / 65536)
        return "10.0.$build.$revision"
    }

    throw "Cannot derive assembly version from build number '$BuildNumber'. Pass -BuildId or use yyyyMMdd.revision format."
}

function Set-OrAddProperty {
    param(
        [System.Xml.XmlElement]$PropertyGroup,
        [string]$Name,
        [string]$Value
    )

    $existing = $PropertyGroup.SelectSingleNode($Name)
    if ($null -eq $existing) {
        $element = $PropertyGroup.OwnerDocument.CreateElement($Name)
        $element.InnerText = $Value
        [void]$PropertyGroup.AppendChild($element)
        return
    }

    $existing.InnerText = $Value
}

$assemblyVersion = Get-AssemblyVersionFromBuildNumber -BuildNumber $VersionNumber -BuildId $BuildId
Write-Host "Package/informational version: $VersionNumber"
Write-Host "Assembly/file version: $assemblyVersion"

$projectFiles = Get-ChildItem -Path $Path -Filter '*.csproj' -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/]bin[\\/]|[\\/]obj[\\/]' }

foreach ($projectFile in $projectFiles) {
    [xml]$project = Get-Content -LiteralPath $projectFile.FullName -Encoding UTF8
    $sdk = $project.Project.Sdk
    if ([string]::IsNullOrWhiteSpace($sdk) -or $sdk -notmatch 'Microsoft\.NET\.Sdk') {
        continue
    }

    $propertyGroup = $project.Project.PropertyGroup | Select-Object -First 1
    if ($null -eq $propertyGroup) {
        Write-Warning "Skipping $($projectFile.FullName): no PropertyGroup found."
        continue
    }

    Set-OrAddProperty -PropertyGroup $propertyGroup -Name 'Version' -Value $VersionNumber
    Set-OrAddProperty -PropertyGroup $propertyGroup -Name 'VersionPrefix' -Value $VersionNumber
    Set-OrAddProperty -PropertyGroup $propertyGroup -Name 'AssemblyVersion' -Value $assemblyVersion
    Set-OrAddProperty -PropertyGroup $propertyGroup -Name 'FileVersion' -Value $assemblyVersion

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)

    $writer = [System.Xml.XmlWriter]::Create($projectFile.FullName, $settings)
    try {
        $project.Save($writer)
    }
    finally {
        $writer.Close()
    }

    Write-Host "Versioned $($projectFile.FullName) -> $VersionNumber (assembly $assemblyVersion)"
}

Write-Host "##vso[task.setvariable variable=OutputedVersion]$VersionNumber"
