# Deploying EcommerceApp

Azure DevOps builds and deploys on one Linux server, through an agent that runs on the server itself. Nothing outside connects to the server: the agent calls out to Azure DevOps.

```
pull request
  -> GitHub Actions (.github/workflows/ci.yml): build, test, prove the images build
     main accepts the merge only when build-and-test passed

merge to main
  -> Azure DevOps (azure-pipelines.yml), on the server's agent
       Build               docker build ecommerce-api and ecommerce-migrations, tagged with the run number
                           (e.g. 20261114.1), then a Trivy scan (report only). Nothing is pushed anywhere.
       Deploy staging      automatic
       Deploy production   started by hand, from the same run, once staging looks right
     each deploy (deploy/deploy.sh): preflight -> migrations -> new container -> healthy -> switch -> smoke test
```

Pull requests never run on the server: GitHub's hosted runners build and test them, and the Azure pipeline only runs for `main` (`pr: none`).

## What runs on the server

| Piece | From | Where |
|---|---|---|
| Traefik + a read-only Docker socket proxy | `deploy/edge/` | `/opt/edge`, ports 80 (redirects) and 443 |
| PostgreSQL, Redis, OpenTelemetry collector, Loki, Tempo, Prometheus, Grafana | `infra.compose.yml` | `/opt/ecommerce/infra`, shared by both environments |
| The API, per environment | `deploy/app.compose.yml` | `/opt/ecommerce/<env>/releases/<time>_<tag>/`, `current` points at the running one |

Both environments share PostgreSQL (a database each: `POSTGRES_DB`) and Redis (a database number each: `REDIS_DATABASE`, so their caches never mix). Traefik routes by hostname (`PUBLIC_HOST`) and only sends traffic to a container whose healthcheck (`GET /health`: PostgreSQL and Redis answer) passes. The API trusts `X-Forwarded-For` only from Traefik's fixed address, `172.29.0.2`.

## How a deploy works

`deploy/deploy.sh` runs as the agent user. In order:

1. **Preflight.** Both images are on the server, the networks exist, Traefik runs, PostgreSQL and Redis are healthy, every required variable is set and `JWT_SECRET_KEY` has at least 32 bytes. Nothing has changed yet if any of this fails.
2. **Release folder.** `app.compose.yml` plus a `.env` (mode 0600) written from the variable group, checked with `docker compose config`.
3. **Migrations.** The EF Core migration bundle of the same commit runs against the environment's database. If it fails, nothing is switched.
4. **Switch**, by strategy:
   - `bluegreen` (the default): the new container starts in the other slot (`blue`/`green`) next to the running one. Traefik balances across both once the new one is healthy; then the old one stops. No downtime. A release that never gets healthy is removed and the running one is never touched.
   - `recreate`: stop the running container, start the new one. A few seconds of downtime; if it fails, the previous release starts again.
5. **Smoke test** through Traefik with the real hostname (`https://$PUBLIC_HOST/health` must return `Healthy`). If it fails, the old container starts again.
6. **Retention.** The newest 5 release folders per environment and every image tag a kept release uses stay; older ones are removed.

The running release keeps serving while migrations run, and with `bluegreen` the old and new release run side by side for a moment. So write each migration so the previous release still works with the new schema: add first, remove in a later release. Migrations are never reverted.

### Rolling back

On the server, as the agent user, start an earlier release folder again. Its `.env` has everything it needs, and its images are kept by retention. A few seconds of downtime:

```bash
sudo -u azp bash
cd /opt/ecommerce/production
ls releases/                                     # current points at the running one; pick an earlier one
release() { docker compose -p "ecommerce-production-$(sed -n "s/^SLOT='\(.*\)'$/\1/p" "$1/.env")" \
  --project-directory "$1" -f "$1/app.compose.yml" --env-file "$1/.env" "${@:2}"; }
prev="$(readlink -f releases/<time>_<tag>)"
release "$(readlink -f current)" down
release "$prev" up -d
ln -sfn "$prev" current
```

Never run `docker system prune -a` on the server: it deletes the images a rollback needs.

## One-time setup

### 1. The server

An Ubuntu Server with Docker Engine and the compose plugin ([docs.docker.com/engine/install/ubuntu](https://docs.docker.com/engine/install/ubuntu/)).

**The agent.** In Azure DevOps, create the agent pool `ecommerce-vm` (Project settings > Agent pools > Add pool > Self-hosted). It must hold only this agent: the images live in this server's Docker daemon. Then, on the server, a user for the agent and nothing else:

```bash
sudo useradd --create-home --shell /bin/bash azp
sudo chmod 750 /home/azp
sudo -iu azp
mkdir agent && cd agent
# Agent pools > ecommerce-vm > New agent > Linux shows the current download link
curl -fsSLO https://download.agent.dev.azure.com/agent/<version>/vsts-agent-linux-x64-<version>.tar.gz
tar xzf vsts-agent-linux-x64-<version>.tar.gz
./config.sh        # your organization URL, a PAT with Agent Pools (Read & manage), pool ecommerce-vm
exit
sudo bash -c 'cd /home/azp/agent && ./svc.sh install azp && ./svc.sh start'
```

The PAT is only used to register the agent; give it a short expiry.

**The host.** Clone this repo anywhere on the server (it is only the source of the setup scripts), then:

```bash
git clone https://github.com/sayyedulawwab/EcommerceApp.git && cd EcommerceApp
sudo AGENT_USER=azp deploy/host/bootstrap-host.sh    # docker group, live-restore, edge network, /opt/ecommerce/<env>, weekly cache cleanup
cd /home/azp/agent && sudo ./svc.sh stop && sudo ./svc.sh start && cd -   # the agent picks up the docker group
sudo deploy/host/install-edge.sh                     # Traefik on 80/443
```

**The infrastructure.** Its secrets file, owned by the agent user and readable by nobody else:

```bash
sudo install -d -m 0750 -o azp -g azp /etc/ecommerce
sudo install -m 0600 -o azp -g azp /dev/null /etc/ecommerce/infra.env
sudo -u azp nano /etc/ecommerce/infra.env
#   POSTGRES_DB=ecommerce
#   POSTGRES_USER=postgres
#   POSTGRES_PASSWORD=<openssl rand -base64 32>
sudo AGENT_USER=azp deploy/host/install-infra.sh
sudo AGENT_USER=azp deploy/host/verify-host.sh       # every line PASS
```

PostgreSQL only reads `POSTGRES_PASSWORD` when it creates its data volume: changing it later means changing it in PostgreSQL too.

### 2. Azure DevOps

**Environments.** Pipelines > Environments > New environment, twice: `ecommerce-staging` and `ecommerce-production` (Resource: None). On each, ⋮ > Approvals and checks:

- **Branch control:** allowed branches `refs/heads/main`.
- **Exclusive lock:** two deploys to the same environment never overlap.

Add an **Approval** on `ecommerce-production` if someone should confirm each production deploy; without it, starting the manual stage is the confirmation.

**Variable groups.** One per environment, `ecommerce-staging` and `ecommerce-production`, with the az CLI (`az extension add --name azure-devops`, `az devops configure --defaults organization=<url> project=<project>`):

```bash
az pipelines variable-group create --name ecommerce-staging --variables \
  POSTGRES_DB=ecommerce_staging POSTGRES_USER=postgres \
  PUBLIC_HOST=api-staging.ecommerce.test REDIS_DATABASE=1 SMOKE_INSECURE=true
# the id it prints, then each secret (az asks for the value, so it never lands in your shell history):
az pipelines variable-group variable create --group-id <id> --name POSTGRES_PASSWORD --secret true
az pipelines variable-group variable create --group-id <id> --name JWT_SECRET_KEY --secret true   # openssl rand -base64 48
```

The same for `ecommerce-production` with `POSTGRES_DB=ecommerce_production`, `PUBLIC_HOST=api.ecommerce.test`, `REDIS_DATABASE=0` and its own `JWT_SECRET_KEY`. `POSTGRES_USER` and `POSTGRES_PASSWORD` are the ones in `/etc/ecommerce/infra.env`.

| Variable | Required | Meaning |
|---|---|---|
| `POSTGRES_DB` | yes | the environment's database; the migration bundle creates it on the first deploy |
| `POSTGRES_USER`, `POSTGRES_PASSWORD` (secret) | yes | the PostgreSQL login from `/etc/ecommerce/infra.env` |
| `JWT_SECRET_KEY` (secret) | yes | at least 32 bytes, different per environment |
| `PUBLIC_HOST` | yes | hostname Traefik routes to this environment |
| `REDIS_DATABASE` | yes | Redis database number, different per environment (0-15) |
| `SMOKE_INSECURE` | no | `true` while Traefik serves its self-signed default certificate (no domain yet) |
| `JWT_ISSUER`, `JWT_AUDIENCE` | no | default `Ecommerce` |
| `API_MEM_LIMIT` | no | container memory limit, default `512m` |

No `'` and no line breaks in any value: they go into the release `.env` single-quoted.

**The pipeline.** Pipelines > New pipeline > GitHub > this repo > Existing Azure Pipelines YAML file > `/azure-pipelines.yml`. Then, in its settings (Edit > ⋮ > Triggers > Pull request validation), make sure **Build pull requests from forks** is off. The first run asks to use the pool, the environments and the variable groups: **View > Permit**, for this pipeline only.

### 3. Calling the API

Without a domain, point the two hostnames at the server in the hosts file of the machine you call from (`/etc/hosts`, or `C:\Windows\System32\drivers\etc\hosts`):

```
<server address>  api-staging.ecommerce.test  api.ecommerce.test
```

```bash
curl -k https://api-staging.ecommerce.test/health    # -k: Traefik's self-signed default certificate
```

With a domain: point its DNS at the server, put the certificate in `/opt/edge/certs`, list it in `deploy/edge/dynamic/tls.yml`, re-run `install-edge.sh`, and remove `SMOKE_INSECURE` from the variable groups.

## Security notes

- The agent user is in the `docker` group, which is root-equivalent. Use it for nothing else, and keep pull requests off the server (GitHub Actions builds them).
- Each release `.env` holds that environment's secrets, mode 0600, owned by the agent user; old releases are deleted by retention.
- `infra.compose.yml` publishes PostgreSQL (5432), Redis (6379) and the observability ports on all interfaces, and Docker ignores `ufw` for published ports. On a server reachable from outside your LAN, remove those `ports:` entries or bind them to `127.0.0.1`. Grafana still has the `admin`/`admin` login.
- Traefik's default certificate is self-signed: fine on a LAN, not for the internet.

## Moving from the timer deploy

An earlier version of this folder deployed with a systemd timer from a clone in `/opt/ecommerce`. Use one deployer per server; before the first Azure deploy:

```bash
sudo systemctl disable --now ecommerce-deploy.timer
sudo rm /etc/systemd/system/ecommerce-deploy.{service,timer} && sudo systemctl daemon-reload
sudo docker compose -p ecommerce down                # the old API and nginx containers
sudo install -d -m 0750 -o azp -g azp /etc/ecommerce
sudo mv /etc/ecommerce/.env.prod /etc/ecommerce/infra.env && sudo chown azp:azp /etc/ecommerce/infra.env   # keeps the PostgreSQL password
sudo rm -rf /opt/ecommerce                           # the old clone; the PostgreSQL data is in a docker volume
```

Then remove every line but `POSTGRES_*` from `infra.env`, and continue with `bootstrap-host.sh` above. `install-infra.sh` keeps the existing PostgreSQL volume (same compose project, `ecommerce-infra`).
