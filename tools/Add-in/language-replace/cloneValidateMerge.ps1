param (
    [string]$SourceRepoUrl = "https://github.com/swatimahajan2790/sourcerepo.git",
    [string]$MainRepoUrl  = "https://github.com/swatimahajan2790/mainrepo.git",
    [string]$WorkDir = "C:\temp\mdp-onboard",
    [string]$BranchPrefix = "feature/add-tool"
)

Write-Host "===== MDP Tool Onboarding Started ====="

# Cleanup
if (Test-Path $WorkDir) {
    Remove-Item -Recurse -Force $WorkDir
}
New-Item -ItemType Directory -Path $WorkDir | Out-Null

Set-Location $WorkDir

# ----------------------------
# STEP 1: Clone Repositories
# ----------------------------
Write-Host "Cloning repositories..."

git clone $MainRepoUrl main-repo
git clone $SourceRepoUrl source-repo

if (!(Test-Path "main-repo") -or !(Test-Path "source-repo")) {
    Write-Error "Repo clone failed"
    exit 1
}

# ----------------------------
# STEP 2: Read tool.json
# ----------------------------
$toolJsonPath = "source-repo/tool.json"

if (!(Test-Path $toolJsonPath)) {
    Write-Error "tool.json not found in source repo"
    exit 1
}

$json = Get-Content $toolJsonPath | ConvertFrom-Json

$toolName = $json.Name
$toolType = $json.Type

if ([string]::IsNullOrEmpty($toolName) -or [string]::IsNullOrEmpty($toolType)) {
    Write-Error "Invalid tool.json: toolName/toolType missing"
    exit 1
}

Write-Host "Tool Name: $Name"
Write-Host "Tool Type: $toolType"

# ----------------------------
# STEP 3: Prepare Target Path
# ----------------------------
$targetPath = "main-repo/tools/$toolType/$toolName"

Write-Host "Creating target path: $targetPath"

New-Item -ItemType Directory -Force -Path $targetPath | Out-Null

# ----------------------------
# STEP 4: Copy Files
# ----------------------------
Write-Host "Copying source to target..."

Copy-Item "source-repo/*" -Destination $targetPath -Recurse -Force

# ----------------------------
# STEP 5: VALIDATION
# ----------------------------
Write-Host "Validating structure..."

$validationFailed = $false

# Core validations
if (!(Test-Path "$targetPath/tool.json")) {
    Write-Error "tool.json missing"
    $validationFailed = $true
}

#if (!(Test-Path "$targetPath/src")) {
  #  Write-Error "src folder missing"
  #  $validationFailed = $true
#}

# Tool-type specific validations
switch ($toolType.ToLower()) {

    "Add-in" {
        if (!(Get-ChildItem $targetPath -Recurse -Filter *.csproj)) {
            Write-Error "C# tool must contain .csproj file"
            $validationFailed = $true
        }
    }

    "pyrevit" {
        if (!(Get-ChildItem $targetPath -Recurse -Filter *.py)) {
            Write-Error "pyRevit tool must contain .py file"
            $validationFailed = $true
        }
    }

    "dynamo" {
        if (!(Get-ChildItem $targetPath -Recurse -Filter *.dyn)) {
            Write-Error "Dynamo tool must contain .dyn file"
            $validationFailed = $true
        }
    }

    "lisp" {
        if (!(Get-ChildItem $targetPath -Recurse -Filter *.lsp)) {
            Write-Error "LISP tool must contain .lsp file"
            $validationFailed = $true
        }
    }

    "grasshopper" {
        if (!(Get-ChildItem $targetPath -Recurse -Filter *.gh)) {
            Write-Error "Grasshopper tool must contain .gh file"
            $validationFailed = $true
        }
    }

    default {
        Write-Warning "Unknown tool type: $toolType"
    }
}

if ($validationFailed) {
    Write-Error "Validation failed. Aborting process."
    exit 1
}

Write-Host "Validation successful"

# ----------------------------
# STEP 6: Git Branch + Commit
# ----------------------------
Set-Location "main-repo"

$branchName = "$BranchPrefix-$toolName"

git checkout -b $branchName

git add .

git commit -m "Added $toolName under $toolType via automated onboarding"

git push origin $branchName

Write-Host "Code pushed to branch: $branchName"

# ----------------------------
# STEP 7: AUTO-MERGE (Git Only Strategy)
# ----------------------------
# NOTE:
# With Git-only access (no REST API), true PR automation isn't possible.
# Instead, we directly merge into main after validation.

git checkout main

git pull origin main

git merge $branchName --no-ff -m "Merging $toolName after validation"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Merge conflict occurred!"
    exit 1
}

git push origin main

Write-Host "Successfully merged into main"

# ----------------------------
# CLEANUP
# ----------------------------
Write-Host "Cleaning up..."
Set-Location ..

Remove-Item -Recurse -Force $WorkDir

Write-Host "===== MDP Tool Onboarding Completed ====="