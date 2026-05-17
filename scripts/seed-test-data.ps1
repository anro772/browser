$ErrorActionPreference = 'Stop'

function PostJson($path, $body) {
  Invoke-RestMethod -Method Post -Uri "http://localhost:5000$path" `
    -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 8)
}

$pack1Rules = '[{"type":"inject_css","css":".cookie-banner,#onetrust-consent-sdk,[id*=cookie]{display:none !important}"}]'
$pack2Rules = '[{"type":"block","match":{"urlPattern":"*doubleclick.net*","resourceType":"xhr"}},{"type":"block","match":{"urlPattern":"*googleadservices.com*"}}]'
$pack3Rules = '[{"type":"inject_css","css":"body{background:#1a1a1a !important;color:#e0e0e0 !important}"}]'
$pack4Rules = '[{"type":"inject_css","css":"[data-testid=sidebarColumn] [data-testid=placementTracking]{display:none !important}"}]'
$pack5Rules = '[{"type":"inject_css","css":"[class*=paywall],[id*=paywall]{display:none !important}body,html{overflow:auto !important}"}]'
$pack6Rules = '[{"type":"block","match":{"urlPattern":"*google-analytics*"}},{"type":"block","match":{"urlPattern":"*hotjar*"}},{"type":"block","match":{"urlPattern":"*mixpanel*"}},{"type":"block","match":{"urlPattern":"*segment.io*"}}]'
$pack7Rules = '[{"type":"inject_css","css":"#mw-content-text{max-width:680px;margin:auto;font-size:17px;line-height:1.6}"}]'
$pack8Rules = '[{"type":"block","match":{"urlPattern":"*facebook.com/tr*"}},{"type":"block","match":{"urlPattern":"*connect.facebook.net*"}}]'

$packs = @(
  @{ Name='Cookie Banner Killer'; Description='Auto-dismisses cookie consent banners across the web. Reduces visual clutter on every site.'; Site='*'; Priority=10; RulesJson=$pack1Rules; AuthorUsername='priv_advocate'; Tags=@('privacy','cookies','annoyances') },
  @{ Name='YouTube Ad Blocker'; Description='Blocks pre-roll, mid-roll, and overlay ads on YouTube videos.'; Site='*.youtube.com'; Priority=20; RulesJson=$pack2Rules; AuthorUsername='video_freedom'; Tags=@('ads','youtube','video') },
  @{ Name='Force Dark Reddit'; Description='Forces a true dark theme on old.reddit.com for low-light browsing.'; Site='*.reddit.com'; Priority=5; RulesJson=$pack3Rules; AuthorUsername='dark_mode_fan'; Tags=@('theme','dark-mode','reddit') },
  @{ Name='Twitter Cleanup'; Description='Removes sidebar ads, promoted tweets, and the trending section.'; Site='*.x.com'; Priority=15; RulesJson=$pack4Rules; AuthorUsername='social_minimal'; Tags=@('twitter','clean-ui','distractions') },
  @{ Name='News Paywall Bypass'; Description='Disables overlay paywalls on common news sites. Use responsibly.'; Site='*'; Priority=8; RulesJson=$pack5Rules; AuthorUsername='news_reader'; Tags=@('news','paywall','reading') },
  @{ Name='Tracker Blocker Pro'; Description='Comprehensive blocklist for analytics, telemetry, and fingerprinting scripts.'; Site='*'; Priority=25; RulesJson=$pack6Rules; AuthorUsername='priv_advocate'; Tags=@('privacy','analytics','trackers') },
  @{ Name='Mobile-Style Wikipedia'; Description='Cleaner reading on Wikipedia with mobile-style typography and narrower columns.'; Site='*.wikipedia.org'; Priority=3; RulesJson=$pack7Rules; AuthorUsername='reader_one'; Tags=@('reading','wikipedia','typography') },
  @{ Name='Block FB Pixel Everywhere'; Description='Stops Facebook tracking pixel and Meta Connect across all sites.'; Site='*'; Priority=22; RulesJson=$pack8Rules; AuthorUsername='priv_advocate'; Tags=@('privacy','facebook','pixel','trackers') }
)

$createdPacks = @()
foreach ($p in $packs) {
  try {
    $r = PostJson '/api/marketplace/rules' $p
    $createdPacks += $r
  } catch { Write-Host "FAIL $($p.Name): $_" }
}
"Marketplace packs created: $($createdPacks.Count)"

$bumps = @(47, 121, 9, 33, 18, 256, 4, 88)
for ($i = 0; $i -lt $createdPacks.Count; $i++) {
  for ($n = 0; $n -lt $bumps[$i]; $n++) {
    try { Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/marketplace/rules/$($createdPacks[$i].id)/download" | Out-Null } catch {}
  }
}
"Download counts bumped"

$channels = @(
  @{ Name='acme-internal'; Description='ACME Corp internal browsing policy. Blocks competitor sites and enforces tracking opt-outs across all teams.'; OwnerUsername='admin'; Password='secret123'; IsPublic=$true },
  @{ Name='dev-team'; Description='Engineering team standards. Blocks distraction sites during work hours, enforces HTTPS upgrade.'; OwnerUsername='admin'; Password='devteam2026'; IsPublic=$true },
  @{ Name='journalism-pack'; Description='Source verification helpers for journalists. Blocks tracker scripts on news sites.'; OwnerUsername='admin'; Password='presspass'; IsPublic=$true }
)

$createdChannels = @()
foreach ($c in $channels) {
  try {
    $result = PostJson '/api/channel/channels' $c
    $createdChannels += $result
  } catch { Write-Host "Create channel failed for $($c.Name): $_" }
}
"Channels created: $($createdChannels.Count)"

$cr1 = '[{"type":"block","match":{"urlPattern":"*facebook.com/plugins*"}}]'
$cr2 = '[{"type":"inject_css","css":"meta[name=referrer]{display:none}"}]'
$cr3 = '[{"type":"inject_css","css":"[id*=cookie-banner]{display:none !important}"}]'
$cr4 = '[{"type":"block","match":{"urlPattern":"*twitter.com/widgets*"}}]'

foreach ($ch in $createdChannels) {
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
}
"Channel rules added"
"Done"
