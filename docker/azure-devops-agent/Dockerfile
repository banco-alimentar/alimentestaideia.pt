FROM ubuntu:24.04

ARG TARGETARCH=amd64
RUN test "$TARGETARCH" = amd64

ENV DEBIAN_FRONTEND=noninteractive \
    TZ=Etc/UTC \
    AZP_WORK=/azp/_work \
    AGENT_TOOLSDIRECTORY=/azp/_work/_tool \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=true

COPY install-dependencies.sh /tmp/install-dependencies.sh
RUN bash /tmp/install-dependencies.sh && rm /tmp/install-dependencies.sh

RUN useradd --create-home --uid 1001 --shell /bin/bash agent \
    && mkdir -p /azp/agent /azp/_work/_tool \
    && chown -R agent:agent /azp

COPY start.sh /usr/local/bin/start-agent
RUN chmod 0755 /usr/local/bin/start-agent

WORKDIR /azp/agent
USER agent
ENTRYPOINT ["/usr/bin/tini", "--", "/usr/local/bin/start-agent"]
