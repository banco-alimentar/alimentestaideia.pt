# Containerized Azure DevOps agent

The [Dockerfile](../docker/azure-devops-agent/Dockerfile) runs a non-root Azure
Pipelines agent on Ubuntu 24.04 **amd64**. Its dependencies follow
[`setup-ubuntu-agent.sh` at be09d270](https://github.com/banco-alimentar/alimentestaideia.pt/blob/be09d270ffe8213b15c40c7695f047b103f67a75/scripts/setup-ubuntu-agent.sh):
Git, curl, zip/unzip, Python 3, Ruby with Minitest/Psych, PowerShell 7, Azure CLI,
Chrome and matching ChromeDriver, native .NET libraries, Docker CLI, Buildx, and
Compose. It does not install systemd or a Docker daemon. `UseDotNet@2` continues
to install .NET SDK 10 for each pipeline; no SDK is baked into the image.

## Build

From the repository root, on a dedicated Linux Docker host:

```bash
docker build --pull --platform linux/amd64 \
  --tag alimentestaideia-ado-agent:ubuntu24.04 docker/azure-devops-agent
```

On ARM64 hosts (including Apple Silicon), `--platform linux/amd64` is required;
the builder must support AMD64 emulation. Without it, Docker selects an ARM64
Ubuntu base while the vendor repositories supply AMD64 packages, producing
uninstallable dependencies. The installer checks the base image's actual
architecture before running APT and rejects non-AMD64 images with the rebuild
command. Setting `--build-arg TARGETARCH=amd64` does not select an AMD64 base.
Cross-building on Docker Desktop does not change the Linux host requirements
for running the agent below.

APT vendor packages are resolved at build time. Rebuild with `--no-cache --pull`
for security updates; the build resolves and verifies ChromeDriver against the
installed Chrome build. Keep the resulting image digest for repeatable rollouts.

## Build and publish with GitHub Actions

The [agent container workflow](../.github/workflows/agent-container.yml) builds
`linux/amd64` on a GitHub-hosted Ubuntu runner and pushes to:

```text
ghcr.io/banco-alimentar/alimentestaideia.pt/azure-devops-agent
```

It runs on pushes to `developer` that change the agent Docker context, its script
tests, or the workflow itself. For a manual rebuild, select **Build and publish
Azure DevOps agent** in GitHub Actions, choose **Run workflow**, and select
`developer`. GitHub must have the workflow on the repository's default branch
before showing the manual trigger. Other branches and forks cannot publish, and
pull requests do not trigger this workflow.

Each successful run tests the agent scripts, pulls the current Ubuntu base,
reinstalls dependencies without cached layers, and publishes `latest`,
`ubuntu24.04`, and `sha-<full-commit-SHA>` tags. Tags may move on a rerun; use the
published digest for an exact image version.

Authentication uses the automatic `GITHUB_TOKEN` with job-scoped
`contents: read` and `packages: write`; no additional repository secret or agent
PAT is needed. The image's source label links the package to this repository.
Organization policy must permit package publication. If the package already
exists without repository access, grant this repository write access under the
package's **Manage Actions access** settings.

GHCR packages are private on first publication. To permit anonymous pulls,
change the package visibility to public if organization policy allows it.
Otherwise, authenticate on the agent host with a token allowed to read the
package (`read:packages` for a personal access token (classic)).

```bash
docker pull ghcr.io/banco-alimentar/alimentestaideia.pt/azure-devops-agent:ubuntu24.04
```

Use that fully qualified image name instead of the local
`alimentestaideia-ado-agent:ubuntu24.04` tag in the run command below. Publishing
does not start or register an agent, change pipeline pools, or deploy the
application.

## Register and run

Create a self-hosted pool in Azure DevOps and a short-lived PAT with **Agent Pools
(Read & manage)**, owned by an identity allowed to administer that pool. Place
the PAT alone in a file **outside the repository and build context**. Make it
readable by container UID 1001, without granting access to other host users.
Do not pass it as a build argument or commit it. The example uses an existing
file at `/etc/azure-devops/agent.pat`.

```bash
docker run --detach \
  --name alimentestaideia-agent-01 \
  --network host \
  --shm-size 2g \
  --cap-add SYS_ADMIN \
  --group-add "$(stat -c '%g' /var/run/docker.sock)" \
  --mount type=bind,src=/var/run/docker.sock,dst=/var/run/docker.sock \
  --mount type=bind,src=/etc/azure-devops/agent.pat,dst=/run/secrets/azp-pat,readonly \
  --env AZP_URL=https://dev.azure.com/BancoAlimentar \
  --env AZP_POOL=Alimentestaideia-Linux \
  --env AZP_AGENT_NAME=alimentestaideia-agent-01 \
  --env AZP_TOKEN_FILE=/run/secrets/azp-pat \
  alimentestaideia-ado-agent:ubuntu24.04
```

The image uses Tini to handle container child processes. `SYS_ADMIN` permits
Chrome's sandbox in Docker; do not disable the browser sandbox or use
`--privileged`. The enlarged shared memory avoids Chrome's small default `/dev/shm`.
Verify Chrome sandbox support on the host before enabling browser jobs; host
AppArmor/user-namespace restrictions can require administrator configuration.

The host socket group is added numerically, not assumed to be the image's group.
`--network host` is required because Compose creates Mailpit on the **host**
daemon and binds its published ports to host loopback. Ordinary bridge networking or
`host.docker.internal` will not make those loopback-only ports available.
This configuration is for Linux Docker Engine, not a portable Docker Desktop setup.
The shared Selenium job requests Docker-assigned host ports for both SMTP and HTTP
and passes the discovered endpoints to its tests, so separate agent containers
can run Mailpit concurrently without competing for ports 1025/8025. Keep each
agent's workspace separate and size host CPU, memory, and disk for concurrency.
Local Compose runs outside CI still default to ports 1025/8025.

At startup the agent downloads the latest compatible `linux-x64` package from
your organization, registers without replacing any existing agent, and starts
listening. Set `AZP_AGENT_VERSION` to request a specific available package version
(normal Azure DevOps agent auto-update policies still apply). Outbound HTTPS,
package-registry access, and access to deployment targets are required; no
inbound agent port is needed.

Check `docker logs alimentestaideia-agent-01` and the pool's agent list before
queueing a job. Stop gracefully to unregister:

```bash
docker stop --time 120 alimentestaideia-agent-01
docker rm alimentestaideia-agent-01
```

SIGTERM/SIGINT are forwarded to the listener and removal is retried three times.
A forced kill, host outage, or expired PAT can leave an offline registration:
remove it manually before reusing the name. The PAT file must remain readable
until shutdown. Do not mount `/azp/agent` as a persistent volume: it contains
registration credentials. Workspaces/tool caches are ephemeral by default;
size host storage for restores, artifacts, and the .NET SDK.

**Trust boundary:** access to the Docker socket is root-equivalent on the host;
host networking and `SYS_ADMIN` further reduce isolation. Use a dedicated,
disposable host and only trusted pipelines. Do not run untrusted fork PRs here.
Keep secret-free browser validation separate from credentialed deployment pools.
Never mount Azure credentials or host home directories. Docker cleanup must
target only the current job's Compose project, never a host-wide prune.

## Pipeline selection and limitations

This image supplies the Linux prerequisites; it does not change pool routing.
Current YAML explicitly selects Microsoft-hosted agents. For a Linux-compatible
job, replace its `vmImage` pool with the self-hosted pool, for example:

```yaml
pool:
  name: Alimentestaideia-Linux
  demands:
    - Agent.OS -equals Linux
```

Apply that selection at the actual job/template level, including
[`selenium-job.yml`](../azure-pipelines/templates/selenium-job.yml) when intended;
a root-level pool cannot override its explicit job pool. Classic definitions
need separate pool changes in Azure DevOps. Existing pipeline policy tests
deliberately check hosted pool selections and must be reviewed if YAML is changed.

The Selenium job and Core Release packaging are Linux-compatible. Keep
`UseDotNet@2` and other tool-installer tasks. No SQL Server, Functions host, or
cloud credentials are needed for self-contained browser tests.

**This is not a Windows agent.** Developer/preproduction jobs currently select
`windows-latest`, and developer deployment uses Windows-only `webDeploy`
(MSDeploy). Leave those jobs on Windows. Moving them to Linux requires a separate
reviewed deployment change (including Zip Deploy and task/script compatibility);
installing Azure CLI/PowerShell alone does not make Web Deploy work on Linux.
Pool configuration, service connections, approvals, marketplace extensions,
database migration ordering, and preproduction smoke-test gates remain unchanged.

## Startup troubleshooting

If startup fails with `./env.sh: line 34: !checkVar: unbound variable`, rebuild
and publish the updated image, then pull it and recreate the container using
the same run options. Restarting an existing container does not update its image.
The downloaded agent environment script reads optional unset variables, so the
entrypoint disables `nounset` only while sourcing that script and restores it
before registration. Command failures still stop startup; `errexit` and
`pipefail` remain enabled.

## Local checks

```bash
python3 -m unittest discover -s scripts/ci/tests -p 'agent_container_test.py'
```

The image build checks installed tool versions and the Chrome/ChromeDriver build
match. Registration requires a real organization/PAT. Before adopting the pool,
run only the secret-free standalone Selenium pipeline and confirm browser startup,
Mailpit readiness, all test results, and per-job cleanup on the dedicated host.
