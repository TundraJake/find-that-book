# Deployment (scaffold — not live yet)

## Topology
```
GitHub (dev, main)
  └─ GitHub Actions: CI → build image → push to DigitalOcean Container Registry → deploy App Platform
                                                                                       │
Browser → Cloudflare (DNS + proxy, TLS) → DigitalOcean App Platform (container: API + React in wwwroot)
                                                                                       └─ Gemini API, Open Library
```

| Branch | Environment | URL | App Platform app |
|---|---|---|---|
| `dev` | staging | `https://findthatbook-dev.explorator-maris.com` | `find-that-book-staging` |
| `main` | production | `https://findthatbook.explorator-maris.com` | `find-that-book` |

Flow: feature branch → PR into `dev` (CI runs) → merge deploys staging → PR `dev` → `main` → merge deploys production.

## Files
| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage: builds React, publishes the API, serves React from `wwwroot` on port 8080. |
| `.dockerignore` | Keeps build output, secrets, docs and tests out of the image. |
| `.github/workflows/ci.yml` | PRs into `dev`/`main`: .NET build + test, client lint + build, Docker build (no push). Also reused by Deploy. |
| `.github/workflows/deploy.yml` | Push to `dev`/`main`: CI → push image → deploy App Platform spec → purge Cloudflare cache → smoke test `/api/health`. |
| `.do/app.yaml` | App Platform spec template; `${...}` values come from the workflow environment. |

The deploy job is **skipped** until the repo variable `DEPLOY_ENABLED` is `true`, so pushes are safe before setup.

## One-time setup checklist

### DigitalOcean
- [ ] Create a Container Registry (note its name → `DIGITALOCEAN_REGISTRY`).
- [ ] Create a personal access token with App Platform + Registry write scopes → `DIGITALOCEAN_ACCESS_TOKEN`.
- [ ] First deploy creates the apps from `.do/app.yaml`; note each app's default `*.ondigitalocean.app` hostname.

### Cloudflare (`explorator-maris.com` zone)
- [ ] Add `CNAME findthatbook → <production-app>.ondigitalocean.app` and `CNAME findthatbook-dev → <staging-app>.ondigitalocean.app`.
- [ ] Start the records **DNS only (grey cloud)** until App Platform shows the domain as Active (its certificate validation needs to reach DigitalOcean directly), then switch to **Proxied (orange cloud)**.
- [ ] SSL/TLS mode: **Full (strict)**.
- [ ] API token scoped to this zone with `Cache Purge` → `CLOUDFLARE_API_TOKEN`; zone ID → `CLOUDFLARE_ZONE_ID`.
- [ ] Optional: cache rule to bypass cache for `/api/*`.
- [ ] Rules → Settings → Managed Transforms → enable **Add visitor location headers** (free plan). Without it the search audit log records only the IP and country.

### GitHub repository settings
- [ ] Environments `staging` and `production` (add required reviewers on `production` if wanted).
- [ ] Repository secrets: `DIGITALOCEAN_ACCESS_TOKEN`, `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ZONE_ID`.
- [ ] Repository variables: `DIGITALOCEAN_REGISTRY`, `DEPLOY_ENABLED=true` (when ready).
- [ ] Per-environment secret: `GEMINI_API_KEY`.
- [ ] Per-environment variables:

| Variable | staging | production |
|---|---|---|
| `APP_NAME` | `find-that-book-staging` | `find-that-book` |
| `APP_DOMAIN` | `findthatbook-dev.explorator-maris.com` | `findthatbook.explorator-maris.com` |
| `ASPNETCORE_ENVIRONMENT` | `Staging` | `Production` |

- [ ] Branch protection on `main` and `dev`: require PR + passing `CI` checks; no direct pushes to `main`.

## Open items
- Confirm the `${VAR}` substitution behavior of `digitalocean/app_action/deploy@v2` against its current README before the first real deploy.
- Pin GitHub Action versions to commit SHAs once the pipeline is proven.
- Search audit log: the client IP comes from `cf-connecting-ip` / `do-connecting-ip` / `X-Forwarded-For`, read only because `SearchAudit__TrustProxyHeaders=true` is set in `.do/app.yaml`. App Platform's filesystem is wiped on every redeploy, so the `.jsonl` files there are short-lived; each entry is also written to the app log (App Platform → Runtime Logs). Durable storage (database table or log forwarding) is an open decision.
