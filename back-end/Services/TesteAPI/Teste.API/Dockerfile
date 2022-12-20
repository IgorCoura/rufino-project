FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Teste.API/Teste.API.csproj", "Teste.API/"]
RUN dotnet restore "Teste.API/Teste.API.csproj"
COPY . .
WORKDIR "/src/Teste.API"
RUN dotnet build "Teste.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Teste.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Teste.API.dll"]