# Azure Deployment — One-Time Setup

This document covers the one-time provisioning needed before `deploy.yml` can ship code
to Azure. Once it's done, every push to `main` builds, tests, publishes and deploys
automatically.

Architecture (single Web App, "Blazor Hosted" pattern):

- **App Service** runs `TimeTracker.Api`, which serves both the REST API and the Blazor
  WASM client (Web project's `wwwroot/` ships inside the API's publish output).
- **Azure SQL Database** holds all data. EF Core migrations run on startup
  (`db.Database.MigrateAsync()` in `Program.cs`) so deploys self-heal the schema.
- **GitHub Actions** is the only deployment path; no manual `az webapp deploy`.

Only the **production** environment exists today.

---

## 1. Create Azure resources

Pick a region you'll use throughout (`eastus`, `westus2`, etc.). Names are global within
their service, so prefix to taste (e.g. `tt-` for TimeTracker).

### Option A — Azure Portal (click-ops, easier for the first run)

1. **Resource Group** — e.g. `tt-prod-rg`.
2. **Azure SQL** — Create *SQL Database* (creates the logical server alongside if needed):
   - Server name e.g. `tt-prod-sql` (`.database.windows.net` appended automatically).
   - Authentication: SQL authentication. Set a server admin login + strong password — save
     these; they go into the connection string.
   - Database name e.g. `timetracker`.
   - Compute tier: **General Purpose → Serverless** is the cheapest sane prod choice; it
     auto-pauses when idle. Min vCores 0.5, max 1.
   - **Networking**: enable *Allow Azure services and resources to access this server*.
     (Without this, the App Service can't reach the DB.)
3. **App Service** — Create *Web App*:
   - Name e.g. `timetracker-prod` (this becomes `<name>.azurewebsites.net` and goes in the
     `AZURE_WEBAPP_NAME` secret).
   - Publish: **Code**. Runtime stack: **.NET 10 (LTS)**. OS: **Linux**.
   - Plan: a new **Basic B1** plan is fine for prod-of-one; scale up later.

### Option B — Azure CLI (reproducible)

```bash
RG=tt-prod-rg
LOC=eastus
SQL_SERVER=tt-prod-sql
SQL_DB=timetracker
SQL_ADMIN=ttadmin
SQL_PASSWORD='<paste-a-strong-password>'
APP_NAME=timetracker-prod
PLAN=tt-prod-plan

az group create -n $RG -l $LOC

az sql server create -g $RG -n $SQL_SERVER -l $LOC \
  --admin-user $SQL_ADMIN --admin-password "$SQL_PASSWORD"
az sql server firewall-rule create -g $RG --server $SQL_SERVER \
  -n AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
az sql db create -g $RG --server $SQL_SERVER -n $SQL_DB \
  --edition GeneralPurpose --family Gen5 --capacity 1 --compute-model Serverless \
  --auto-pause-delay 60

az appservice plan create -g $RG -n $PLAN --sku B1 --is-linux
az webapp create -g $RG -p $PLAN -n $APP_NAME --runtime "DOTNETCORE:10.0"
```

---

## 2. Configure the App Service

### Connection string

`App Service → Settings → Environment variables → Connection strings`. Add:

- **Name**: `TimeTracker`
- **Type**: `SQLAzure`
- **Value**:

```
Server=tcp:<sql-server>.database.windows.net,1433;Database=timetracker;User ID=<sql-admin>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

The API resolves `ConnectionStrings:TimeTracker` (see `Infrastructure/DependencyInjection.cs`).
Connection strings set this way are exposed to the app as `SQLAZURECONNSTR_TimeTracker`
and override the `appsettings.json` value automatically — no code change needed.

### App settings

Same screen, *Application settings* tab:

- `ASPNETCORE_ENVIRONMENT` = `Production` (default is fine, but set it explicitly).
- `Auth__Authority`, `Auth__Audience` — paste the production OIDC issuer + audience once
  the real identity provider is wired up (Program.cs reads `Auth:Authority` / `Auth:Audience`).
  **Until then**, the dev backdoor is the only auth path; do not expose the site publicly
  without setting these.

### Logs

`Monitoring → App Service logs` → turn on **Application logging (Filesystem)** at level
*Information*. Lets you tail `az webapp log tail` when a deploy misbehaves.

---

## 3. Wire GitHub Actions to the App Service

### Download the publish profile

`App Service → Overview → ⋯ → Get publish profile`. Saves an XML file. Open it in any
text editor; you'll copy the **entire** contents into a GitHub secret.

### Create GitHub secrets

In the repo: `Settings → Secrets and variables → Actions → New repository secret`.

| Name | Value |
| --- | --- |
| `AZURE_WEBAPP_NAME` | The App Service name, e.g. `timetracker-prod` |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Full XML body of the downloaded publish profile |

### Create the `production` environment

`Settings → Environments → New environment → production`. The deploy job in
`deploy.yml` already targets this environment. Optional but recommended:

- **Required reviewers** — add yourself; deploys then pause for a one-click approval.
- **Deployment branches** — restrict to `main`.

---

## 4. First deploy

Push any commit to `main`. The workflow runs:

1. `dotnet restore` / `build` / `test`.
2. `dotnet publish` the API — the Web project's WASM bundle is included automatically via
   the project reference in `TimeTracker.Api.csproj`.
3. `azure/webapps-deploy@v3` uploads the publish folder.

On the first request after deploy, `Program.cs` runs `db.Database.MigrateAsync()` and
the schema is created in the empty Azure SQL database.

**Sanity check**: open `https://<APP_NAME>.azurewebsites.net/`. The Blazor login/home
page should load; `/api/me` returns 401 (no auth wired yet). If the page is blank,
`az webapp log tail -g <rg> -n <app>` will show the EF connection failure or middleware
exception.

---

## 5. Day-2 operations

- **Roll back**: redeploy a prior commit from the Actions tab → workflow run → *Re-run all jobs*.
- **Schema changes**: ship the migration with the code; it applies on the next startup.
  If a migration is destructive, take a database export first (`SQL Database → Export`).
- **Scale**: bump the App Service Plan SKU (Basic → Standard) or the SQL serverless cap
  in-place. No code change needed.
- **Secrets rotation**: regenerate the publish profile, replace `AZURE_WEBAPP_PUBLISH_PROFILE`,
  and the next deploy picks up the new one.
