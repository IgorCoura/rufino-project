FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Authenticate.Api/Authenticate.Api.csproj", "Authenticate.Api/"]
RUN dotnet restore "Authenticate.Api/Authenticate.Api.csproj"
COPY . .
WORKDIR "/src/Authenticate.Api"
RUN dotnet build "Authenticate.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Authenticate.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Authenticate.Api.dll"]