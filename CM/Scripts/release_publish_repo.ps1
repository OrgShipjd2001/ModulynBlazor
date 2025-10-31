# Requires: PowerShell (pwsh or powershell.exe)

# --- 1. Variable Assignment and Validation ---

# Define required environment variables
$RepoUrl = $env:PublishRepo
$tag = $env:CI_COMMIT_TAG
$GithubToken = $env:PublishRepoToken # Read the GitHub access token

Write-Host "--- Git Tag Push Script ---"

# Check if the target repository URL variable is set
if ([string]::IsNullOrEmpty($RepoUrl)) {
    Write-Error "ERROR: Environment variable 'PublishRepo' is missing or empty."
    Write-Error "Cannot determine the target repository (GitHub) URL."
    exit 1
}

# Check if the CI commit tag variable is set
if ([string]::IsNullOrEmpty($tag)) {
    Write-Error "ERROR: Environment variable 'CI_COMMIT_TAG' is missing or empty."
    Write-Error "Cannot determine the tag name to push."
    exit 1
}

# Check if the GitHub token variable is set
if ([string]::IsNullOrEmpty($GithubToken)) {
    Write-Error "ERROR: Environment variable 'PublishRepoToken' (GitHub Token) is missing or empty."
    Write-Error "Cannot authenticate to GitHub."
    exit 1
}

# Construct the authenticated URL (Token injection)
# This format is required to pass the credentials non-interactively:
# https://<token>@github.com/user/repo.git
$AuthenticatedRepoUrl = $RepoUrl -replace 'https://', "https://$GithubToken@"

Write-Host "Target Repository (GitHub): $($RepoUrl)"
Write-Host "Tag to Push: $($tag)"

# --- 2. Execution with Error Handling ---

# Note: The $RepoUrl may not be defined as a remote in the local repository.
# The command below implicitly adds it as a temporary remote for the push operation.

Write-Host "Attempting to push tag '$tag' to GitHub..."

# Use a try/catch block for robust error handling.
try {
    # Execute the git push command using the authenticated URL.
    # The token is injected into the URL for non-interactive authentication.
    
    git push $AuthenticatedRepoUrl $tag 2>&1 | Write-Host

    # Check the exit code of the last external command (git).
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SUCCESS: Tag '$tag' successfully pushed to GitHub ($RepoUrl)."
    }
    else {
        # This branch catches external command failures not thrown as PowerShell exceptions.
        Write-Error "FAILURE: Git push failed with exit code $LASTEXITCODE. See output above for details."
        exit 1
    }

}
catch {
    # This catches PowerShell-specific errors (e.g., if git command is not found).
    Write-Error "CRITICAL ERROR during git push operation:"
    Write-Error $_
    exit 1
}

# --- 3. Optional: Verify Tag Exists on Remote ---
# Note: Git does not provide a direct success message beyond the exit code,
# but we can optionally check the remote tag list.

Write-Host "Verifying tag existence on remote..."

# Use git ls-remote to check if the tag is listed on the remote, using the authenticated URL
$tagCheck = git ls-remote $AuthenticatedRepoUrl "refs/tags/$tag" 2>$null

if ([string]::IsNullOrEmpty($tagCheck)) {
    Write-Error "VERIFICATION FAILED: Tag '$tag' was not found on the remote repository $RepoUrl after push."
    exit 1
} else {
    Write-Host "VERIFICATION SUCCESS: Tag '$tag' confirmed on remote."
}

Write-Host "--- Script finished successfully ---"
exit 0
