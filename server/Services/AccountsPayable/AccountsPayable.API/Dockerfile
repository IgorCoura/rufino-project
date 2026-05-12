FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AccountsPayable.API/AccountsPayable.API.csproj", "AccountsPayable.API/"]
COPY ["AccountsPayable.Application/AccountsPayable.Application.csproj", "AccountsPayable.Application/"]
COPY ["AccountsPayable.Domain/AccountsPayable.Domain.csproj", "AccountsPayable.Domain/"]
COPY ["AccountsPayable.Infra/AccountsPayable.Infra.csproj", "AccountsPayable.Infra/"]
RUN dotnet restore "AccountsPayable.API/AccountsPayable.API.csproj"
COPY . .
WORKDIR "/src/AccountsPayable.API"
RUN dotnet build "AccountsPayable.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AccountsPayable.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountsPayable.API.dll"]
