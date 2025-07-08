#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

#####################
#PUPPETEER RECIPE
#####################
RUN apt-get update && \
    apt-get install -y \
        wget \
        gnupg2 \
        apt-transport-https \
        ca-certificates \
        fonts-liberation \
        libappindicator3-1 \
        libasound2 \
        libatk-bridge2.0-0 \
        libatk1.0-0 \
        libcups2 \
        libdbus-1-3 \
        libgdk-pixbuf2.0-0 \
        libnspr4 \
        libnss3 \
        libx11-xcb1 \
        libxcomposite1 \
        libxdamage1 \
        libxrandr2 \
        xdg-utils \
        libgbm1 \
        libxcb-dri3-0 \
        libxss1 && \
    rm -rf /var/lib/apt/lists/*

RUN wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | \
    gpg --dearmor | tee /usr/share/keyrings/google-linux-signing-keyring.gpg > /dev/null \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-linux-signing-keyring.gpg] http://dl.google.com/linux/chrome/deb/ stable main" \
    > /etc/apt/sources.list.d/google-chrome.list


RUN apt-get update && apt-get install -y \
  google-chrome-unstable \
  fonts-ipafont-gothic \
  fonts-wqy-zenhei \
  fonts-thai-tlwg \
  fonts-kacst \
  fonts-freefont-ttf \
  --no-install-recommends
  
ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-unstable"

#####################
#END PUPPETEER RECIPE
#####################


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["./PeopleManagement.API/PeopleManagement.API.csproj", "."]
RUN dotnet restore "./PeopleManagement.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "PeopleManagement.API.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "PeopleManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false



FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PeopleManagement.API.dll"]

