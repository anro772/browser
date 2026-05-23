# Deploying the Slate backend to Fly.io + Neon

Goal: stand up `BrowserApp.Server` at `https://<your-app>.fly.dev` so two
Slate installations on different machines share a single Postgres-backed
catalog of channels and marketplace packs.

Cost on free tier: **$0/month**. Fly.io's free allowance fits one
shared-cpu-1x machine; Neon's free tier gives 0.5 GB of Postgres.

## One-time setup

### 1. Accounts

- **Fly.io** — sign up at https://fly.io. Install `flyctl`:
  ```powershell
  iwr https://fly.io/install.ps1 -useb | iex
  flyctl auth login
  ```
- **Neon** — sign up at https://neon.tech, create a new project (any
  name). On the project dashboard, copy the **pooled** connection string
  (looks like `postgres://user:pass@ep-xxx-pooler.us-east-2.aws.neon.tech/neondb?sslmode=require`).

### 2. Create the Fly app

From the **repo root** (the `fly.toml` lives there):

```powershell
flyctl apps create slate-api          # or whatever name you want; update fly.toml's `app = "..."` line to match
```

If `slate-api` is taken, pick a different name. Keep it short — it
becomes part of the URL.

### 3. Store the Neon connection string as a Fly secret

```powershell
flyctl secrets set "ConnectionStrings__PostgreSQL=postgres://USER:PASS@ep-xxx-pooler.us-east-2.aws.neon.tech/neondb?sslmode=require"
```

The double-underscore (`__`) is how .NET reads nested config keys from
environment variables — this maps to
`ConnectionStrings:PostgreSQL` in `appsettings.json`.

### 4. Deploy

```powershell
flyctl deploy
```

First deploy takes ~3-5 minutes. The Dockerfile builds the .NET 8
image, pushes to Fly's registry, and starts a machine. `Database.Migrate()`
runs on startup and creates the schema in Neon automatically.

### 5. Verify

```powershell
flyctl status                                       # machine running
curl https://slate-api.fly.dev/api/health           # 200 OK
```

Open `https://slate-api.fly.dev/swagger` in a browser — Swagger UI
should load.

### 6. Populate the cloud catalog

```powershell
.\scripts\seed-test-data.ps1 -BaseUrl https://slate-api.fly.dev
```

That creates 8 marketplace packs + 3 channels with their rules. Idempotent
to re-run — the API already de-duplicates on name.

### 7. Point the Slate client at the cloud

Edit `BrowserApp.UI/appsettings.json`:

```json
{
  "MarketplaceApi": {
    "BaseUrl": "https://slate-api.fly.dev"
  }
}
```

Then repackage and reinstall as usual:

```powershell
.\publish.ps1
iscc installer\BrowserApp.iss
# Run the new installer from publish\installer\Slate-Setup-0.1.0.exe
```

## Per-version redeploy

Code change in `BrowserApp.Server` → `flyctl deploy` from repo root.
That's the whole loop.

## Diagnostics

```powershell
flyctl logs                       # live tail of server stdout/stderr
flyctl ssh console                # shell into the running container
flyctl machine restart            # bounce the machine after a stuck deploy
flyctl secrets list               # confirm the connection string is set
```

If the server fails to start, the most common cause is the connection
string. `flyctl logs` will show
`Npgsql.NpgsqlException: Failed to connect ...` — verify Neon's pooler
URL is what you set, and `?sslmode=require` is present.

## Free-tier caveats

- **Cold start:** Fly stops the machine after idle. First request after
  idle adds ~3-5 s. Subsequent requests are warm. The Slate client
  tolerates this — `CheckConnectionAsync` has a 30 s timeout.
- **Neon idle:** Neon also auto-suspends after 5 min of inactivity on
  free tier. First query after suspend adds ~1 s. Auto-resumes.
- **Bandwidth:** Fly free tier has 160 GB/month outbound. Slate's
  60-second auto-refresh moves ~5 KB per tick — nowhere near the limit.

## Going to a custom domain (optional, later)

```powershell
flyctl certs add slate.your-domain.tld
# Add the DNS records Fly tells you to.
```

Then update `appsettings.json` BaseUrl and repackage.
