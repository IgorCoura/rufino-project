FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EconomicCore.API/EconomicCore.API.csproj", "EconomicCore.API/"]
COPY ["EconomicCore.Application/EconomicCore.Application.csproj", "EconomicCore.Application/"]
COPY ["EconomicCore.Domain/EconomicCore.Domain.csproj", "EconomicCore.Domain/"]
COPY ["EconomicCore.Infra/EconomicCore.Infra.csproj", "EconomicCore.Infra/"]
RUN dotnet restore "EconomicCore.API/EconomicCore.API.csproj"
COPY . .
WORKDIR "/src/EconomicCore.API"
RUN dotnet build "EconomicCore.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EconomicCore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EconomicCore.API.dll"]
