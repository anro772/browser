# Self-Hosted LLM via Cloudflare Tunnel + Caddy

Slate's Copilot sidebar talks to an Ollama instance. On the developer's PC
this runs locally. For distributed installs (laptop, friends), the PC's
Ollama is exposed through a Cloudflare Tunnel with a Caddy reverse proxy
handling bearer-token authentication.

## Architecture

```
Friend's Slate ── HTTPS ──► Cloudflare edge ──► Tunnel ──► PC
                                                            │
                                               Caddy :11435 (bearer auth)
                                                            │
                                                 Ollama :11434 (localhost only)
```

- **Ollama** binds to `localhost:11434` — never exposed to the network directly.
- **Caddy** listens on `:11435`, validates `Authorization: Bearer <token>`,
  proxies to Ollama with `Host` rewritten to `localhost:11434` (Ollama
  rejects non-localhost Host headers).
- **Cloudflare Tunnel** (`cloudflared`) creates an outbound-only encrypted
  connection to Cloudflare's edge. No port-forwarding, no public IP exposure.
- **Cloudflare** terminates TLS, provides DDoS protection, and routes
  `slate-llm.anro-slate.org` to the tunnel.

## Components on the PC

| Process | How it runs | Auto-starts? |
|---------|-------------|-------------|
| Ollama | Tray app (auto-launches on Windows login) | After login |
| CaddySlate | Windows service via NSSM | At boot |
| CloudflaredSlate | Windows service via NSSM | At boot |

## Configuration files

### Caddyfile — `C:\ProgramData\Caddy\Caddyfile`

```caddyfile
:11435 {
    @authed header Authorization "Bearer <YOUR_TOKEN>"
    handle @authed {
        reverse_proxy localhost:11434 {
            header_up Host {upstream_hostport}
        }
    }
    respond 401
}
```

The `header_up Host {upstream_hostport}` line rewrites the Host header
to `localhost:11434` so Ollama's origin check passes when requests
arrive via the tunnel with `Host: slate-llm.anro-slate.org`.

### Cloudflared config — `C:\Users\stefa\.cloudflared\config.yml`

```yaml
tunnel: bc0b3269-06d8-4e8d-83ed-74bd5025a598
credentials-file: C:\Windows\System32\config\systemprofile\.cloudflared\bc0b3269-06d8-4e8d-83ed-74bd5025a598.json

ingress:
  - hostname: slate-llm.anro-slate.org
    service: http://localhost:11435
  - service: http_status:404
```

A copy of the credentials file + config.yml also lives at
`C:\Windows\System32\config\systemprofile\.cloudflared\` so the
LocalSystem-context NSSM service can read them.

### Slate client — `BrowserApp.UI/appsettings.json`

```json
"Ollama": {
  "BaseUrl": "https://slate-llm.anro-slate.org",
  "Model": "llama3.2",
  "AuthToken": "<YOUR_TOKEN>"
}
```

When `AuthToken` is empty, `OllamaClient` sends no auth header
(local-Ollama use, no proxy needed).

## Day-to-day operations

### Check status

```powershell
Get-Service CaddySlate, CloudflaredSlate
Get-Process ollama
```

### Stop the LLM service (friends lose access)

```powershell
Stop-Service CaddySlate, CloudflaredSlate
```

### Start it back up

```powershell
Start-Service CaddySlate, CloudflaredSlate
```

### Disable auto-start at boot

```powershell
Set-Service CaddySlate -StartupType Disabled
Set-Service CloudflaredSlate -StartupType Disabled
```

### Re-enable auto-start

```powershell
Set-Service CaddySlate -StartupType Automatic
Set-Service CloudflaredSlate -StartupType Automatic
```

### Health check

```powershell
curl.exe https://slate-llm.anro-slate.org/api/tags -H "Authorization: Bearer <TOKEN>"
```

Returns JSON model list if everything is up.

## Editing the setup

### Rotate the bearer token

1. Generate new token: `[guid]::NewGuid().ToString("N")`
2. Update `C:\ProgramData\Caddy\Caddyfile` with the new token.
3. Reload Caddy: `Restart-Service CaddySlate`
4. Update `BrowserApp.UI/appsettings.json` → `Ollama.AuthToken`.
5. Repackage Slate: `.\publish.ps1 && iscc installer\BrowserApp.iss`
6. Old installers stop working immediately after step 3.

### Change the Ollama model

```powershell
ollama pull mistral           # download a new model
ollama list                   # verify it's available
```

Then edit `appsettings.json` → `Ollama.Model` and repackage. Or
the user can switch models live in the Copilot sidebar's model picker.

### Add Ollama as a boot service (optional — headless PC)

If you want the LLM to work even when nobody is logged into the PC:

```powershell
# Quit the Ollama tray app first
$ollamaPath = (Get-Command ollama).Source
nssm install OllamaService $ollamaPath serve
nssm set OllamaService Start SERVICE_AUTO_START
nssm set OllamaService AppEnvironmentExtra "OLLAMA_MODELS=$env:USERPROFILE\.ollama\models"
Start-Service OllamaService
```

## Costs

- **Cloudflare Tunnel**: free (unlimited bandwidth, unlimited tunnels).
- **Domain** (`anro-slate.org`): ~$10/year via Cloudflare Registrar.
- **PC electricity**: negligible. Idle Ollama uses ~5 MB RAM, 0% CPU.
  GPU only activates during inference (~2-10s per request).
- **Caddy + cloudflared**: combined ~30 MB RAM idle.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| `error code: 1033` | Tunnel not connected | `Get-Service CloudflaredSlate` — if stopped, `Start-Service`. Check logs: `Get-Content C:\ProgramData\Caddy\cloudflared.log -Tail 30` |
| HTTP 403 from tunnel | Ollama rejecting non-localhost Host | Verify Caddyfile has `header_up Host {upstream_hostport}` inside `reverse_proxy` block |
| HTTP 401 when token is correct | Whitespace in token or Caddyfile encoding issue | Open Caddyfile in a text editor, verify no trailing spaces. `Restart-Service CaddySlate`. |
| Caddy won't start (port conflict) | Another Caddy running in foreground | `Get-Process caddy \| Stop-Process -Force`, then `Start-Service CaddySlate` |
| `Stop-Service` hangs for cloudflared | Graceful drain timeout | `Get-Process cloudflared \| Stop-Process -Force` from another window |
| DNS "could not resolve" on PC | Local DNS cache stale | `ipconfig /flushdns` or wait ~5 min for propagation |
