#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# ──────────────────────────────────────────────────────────────────────────────
# Estágio do navegador
#
# Baixa o chrome-headless-shell (build headless-only do Chrome for Testing) em
# vez de instalar o google-chrome-stable de desktop via apt. O binário completo
# do Chrome ocupa 441 MB instalados; o headless-shell, depois de podar os
# locales, fica em ~215 MB — e nada aqui (wget, unzip, o zip) vai para a imagem
# final, porque só a pasta extraída é copiada adiante.
#
# A versão é fixada de propósito: o zip do Chrome for Testing não é assinado,
# então o sha256 é o único controle de integridade que temos. Para atualizar:
#   1. veja a stable em
#      https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json
#   2. baixe o zip e rode `sha256sum` nele
#   3. troque os dois ARGs abaixo no mesmo commit
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS browser

ARG CHROME_HEADLESS_SHELL_VERSION=153.0.8010.36
ARG CHROME_HEADLESS_SHELL_SHA256=a0079df5617da34bcd1debad18196568b072ec3d8ec57944008972a4fc970580

RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates \
        unzip \
        wget \
 && wget -q -O /tmp/chs.zip \
      "https://storage.googleapis.com/chrome-for-testing-public/${CHROME_HEADLESS_SHELL_VERSION}/linux64/chrome-headless-shell-linux64.zip" \
 && echo "${CHROME_HEADLESS_SHELL_SHA256}  /tmp/chs.zip" | sha256sum -c - \
 && unzip -q /tmp/chs.zip -d /opt \
 && mv /opt/chrome-headless-shell-linux64 /opt/chrome-headless-shell \
 # locales/ traz 220 arquivos .pak (48 MB) de strings de UI. Um renderizador de
 # PDF sem interface usa só o idioma dos documentos e o fallback en-US.
 && find /opt/chrome-headless-shell/locales -name '*.pak' \
        ! -name 'en-US*' ! -name 'pt-BR*' -delete \
 && rm -rf /opt/chrome-headless-shell/deb.deps \
           /opt/chrome-headless-shell/rpm.deps \
           /tmp/chs.zip \
           /var/lib/apt/lists/*


# ──────────────────────────────────────────────────────────────────────────────
# Imagem base de runtime
#
# --no-install-recommends é obrigatório aqui. Sem ele o apt puxa os recommends
# do xdg-utils (x11-utils e x11-xserver-utils), que arrastam a pilha Mesa/LLVM
# inteira (libllvm15 sozinha tem 114 MB), o cpp-12 e o Perl completo — ~250 MB
# de drivers de GPU e compilador numa imagem que só imprime PDF headless.
#
# A lista abaixo é o `deb.deps` que o próprio bundle do headless-shell declara,
# menos wget e xdg-utils, que só servem ao Chrome de desktop.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates \
        fonts-liberation \
        libasound2 \
        libatk-bridge2.0-0 \
        libatk1.0-0 \
        libatspi2.0-0 \
        libcairo2 \
        libcups2 \
        libdbus-1-3 \
        libexpat1 \
        libgbm1 \
        libglib2.0-0 \
        libgtk-3-0 \
        libnspr4 \
        libnss3 \
        libpango-1.0-0 \
        libudev1 \
        libx11-6 \
        libxcb1 \
        libxcomposite1 \
        libxdamage1 \
        libxext6 \
        libxfixes3 \
        libxkbcommon0 \
        libxrandr2 \
 && rm -rf /var/lib/apt/lists/*

COPY --from=browser /opt/chrome-headless-shell /opt/chrome-headless-shell

ENV PUPPETEER_EXECUTABLE_PATH="/opt/chrome-headless-shell/chrome-headless-shell"


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/PeopleManagement/PeopleManagement.API/PeopleManagement.API.csproj", "Services/PeopleManagement/PeopleManagement.API/"]
RUN dotnet restore "Services/PeopleManagement/PeopleManagement.API/PeopleManagement.API.csproj"
COPY . .
WORKDIR "/src/Services/PeopleManagement/PeopleManagement.API"
RUN dotnet build "PeopleManagement.API.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "PeopleManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false



FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PeopleManagement.API.dll"]
