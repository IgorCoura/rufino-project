FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TenantManagement.API/TenantManagement.API.csproj", "TenantManagement.API/"]
COPY ["TenantManagement.Application/TenantManagement.Application.csproj", "TenantManagement.Application/"]
COPY ["TenantManagement.Domain/TenantManagement.Domain.csproj", "TenantManagement.Domain/"]
COPY ["TenantManagement.Infra/TenantManagement.Infra.csproj", "TenantManagement.Infra/"]
RUN dotnet restore "TenantManagement.API/TenantManagement.API.csproj"
COPY . .
WORKDIR "/src/TenantManagement.API"
RUN dotnet build "TenantManagement.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TenantManagement.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TenantManagement.API.dll"]
