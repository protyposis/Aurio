# -Raw: read as single string instead of line array
$content = Get-Content -Path ./README.md -Raw

# (?ms) modifiers: multiline, match whitespace (line breaks)
# .*?: ? = non-greedy
$filteredContent = $content -replace "(?ms)^<!-- nuget-exclude:start -->(.*?)<!-- nuget-exclude:end -->$", ""

$filteredContent | Out-File -Encoding utf8 -FilePath ./src/Aurio/README.nuget.md
