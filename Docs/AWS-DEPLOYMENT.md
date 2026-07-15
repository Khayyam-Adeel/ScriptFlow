# Deploying ScriptFlow to AWS

Goal: run the full docker-compose stack (SQL Server, RabbitMQ, ScriptFlow.API,
Dispatch.Worker, Notification.Service, PharmacyGateway.mock, Angular UI) on AWS.

> **Current path:** you already launched an Ubuntu EC2 instance (`c7i-flex.large`,
> 2 vCPU / 4 GB RAM - fine for this stack, no swap needed) and connected via SSH.
> **[Manual deploy over SSH](#manual-deploy-over-ssh-current-path)** below gets you
> live today using that box directly. The Terraform + GitHub Actions section further
> down is a later upgrade path (IaC, no SSH/port 22, auto-deploy on push) - it
> provisions a *fresh* instance, so treat it as a future rebuild, not something to
> run against the box you already have.

---

## Manual deploy over SSH (current path)

### 0. What you need before starting
- The EC2 public IP (or Elastic IP if you allocated one) and your `.pem`/`.ppk` key.
- In the EC2 security group: inbound **22/tcp** from your IP (already open, since
  you connected) and **80/tcp** from `0.0.0.0/0`. Everything else (1433, 5672,
  15672, 5006, 5100, 5287) should stay closed - the prod compose override below
  stops publishing those ports at all, so the app doesn't need them open even
  internally-facing.

### 1. Install Docker on the instance
SSH in, then:
```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker $USER
# log out and back in (or `newgrp docker`) so the group change applies
docker compose version   # confirm >= 2.24 (needed for the !override tag below)
```

### 2. Get the code onto the instance
Simplest for a solo deploy - clone directly (repo doesn't need to be public if
you set up a deploy key, but a plain `git clone` works if it's public or you use
a PAT):
```bash
git clone https://github.com/<your-user>/ScriptFlow.git /opt/scriptflow
cd /opt/scriptflow
```

### 3. Create the production secrets file
```bash
cp .env.prod.example .env
nano .env
```
Fill in real values (generate strong ones locally with `openssl rand -base64 32`):
- `MSSQL_SA_PASSWORD`, `JWT_SIGNING_KEY`, `RABBITMQ_USER` / `RABBITMQ_PASSWORD`
- `PUBLIC_HOST` = the instance's public IP (e.g. `3.10.45.201`)

### 4. Bring the stack up
```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --build
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f db-init scriptflow-api
```
`db-init` must show `service_completed_successfully` before `scriptflow-api`
starts - first boot can take a few minutes while SQL Server initializes and the
schema/seed scripts run.

### 5. Smoke test
Open `http://<instance-public-ip>` in a browser:
- UI loads (nginx serving the Angular build)
- Log in works (proves `/api` → scriptflow-api proxy + JWT)
- Create/sign a prescription and watch it move through the stepper (proves
  RabbitMQ → dispatch-worker → pharmacy-gateway, and the `/hubs/prescriptions`
  SignalR proxy for live updates)

If the UI loads but API calls 502, check `docker compose logs scriptflow-api` and
confirm nginx's `/api/` proxy target (`scriptflow-api:8080`) matches the service
name in `docker-compose.yml`.

### 6. Redeploying after a code change
```bash
cd /opt/scriptflow
git pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --build
```

### 7. Keep it running after reboot
`restart: unless-stopped` (already in `docker-compose.prod.yml`) restarts
containers automatically after a reboot as long as the Docker daemon starts -
`docker-ce` enables that systemd unit by default, so no extra step needed.
Verify with `sudo systemctl is-enabled docker`.

### 8. Optional: HTTPS
Point a free DuckDNS subdomain at the public IP, put Caddy or nginx+certbot in
front, open 443 in the security group, update `PUBLIC_HOST`/CORS origins.

### 9. Cost watch
`c7i-flex.large` is **not** covered by the classic 12-month free tier (only
`t2.micro`/`t3.micro` are) - it draws down any signup credit. Set a billing
budget alert (Billing → Budgets) if you haven't, and stop the instance
(`aws ec2 stop-instances --instance-ids <id>`) when not actively demoing it -
EBS storage still bills while stopped, but compute doesn't.

---

## Terraform + GitHub Actions (future upgrade path)

The sections below provision a **new** instance from scratch via IaC with
SSM-only access (no SSH) and CI/CD auto-deploy. Revisit this once the manual
deploy above is working and you want to stop SSHing in by hand.

> **Reality check on "free tier":** accounts created after July 2025 get the *new*
> free tier — a $100 signup credit (plus up to ~$100 more for completing activities
> like setting a budget and launching an EC2 instance), valid ~6 months on the Free
> Plan. Usage *draws down the credits* rather than being free forever. SQL Server
> needs ~2 GB RAM by itself, so the 1 GB `t3.micro` can't run this stack. Expect the
> stack below to cost roughly **$20–40/month against your credits**, i.e. ~3–5 months
> of runtime. Verify current prices in the AWS Pricing Calculator — they drift.

---

## Architecture decision

**Recommended: one EC2 instance running your existing `docker-compose.yml`.**

| Option | Monthly (approx, us-east-1) | Verdict |
|---|---|---|
| 1× EC2 `t3.small` (2 GB) + swap + capped SQL memory | ~$19 (instance $15 + EBS $3 + public IPv4 ~$4, minus any free-tier IPv4 hours) | Best credit mileage; tight on RAM |
| 1× EC2 `t3.medium` (4 GB) | ~$37 | Comfortable; needed if you load the 1M-row perf dataset |
| ECS Fargate + RDS SQL Server Express | $70+ | Over-engineered for this; burns credits fast |

Why one VM: your compose file already orchestrates everything (healthchecks,
db-init ordering, networking). Reusing it means the EC2 box behaves exactly like
the intended local environment — and since Docker Desktop never ran locally (WSL2
missing), this will actually be the **first end-to-end run of the compose wiring**.
Budget time for first-deploy debugging.

Start with `t3.small` + 2 GB swap + `MSSQL_MEMORY_LIMIT_MB=1536`; resize to
`t3.medium` in Terraform (one line) if SQL Server gets OOM-killed.

---

## Phase 0 — Account prerequisites (console, one-time, ~30 min)

Do these **before** any IaC. You are currently on the **root account — stop using
it for daily work immediately**; root credentials leaking = total account loss.

1. **Secure root**: Console → IAM → enable **MFA on the root user** (authenticator app).
2. **Create an admin identity** (don't use root again after this):
   - Easiest: IAM → Users → `khayyam-admin` → attach `AdministratorAccess` →
     enable console access + MFA.
   - Create an **access key** for it (CLI use) — or better, skip access keys and use
     IAM Identity Center SSO later.
3. **Set a budget alert** (also earns free-tier bonus credits): Billing → Budgets →
   Zero-spend or $10 monthly budget with email alert to khayyam.adeel15761@gmail.com.
   This is your early warning before credits run out — **when Free Plan credits are
   exhausted or the 6-month window ends, AWS closes the account and deletes
   resources (after a grace period)**, so watch this.
4. **Pick one region** and stay in it. `us-east-1` (cheapest, everything available)
   or `eu-west-2`/`me-south-1` if latency matters to you.
5. **Local tooling** (your Windows machine):
   ```powershell
   winget install Amazon.AWSCLI
   winget install Hashicorp.Terraform
   aws configure          # paste the IAM admin access key, region, output=json
   aws sts get-caller-identity   # verify — should show the IAM user, NOT root
   ```

---

## Phase 1 — Repo prep (before Terraform)

1. **Production compose override** — add `docker-compose.prod.yml`:
   - `ASPNETCORE_ENVIRONMENT: Production` / `DOTNET_ENVIRONMENT: Production`
   - Real secrets from environment (`${MSSQL_SA_PASSWORD}`, `${JWT_SIGNING_KEY}`)
     instead of the hardcoded `Your_password123` / `CHANGE_ME...`
   - Non-default RabbitMQ user/password
   - `Cors__AllowedOrigins__0: http://<elastic-ip>` (later: your domain)
   - Only expose ports 80 (UI) and 5006/5100 if the UI calls them directly from the
     browser — do **not** publish 1433, 5672, 15672 publicly
   - `restart: unless-stopped` on every service
   - Cap SQL memory: `MSSQL_MEMORY_LIMIT_MB: "1536"` under the sqlserver service
     (needed on t3.small)
2. **Check the Angular API base URL**: if the UI has `http://localhost:5006` baked
   into `environment.prod.ts`, it will break on AWS. Point it at a relative path and
   proxy `/api` → scriptflow-api in the UI's nginx config, or set it to the EIP.
3. **Images**: CI will build and push all 5 images to **GHCR** (ghcr.io — free with
   your GitHub account, avoids ECR storage limits/cost). The EC2 box pulls from GHCR;
   compose on the server uses `image:` tags instead of `build:`.

---

## Phase 2 — Terraform (the IaC)

Layout: `infra/` at repo root.

```
infra/
  main.tf         # provider, VPC lookup, SG, EC2, EIP
  variables.tf
  outputs.tf      # public IP
  user_data.sh    # cloud-init: docker install, swap, app bootstrap
```

What it provisions (all in the **default VPC** — no need to build a VPC for this):

- **Security group**: inbound 80/tcp (and 443 later) from `0.0.0.0/0`; **no port 22**
  — use SSM Session Manager instead of SSH (no keys to leak).
- **IAM role + instance profile** with `AmazonSSMManagedInstanceCore` (Session
  Manager shell + remote deploy commands) and read access to the SSM parameters below.
- **SSM Parameter Store (SecureString)** for secrets: `/scriptflow/sa-password`,
  `/scriptflow/jwt-signing-key`, `/scriptflow/rabbitmq-password`, `/scriptflow/ghcr-token`
  (a GitHub read-only PAT so the box can pull images).
- **EC2 instance**: `t3.small`, Ubuntu 24.04 or Amazon Linux 2023, 30 GB gp3 EBS.
- **Elastic IP** so the address survives instance replacement.
- **user_data.sh**: create 2 GB swapfile, install docker + compose plugin, log into
  GHCR, fetch secrets from SSM into `/opt/scriptflow/.env`, clone/copy the compose
  files, `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d`.

Run it:
```powershell
cd infra
terraform init
terraform plan          # review everything it will create
terraform apply
terraform output public_ip
```

State: for a solo project, local `terraform.tfstate` is fine — **git-ignore it**
(it contains secrets). Optional later: S3 backend + state locking.

First-boot check (no SSH needed):
```powershell
aws ssm start-session --target <instance-id>
# then on the box:
sudo docker compose -f ... ps
sudo docker compose -f ... logs db-init scriptflow-api
```
Expect to debug here — the compose healthcheck/ordering wiring has never run.

---

## Phase 3 — GitHub Actions

Two workflows in `.github/workflows/`:

### `ci.yml` — on every PR and push
- `dotnet build` + `dotnet test` (this also closes the assignment's CI gap)
- `npm ci && npm run build` for the Angular app
- On PRs: stop there (no deploy).

### `deploy.yml` — on push to `main` (after CI passes)
1. **Build & push images** to GHCR — matrix over the 5 Dockerfiles, tagged
   `ghcr.io/<your-user>/scriptflow-<service>:latest` + `:${{ github.sha }}`.
   Auth is the built-in `GITHUB_TOKEN` (`permissions: packages: write`) — nothing to configure.
2. **Authenticate to AWS via OIDC** — no long-lived AWS keys in GitHub:
   - Terraform additionally creates an IAM **OIDC identity provider** for
     `token.actions.githubusercontent.com` and a role trust-scoped to
     `repo:<your-user>/ScriptFlow:ref:refs/heads/main`, allowing only
     `ssm:SendCommand` on the instance.
   - Workflow uses `aws-actions/configure-aws-credentials@v4` with `role-to-assume`.
3. **Deploy** — one SSM Run Command:
   ```
   cd /opt/scriptflow && docker compose pull && docker compose up -d && docker system prune -f
   ```

Secrets needed in GitHub: **none for AWS** (OIDC) — just the role ARN as a
repo variable.

---

## Phase 4 — After it's up

- Smoke test: `http://<elastic-ip>` loads the UI, log in, create a prescription,
  watch it move through the stepper (proves RabbitMQ + worker + gateway path).
- **HTTPS (recommended before showing anyone)**: point a free DuckDNS subdomain at
  the EIP, put Caddy in front (2-line config, auto Let's Encrypt) or use nginx +
  certbot. Then add 443 to the security group and update CORS origins.
- **Cost watch**: check the budget email + Billing → Free Tier usage weekly.
  Stopping the instance when not demoing (`aws ec2 stop-instances`) stops the
  instance charge (EBS ~$3/mo and the idle EIP still bill).
- **Teardown when done**: `terraform destroy` removes everything.

## Order of operations (TL;DR)

1. Root MFA → IAM admin user → budget alert → AWS CLI + Terraform installed.
2. Add `docker-compose.prod.yml` + fix Angular API base URL.
3. Write `infra/` Terraform → `terraform apply` → debug first `docker compose up` via SSM.
4. Add `ci.yml` + `deploy.yml` (GHCR images, OIDC → SSM deploy).
5. DuckDNS + Caddy for HTTPS; watch the budget.
