# Deploying EcommerceApp

The server pulls releases. Nothing outside it ever connects to it: no SSH from CI, no self-hosted runner, no open port for a pipeline.

```
push to main
  -> GitHub Actions: build + test, then publish to GHCR (only GITHUB_TOKEN, packages: write on that one job)
        ecommerce-migrations:<sha>   the EF Core migration bundle
        ecommerce-api:<sha>          the API
        ecommerce-api:main           moved last, to the same commit
  -> server, every 5 minutes (systemd timer): deploy.sh
        pull "main", read its commit from the image label, stop if that commit is already running
        pull both images, run the bundle, switch the containers, wait for /health
        not healthy -> start the previous release again
```

## What a release is

| Piece | How it is built and shipped |
|---|---|
| API image | `Ecommerce.API/Dockerfile`, tagged with the full commit SHA. |
| Migration bundle | `deploy/migrations.Dockerfile` builds a self-contained `efbundle` (linux-x64) and puts it in a small image (`runtime-deps`, no SDK), tagged with the same SHA. |
| "Latest" | The `main` tag on `ecommerce-api`, pushed after the other two. The image has an `org.opencontainers.image.revision` label with its commit, which is how the server learns the SHA. |

The bundle ships as an image, not as a release asset or an OCI artifact, because the server already needs `docker pull` and a public GHCR image needs no token. The same `docker compose` file runs it (`migrate` service, profile `tools`) with the same connection string as the API.

## Rules the script follows

- The bundle runs first. If it fails, nothing is switched and the old version keeps running. The failed commit is remembered, so the timer does not retry it every 5 minutes; a newer commit, or `deploy.sh --tag <sha>`, tries again.
- After the switch the script waits for the container healthcheck (`GET /health`: PostgreSQL and Redis both answer). If it never becomes healthy, the previous release is started again and the run fails (exit code 1, visible in `journalctl`).
- Nothing happens when the published commit is already running.
- Rolling back is redeploying the previous commit: `sudo -u ecommerce-deploy /opt/ecommerce/deploy/deploy.sh --tag <sha>` (the last two are in `/var/lib/ecommerce-deploy/current` and `previous`). Migrations are not reverted, so write each migration so the previous release still works with the new schema (add first, remove in a later release).
- The only secret on the server is `/etc/ecommerce/.env.prod`. The script refuses to run unless it is mode 0600 and owned by the user running it.

## One-time server setup

Run on the server as a sudo user. Docker Engine and the compose plugin are already installed.

```bash
# 1. The deploy user: no login shell, no sudo, only the docker group.
#    Members of the docker group can start containers as root: treat this user like root and keep it unused for anything else.
sudo useradd --system --create-home --home-dir /var/lib/ecommerce-deploy --shell /usr/sbin/nologin ecommerce-deploy
sudo usermod -aG docker ecommerce-deploy

# 2. The code (public repo, so no credentials). The script checks out the exact commit it deploys.
sudo git clone https://github.com/sayyedulawwab/EcommerceApp.git /opt/ecommerce
sudo chown -R ecommerce-deploy:ecommerce-deploy /opt/ecommerce

# 3. The secrets file: copy .env.example, fill in the values, owner-only.
sudo install -d -m 0750 -o ecommerce-deploy -g ecommerce-deploy /etc/ecommerce
sudo install -m 0600 -o ecommerce-deploy -g ecommerce-deploy /opt/ecommerce/.env.example /etc/ecommerce/.env.prod
sudo -u ecommerce-deploy nano /etc/ecommerce/.env.prod   # POSTGRES_*, JWT_SECRET_KEY (32+ random characters)

# 4. The infrastructure (PostgreSQL, Redis, observability). It creates the shared docker network and keeps running between deploys.
sudo -u ecommerce-deploy docker compose -f /opt/ecommerce/infra.compose.yml --env-file /etc/ecommerce/.env.prod up -d

# 5. The timer.
sudo cp /opt/ecommerce/deploy/ecommerce-deploy.service /opt/ecommerce/deploy/ecommerce-deploy.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now ecommerce-deploy.timer

# First deploy right now, and its log:
sudo systemctl start ecommerce-deploy.service
journalctl -u ecommerce-deploy.service -n 50 --no-pager
```

GitHub side, once: after the first publish, open each package (`ecommerce-api`, `ecommerce-migrations`) under your profile -> Packages -> Package settings -> Change visibility -> Public. The packages are linked to the repo by the `org.opencontainers.image.source` label. Public images are what let the server pull without a token.

Network notes:

- Nothing needs to reach the server from outside. The server only makes outbound HTTPS calls (GHCR, github.com).
- `infra.compose.yml` publishes PostgreSQL (5432), Redis (6379) and the observability ports on all interfaces, and Docker ignores `ufw` for published ports. On a server that is reachable from outside the LAN, remove those `ports:` entries from the infra stack or bind them to `127.0.0.1`.
- `nginx/nginx.prod.conf` has `api.example.com` and expects Let's Encrypt certificates in `/etc/letsencrypt`. Edit it for your domain, or call the API on the container network until you have one.

## Azure Pipelines instead

`azure-pipelines.yml` at the repo root is the same deployment run by Azure DevOps agents on the same server. Use one deployer per server: if you use Azure Pipelines, do not enable the timer (`sudo systemctl disable --now ecommerce-deploy.timer`), or the two will replace each other's release. In that mode the images are built on the server and never pushed to a registry, and the secrets come from a variable group instead of `/etc/ecommerce/.env.prod`.
