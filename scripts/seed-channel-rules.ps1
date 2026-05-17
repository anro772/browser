$ErrorActionPreference = 'Stop'

function PostJson($path, $body) {
  Invoke-RestMethod -Method Post -Uri "http://localhost:5000$path" `
    -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 8)
}

$existing = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/channels"
$wanted = @('acme-internal','dev-team','journalism-pack')
$targets = $existing.channels | Where-Object { $wanted -contains $_.name }
"Found channels: $($targets.Count)"

$cr1 = '[{"type":"block","match":{"urlPattern":"*facebook.com/plugins*"}}]'
$cr2 = '[{"type":"inject_css","css":"meta[name=referrer]{display:none}"}]'
$cr3 = '[{"type":"inject_css","css":"[id*=cookie-banner]{display:none !important}"}]'
$cr4 = '[{"type":"block","match":{"urlPattern":"*twitter.com/widgets*"}}]'

foreach ($ch in $targets) {
  $rules = @(
    @{ Name='Block social embeds'; Description='No FB/Twitter embeds'; Site='*'; Priority=10; RulesJson=$cr1; IsEnforced=$true; Username='admin' },
    @{ Name='Twitter widget block'; Description='Stop twitter widget tracking'; Site='*'; Priority=12; RulesJson=$cr4; IsEnforced=$true; Username='admin' },
    @{ Name='Strip referrer'; Description='No leaky Referer headers'; Site='*'; Priority=15; RulesJson=$cr2; IsEnforced=$false; Username='admin' },
    @{ Name='Hide cookie nags'; Description='Cookie banner suppression'; Site='*'; Priority=5; RulesJson=$cr3; IsEnforced=$false; Username='admin' }
  )
  foreach ($r in $rules) {
    try {
      PostJson "/api/channel/channels/$($ch.id)/rules" $r | Out-Null
    } catch { Write-Host "Failed to add rule to channel $($ch.name): $_" }
  }
  "Rules added to #$($ch.name)"
}
"Done"
