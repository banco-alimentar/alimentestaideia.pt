# Stamps Version, VersionPrefix, and AssemblyVersion on SDK-style .csproj files.
# Replaces the Manifest Versioning Build Tasks extension (VersionDotNetCoreAssemblies) in Azure Pipelines.
param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$VersionNumber
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Set-OrAddProperty {
    param(
        [System.Xml.XmlElement]$PropertyGroup,
        [string]$Name,
        [string]$Value
    )

    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($PropertyGroup.OwnerDocument.NameTable)
    $existing = $PropertyGroup.SelectSingleNode($Name)
    if ($null -eq $existing) {
        $element = $PropertyGroup.OwnerDocument.CreateElement($Name)
        $element.InnerText = $Value
        [void]$PropertyGroup.AppendChild($element)
        return
    }

    $existing.InnerText = $Value
}

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
    Set-OrAddProperty -PropertyGroup $propertyGroup -Name 'AssemblyVersion' -Value $VersionNumber

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

    Write-Host "Versioned $($projectFile.FullName) -> $VersionNumber"
}

Write-Host "##vso[task.setvariable variable=OutputedVersion]$VersionNumber"
