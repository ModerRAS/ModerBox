# PowerShell script to build the Native AOT version of ModerBox
# with a timeout.

$ErrorActionPreference = "Stop"

# Define the project path and output path
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $projectRoot "ModerBox"
$publishPath = Join-Path $projectRoot "publish\native"
$timeoutSeconds = 900 # 15 minutes

# Clean previous publish output
if (Test-Path $publishPath) {
    Write-Host "Removing existing publish directory..."
    Remove-Item -Path $publishPath -Recurse -Force
}

# Start the build job
Write-Host "Starting Native AOT build for ModerBox..."
$job = Start-Job -ScriptBlock {
    param($proj, $out)
    # This command will be executed in a background process
    dotnet publish $proj -r win-x64 -c Release -o $out
} -ArgumentList $projectPath, $publishPath

# Wait for the job to complete with a timeout
Write-Host "Waiting for build to complete (timeout: $($timeoutSeconds)s)..."
$job | Wait-Job -Timeout $timeoutSeconds

# Check the job state
if ($job.State -eq 'Running') {
    Write-Error "Build timed out after $timeoutSeconds seconds."
    Stop-Job $job
    Remove-Job $job
    exit 1
} 

# Get job results
$jobResult = Receive-Job $job

if ($job.State -ne 'Completed') {
    Write-Error "Build failed. See logs for details."
    Write-Host $jobResult
    Remove-Job $job
    exit 1
} else {
    Write-Host "Build completed successfully!"
    Write-Host $jobResult
    Remove-Job $job
}

Write-Host "Publish path: $publishPath"
Write-Host "Listing content of $projectRoot"
Get-ChildItem -Path $projectRoot

exit 0 