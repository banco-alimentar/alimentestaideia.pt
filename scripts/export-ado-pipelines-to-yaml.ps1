# Temporary exporter: analyse Azure DevOps classic build + release pipelines and
# generate starter YAML files for migration.
#
# Usage:
#   $env:AZDO_PAT = "<pat with Build (Read), Release (Read), Task Groups (Read), Agent Pools (Read)>"
#   .\scripts\export-ado-pipelines-to-yaml.ps1
#   .\scripts\export-ado-pipelines-to-yaml.ps1 -OutputDir .\azure-pipelines-export
#
# Output is a best-effort draft. Review secrets, service connections, and task
# mappings before committing YAML. Delete this script and export folder when done.

[CmdletBinding()]
param(
    [string]$Organization = "BancoAlimentar",
    [string]$Project = "Alimentestaideia.pt",
    [string]$OutputDir = "",
    [string]$Pat = $env:AZDO_PAT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Pat)) {
    $Pat = $env:AZURE_DEVOPS_EXT_PAT
}

if ([string]::IsNullOrWhiteSpace($Pat)) {
    throw "Set AZDO_PAT or AZURE_DEVOPS_EXT_PAT with Build (Read) and Release (Read) scopes."
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $PSScriptRoot "ado-yaml-export"
}

$apiVersion = "7.1"
$projectSegment = [uri]::EscapeDataString($Project)
$baseUrl = "https://dev.azure.com/$Organization"
$releaseBaseUrl = "https://vsrm.dev.azure.com/$Organization"
$authHeader = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat"))

function Invoke-AdoGet {
    param([Parameter(Mandatory = $true)][string]$Uri)
    return Invoke-RestMethod -Uri $Uri -Headers @{ Authorization = $authHeader } -Method Get
}

function Get-AdoProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Object) { return $null }
    if ($Object.PSObject.Properties.Match($Name).Count -eq 0) { return $null }
    return $Object.$Name
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function ConvertTo-SafeFileName {
    param([string]$Name)
    $safe = ($Name -replace '[^\w\-]+', '-').Trim('-').ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($safe)) { return "pipeline" }
    return $safe
}

function Escape-YamlScalar {
    param([string]$Value)

    if ($null -eq $Value) { return "''" }
    if ($Value -match '^[A-Za-z0-9_\-./]+$') { return $Value }
    $escaped = $Value -replace "'", "''"
    return "'$escaped'"
}

function Add-YamlLine {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Text,
        [int]$Indent = 0
    )

    $Lines.Add((' ' * $Indent) + $Text)
}

function Get-AdoCollection {
    param($Response)

    if ($null -eq $Response) { return @() }
    if ($Response -is [System.Array]) { return @($Response) }
    if ($Response.PSObject.Properties.Match("value").Count -gt 0) {
        return @($Response.value)
    }

    return @($Response)
}

function Get-TaskCatalog {
    Write-Host "Loading task catalog (for GUID -> task name mapping)..." -ForegroundColor DarkGray
    $catalog = @{}

    try {
        $uri = "$baseUrl/_apis/distributedtask/tasks?api-version=$apiVersion&`$top=5000"
        $response = Invoke-AdoGet -Uri $uri
        $tasks = Get-AdoCollection $response

        foreach ($task in $tasks) {
            $idProp = Get-AdoProperty $task "id"
            if ($null -eq $idProp) { continue }
            $id = $idProp.ToString().ToLowerInvariant()
            if (-not $catalog.ContainsKey($id)) {
                $catalog[$id] = $task
            }
        }
    }
    catch {
        Write-Warning "Could not load full task catalog: $($_.Exception.Message)"
    }

    if ($catalog.Count -eq 0) {
        Write-Warning "Task catalog is empty. Steps will use task names from definitions when available, otherwise UnknownTask-{guid}."
    }
    else {
        Write-Host "  Loaded $($catalog.Count) task definitions." -ForegroundColor DarkGray
    }

    return $catalog
}

function Get-TaskReference {
    param(
        $TaskObject,
        [hashtable]$TaskCatalog
    )

    if ($null -eq $TaskObject) { return "script@1" }

    $name = Get-AdoProperty $TaskObject "name"
    $version = Get-AdoProperty $TaskObject "version"
    if (-not [string]::IsNullOrWhiteSpace($name) -and $null -ne $version) {
        $major = Get-AdoProperty $version "major"
        $minor = Get-AdoProperty $version "minor"
        if ($null -ne $major -and $null -ne $minor) {
            return "$name@$major.$minor"
        }
        return $name
    }

    $idProp = Get-AdoProperty $TaskObject "id"
    if ($null -eq $idProp) { return "script@1" }
    $id = $idProp.ToString().ToLowerInvariant()
    if ($TaskCatalog.ContainsKey($id)) {
        $task = $TaskCatalog[$id]
        $major = Get-AdoProperty (Get-AdoProperty $task "version") "major"
        $minor = Get-AdoProperty (Get-AdoProperty $task "version") "minor"
        $taskName = Get-AdoProperty $task "name"
        if ($null -ne $major -and $null -ne $minor -and $taskName) {
            return "$taskName@$major.$minor"
        }
        if ($taskName) { return $taskName }
    }

    return "UnknownTask-$id"
}

function Get-PoolYaml {
    param($Queue, [array]$ProjectQueues)

    if ($null -eq $Queue) {
        Add-YamlLine $script:yamlLines "pool:"
        Add-YamlLine $script:yamlLines "  vmImage: 'windows-latest'" 2
        return
    }

    $poolId = $null
    if ($Queue.pool) { $poolId = $Queue.pool.id }
    $queueId = Get-AdoProperty $Queue "id"
    $resolved = $ProjectQueues | Where-Object { $_.id -eq $queueId } | Select-Object -First 1
    if ($resolved -and $resolved.pool) {
        $poolId = $resolved.pool.id
        $isHosted = $resolved.pool.isHosted
    }
    elseif ($Queue.pool -and $Queue.pool.PSObject.Properties.Match("isHosted").Count -gt 0) {
        $isHosted = $Queue.pool.isHosted
    }
    else {
        $isHosted = ($poolId -eq 9)
    }

    Add-YamlLine $script:yamlLines "pool:"
    if ($isHosted -or $poolId -eq 9) {
        Add-YamlLine $script:yamlLines "  vmImage: 'windows-latest'" 2
    }
    else {
        Add-YamlLine $script:yamlLines "  name: $(Escape-YamlScalar $Queue.name)" 2
        Add-YamlLine $script:yamlLines "  # queueId=$queueId poolId=$poolId (self-hosted - consider Azure Pipelines)" 2
    }
}

function Get-TriggerYaml {
    param($Triggers)

    $ci = @($Triggers | Where-Object { $_.triggerType -eq "continuousIntegration" })
    if ($ci.Count -eq 0) {
        Add-YamlLine $script:yamlLines "trigger: none"
        return
    }

    $branches = @()
    foreach ($trigger in $ci) {
        foreach ($filter in @($trigger.branchFilters)) {
            if ($filter -match '^\+refs/heads/(.+)$') {
                $branches += $Matches[1]
            }
        }
    }

    $branches = @($branches | Select-Object -Unique)
    if ($branches.Count -eq 0) {
        Add-YamlLine $script:yamlLines "trigger:"
        Add-YamlLine $script:yamlLines "  branches:" 2
        Add-YamlLine $script:yamlLines "    include:" 4
        Add-YamlLine $script:yamlLines "      - developer" 6
        Add-YamlLine $script:yamlLines "  # branch filters not parsed; verify in Azure DevOps" 4
        return
    }

    Add-YamlLine $script:yamlLines "trigger:"
    Add-YamlLine $script:yamlLines "  branches:" 2
    Add-YamlLine $script:yamlLines "    include:" 4
    foreach ($branch in $branches) {
        Add-YamlLine $script:yamlLines "      - $(Escape-YamlScalar $branch)" 6
    }
}

function Add-VariablesYaml {
    param($Variables)

    if ($null -eq $Variables) { return }

    $props = @($Variables.PSObject.Properties)
    if ($props.Count -eq 0) { return }

    Add-YamlLine $script:yamlLines ""
    Add-YamlLine $script:yamlLines "variables:"

    foreach ($prop in $props) {
        $var = $prop.Value
        $name = $prop.Name
        $isSecret = $false
        if ($var -is [PSCustomObject] -and $var.PSObject.Properties.Match("isSecret").Count -gt 0) {
            $isSecret = [bool]$var.isSecret
        }

        if ($isSecret) {
            Add-YamlLine $script:yamlLines "  $name`: # secret - map to variable group or Key Vault" 2
        }
        else {
            $value = if ($var -is [PSCustomObject]) { Get-AdoProperty $var "value" } else { [string]$var }
            Add-YamlLine $script:yamlLines "  $name`: $(Escape-YamlScalar $value)" 2
        }
    }
}

function Add-StepYaml {
    param(
        $Step,
        [hashtable]$TaskCatalog,
        [int]$BaseIndent = 0
    )

    $displayName = Get-AdoProperty $Step "displayName"
    $enabled = Get-AdoProperty $Step "enabled"
    if ($null -eq $enabled) { $enabled = $true }

    if (-not $enabled) {
        Add-YamlLine $script:yamlLines "- task: $(Get-TaskReference (Get-AdoProperty $Step 'task') $TaskCatalog)" $BaseIndent
        Add-YamlLine $script:yamlLines "enabled: false" ($BaseIndent + 2)
        if ($displayName) {
            Add-YamlLine $script:yamlLines "displayName: $(Escape-YamlScalar $displayName)" ($BaseIndent + 2)
        }
        return
    }

    $taskRef = Get-TaskReference (Get-AdoProperty $Step "task") $TaskCatalog
    Add-YamlLine $script:yamlLines "- task: $taskRef" $BaseIndent
    if ($displayName) {
        Add-YamlLine $script:yamlLines "displayName: $(Escape-YamlScalar $displayName)" ($BaseIndent + 2)
    }

    $inputs = Get-AdoProperty $Step "inputs"
    if ($null -eq $inputs) { return }

    Add-YamlLine $script:yamlLines "inputs:" ($BaseIndent + 2)
    foreach ($inputProp in @($inputs.PSObject.Properties)) {
        $inputName = $inputProp.Name
        $inputValue = [string]$inputProp.Value
        if ($inputValue -match '(?i)(password|secret|token|key|connectionstring)') {
            Add-YamlLine $script:yamlLines "${inputName}: # redacted - set via variable group" ($BaseIndent + 4)
        }
        else {
            Add-YamlLine $script:yamlLines "${inputName}: $(Escape-YamlScalar $inputValue)" ($BaseIndent + 4)
        }
    }
}

function Convert-BuildDefinitionToYaml {
    param(
        $Definition,
        [hashtable]$TaskCatalog,
        [array]$ProjectQueues
    )

    $script:yamlLines = [System.Collections.Generic.List[string]]::new()

    Add-YamlLine $script:yamlLines "# Generated draft from Azure DevOps build definition '$($Definition.name)' (id=$($Definition.id))"
    Add-YamlLine $script:yamlLines "# Source: $baseUrl/$projectSegment/_build?definitionId=$($Definition.id)"
    Add-YamlLine $script:yamlLines "# Review and test before replacing the classic pipeline."
    Add-YamlLine $script:yamlLines ""

    Get-TriggerYaml $Definition.triggers
    Add-YamlLine $script:yamlLines ""
    Get-PoolYaml $Definition.queue $ProjectQueues
    Add-VariablesYaml $Definition.variables

    Add-YamlLine $script:yamlLines ""
    Add-YamlLine $script:yamlLines "steps:"

    $process = Get-AdoProperty $Definition "process"
    $processType = Get-AdoProperty $process "type"

    if ($processType -eq 2) {
        $yamlFile = Get-AdoProperty $process "yamlFilename"
        Add-YamlLine $script:yamlLines "  # Already YAML-based; file in repo: $(Escape-YamlScalar $yamlFile)" 2
        return ($script:yamlLines -join [Environment]::NewLine)
    }

    $phases = @()
    if ($process.phases) { $phases = @($process.phases) }
    elseif ($process.steps) {
        $phases = @([PSCustomObject]@{ steps = $process.steps })
    }

    if ($phases.Count -eq 0) {
        Add-YamlLine $script:yamlLines "  - script: echo 'No steps found in definition export'" 2
        return ($script:yamlLines -join [Environment]::NewLine)
    }

    foreach ($phase in $phases) {
        $phaseName = Get-AdoProperty $phase "name"
        if ($phaseName) {
            Add-YamlLine $script:yamlLines "  # Phase: $phaseName" 2
        }

        foreach ($step in @($phase.steps)) {
            Add-StepYaml -Step $step -TaskCatalog $TaskCatalog -BaseIndent 2
        }
    }

    return ($script:yamlLines -join [Environment]::NewLine)
}

function Convert-ReleaseDefinitionToYaml {
    param(
        $Release,
        [hashtable]$TaskCatalog,
        [array]$ProjectQueues,
        [hashtable]$QueueById
    )

    $script:yamlLines = [System.Collections.Generic.List[string]]::new()

    Add-YamlLine $script:yamlLines "# Generated draft from Azure DevOps release definition '$($Release.name)' (id=$($Release.id))"
    Add-YamlLine $script:yamlLines "# Classic releases map to multi-stage YAML. Merge with your build pipeline or use as reference."
    Add-YamlLine $script:yamlLines "# Artifacts: $(@($Release.artifacts | ForEach-Object { $_.alias }) -join ', ')"
    Add-YamlLine $script:yamlLines ""
    Add-YamlLine $script:yamlLines "trigger: none  # typically triggered by build pipeline completion"
    Add-YamlLine $script:yamlLines ""
    Add-YamlLine $script:yamlLines "stages:"

    foreach ($environment in @($Release.environments)) {
        $envName = Get-AdoProperty $environment "name"
        $safeEnv = ($envName -replace '[^\w\- ]', '') -replace '\s+', '_'
        Add-YamlLine $script:yamlLines "- stage: $(Escape-YamlScalar $safeEnv)"
        Add-YamlLine $script:yamlLines "  displayName: $(Escape-YamlScalar $envName)" 2
        Add-YamlLine $script:yamlLines "  jobs:" 2
        Add-YamlLine $script:yamlLines "  - deployment: $(Escape-YamlScalar ($safeEnv + '_Deploy'))" 4
        Add-YamlLine $script:yamlLines "    displayName: $(Escape-YamlScalar $envName)" 6
        Add-YamlLine $script:yamlLines "    environment: $(Escape-YamlScalar $envName)" 6

        $queueId = $null
        $phase = @($environment.deployPhases | Select-Object -First 1)
        if ($phase.Count -gt 0 -and $phase[0].deploymentInput) {
            $queueId = Get-AdoProperty $phase[0].deploymentInput "queueId"
        }

        Add-YamlLine $script:yamlLines "    pool:" 6
        if ($queueId -and $QueueById.ContainsKey($queueId)) {
            $q = $QueueById[$queueId]
            if ($q.pool.isHosted -or $q.pool.id -eq 9) {
                Add-YamlLine $script:yamlLines "      vmImage: 'windows-latest'" 8
            }
            else {
                Add-YamlLine $script:yamlLines "      name: $(Escape-YamlScalar $q.name)" 8
            }
        }
        else {
            Add-YamlLine $script:yamlLines "      vmImage: 'windows-latest'" 8
        }

        Add-YamlLine $script:yamlLines "    strategy:" 6
        Add-YamlLine $script:yamlLines "      runOnce:" 8
        Add-YamlLine $script:yamlLines "        deploy:" 10
        Add-YamlLine $script:yamlLines "          steps:" 12

        $tasks = @()
        foreach ($deployPhase in @($environment.deployPhases)) {
            if ($deployPhase.workflowTasks) {
                $tasks += @($deployPhase.workflowTasks)
            }
        }

        if ($tasks.Count -eq 0) {
            Add-YamlLine $script:yamlLines "          - script: echo 'No workflow tasks exported for this environment'" 12
        }
        else {
            foreach ($task in $tasks) {
                $step = [PSCustomObject]@{
                    displayName = Get-AdoProperty $task "name"
                    enabled     = Get-AdoProperty $task "enabled"
                    task        = Get-AdoProperty $task "task"
                    inputs      = Get-AdoProperty $task "inputs"
                }
                if (-not $step.task -and $task.taskId) {
                    $step.task = [PSCustomObject]@{ id = $task.taskId }
                }
                Add-StepYaml -Step $step -TaskCatalog $TaskCatalog -BaseIndent 12
            }
        }

        Add-YamlLine $script:yamlLines ""
    }

    return ($script:yamlLines -join [Environment]::NewLine)
}

function Write-TextFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $dir = Split-Path $Path -Parent
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

# --- Main ---

Write-Section "Export Azure DevOps pipelines to YAML"
Write-Host "Organization : $Organization"
Write-Host "Project      : $Project"
Write-Host "Output dir   : $OutputDir"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$taskCatalog = Get-TaskCatalog
$projectQueues = Get-AdoCollection (Invoke-AdoGet -Uri "$baseUrl/$projectSegment/_apis/distributedtask/queues?api-version=$apiVersion")
$queueById = @{}
foreach ($q in $projectQueues) {
    $queueById[$q.id] = $q
}

$analysis = [ordered]@{
    organization = $Organization
    project      = $Project
    exportedAt   = (Get-Date).ToUniversalTime().ToString("o")
    builds       = @()
    releases     = @()
}

Write-Section "Build definitions"
$buildSummaries = Get-AdoCollection (Invoke-AdoGet -Uri "$baseUrl/$projectSegment/_apis/build/definitions?api-version=$apiVersion&`$top=100") | Sort-Object name

foreach ($summary in $buildSummaries) {
    $definition = Invoke-AdoGet -Uri "$baseUrl/$projectSegment/_apis/build/definitions/$($summary.id)?api-version=$apiVersion"
    $processType = Get-AdoProperty (Get-AdoProperty $definition "process") "type"
    $typeLabel = if ($processType -eq 2) { "yaml" } else { "classic" }

    $poolName = if ($definition.queue -and $definition.queue.pool) { $definition.queue.pool.name } else { "?" }
    $poolId = if ($definition.queue -and $definition.queue.pool) { $definition.queue.pool.id } else { $null }

    Write-Host "  [$($definition.name)] id=$($definition.id) type=$typeLabel pool=$poolName (id=$poolId)"

    $fileName = "build-$(ConvertTo-SafeFileName $definition.name).yml"
    $filePath = Join-Path $OutputDir $fileName

    if ($processType -eq 2) {
        $yamlFile = Get-AdoProperty $definition.process "yamlFilename"
        $content = @"
# Build definition '$($definition.name)' is already YAML-based.
# Pipeline file in repository: $yamlFile
# Definition id: $($definition.id)
# URL: $baseUrl/$projectSegment/_build?definitionId=$($definition.id)

# No generated steps; edit the existing YAML file in your repo.
"@
    }
    else {
        $content = Convert-BuildDefinitionToYaml -Definition $definition -TaskCatalog $taskCatalog -ProjectQueues $projectQueues
    }

    Write-TextFile -Path $filePath -Content $content
    Write-Host "    -> $fileName" -ForegroundColor Green

    $analysis.builds += [ordered]@{
        id       = $definition.id
        name     = $definition.name
        type     = $typeLabel
        poolId   = $poolId
        poolName = $poolName
        file     = $fileName
        url      = "$baseUrl/$projectSegment/_build?definitionId=$($definition.id)"
    }
}

Write-Section "Release definitions"
$releaseSummaries = Get-AdoCollection (Invoke-AdoGet -Uri "$releaseBaseUrl/$projectSegment/_apis/release/definitions?api-version=$apiVersion&`$top=100") | Sort-Object name

foreach ($summary in $releaseSummaries) {
    $release = Invoke-AdoGet -Uri "$releaseBaseUrl/$projectSegment/_apis/release/definitions/$($summary.id)?api-version=$apiVersion"
    $envNames = @($release.environments | ForEach-Object { $_.name })

    Write-Host "  [$($release.name)] id=$($release.id) environments=$($envNames -join ', ')"

    $fileName = "release-$(ConvertTo-SafeFileName $release.name).yml"
    $filePath = Join-Path $OutputDir $fileName
    $content = Convert-ReleaseDefinitionToYaml -Release $release -TaskCatalog $taskCatalog -ProjectQueues $projectQueues -QueueById $queueById

    Write-TextFile -Path $filePath -Content $content
    Write-Host "    -> $fileName" -ForegroundColor Green

    $analysis.releases += [ordered]@{
        id           = $release.id
        name         = $release.name
        environments = $envNames
        file         = $fileName
        url          = "$baseUrl/$projectSegment/_release?definitionId=$($release.id)"
    }
}

$analysisPath = Join-Path $OutputDir "_analysis.json"
Write-TextFile -Path $analysisPath -Content ($analysis | ConvertTo-Json -Depth 6)

$md = @(
    "# Azure DevOps pipeline export",
    "",
    "Organization: **$Organization**",
    "Project: **$Project**",
    "Exported (UTC): $($analysis.exportedAt)",
    "",
    "## Build pipelines",
    ""
)

foreach ($b in $analysis.builds) {
    $md += "- **$($b.name)** (id=$($b.id), $($b.type)) → ``$($b.file)`` — [open]($($b.url))"
}

$md += ""
$md += "## Release pipelines"
$md += ""

foreach ($r in $analysis.releases) {
    $md += "- **$($r.name)** (id=$($r.id)) → ``$($r.file)`` — [open]($($r.url))"
    $md += "  - Environments: $($r.environments -join ', ')"
}

$md += ""
$md += "## Next steps"
$md += ""
$md += "1. Review generated YAML under ``$OutputDir``."
$md += "2. Merge build + release into one multi-stage pipeline where appropriate."
$md += "3. Replace secret placeholders with variable groups / Key Vault."
$md += "4. Create new YAML pipeline in Azure DevOps pointing at committed files."
$md += "5. Disable classic definitions after validation."
$md += ""
$md += "Delete ``scripts/export-ado-pipelines-to-yaml.ps1`` and this export folder when migration is complete."

Write-TextFile -Path (Join-Path $OutputDir "_analysis.md") -Content ($md -join [Environment]::NewLine)

Write-Section "Done"
Write-Host "Wrote $($analysis.builds.Count) build + $($analysis.releases.Count) release YAML drafts to:"
Write-Host "  $OutputDir"
Write-Host ""
Write-Host "Summary: $(Join-Path $OutputDir '_analysis.md')" -ForegroundColor Yellow
Write-Host "Review drafts before commit. Secret-like inputs are redacted in YAML output." -ForegroundColor Yellow
