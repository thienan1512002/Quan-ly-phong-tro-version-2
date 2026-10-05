param(
    [Parameter(Mandatory=$true)]
    [string]$Command
)

$blocked = @(
    'git\s+push\s+.*--force',
    'git\s+push\s+.*\s(main|master|develop)(\s|$)',
    'DROP\s+DATABASE',
    'DROP\s+TABLE',
    'TRUNCATE\s+TABLE',
    'kubectl\s+delete\s+namespace'
)

foreach ($pattern in $blocked) {
    if ($Command -match $pattern) {
        Write-Error "Blocked by NMV agent safety policy: $pattern"
        exit 10
    }
}

exit 0

