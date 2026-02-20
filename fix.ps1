$content = Get-Content AGENTS.md -Raw
$content = $content.Replace('"', """)
Set-Content AGENTS.md -Value $content
