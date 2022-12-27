#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Services/MaterialPurchase/MaterialPurchase.API/MaterialPurchase.API.csproj", "Services/MaterialPurchase/MaterialPurchase.API/"]
RUN dotnet restore "Services/MaterialPurchase/MaterialPurchase.API/MaterialPurchase.API.csproj"
COPY . .
WORKDIR "/src/Services/MaterialPurchase/MaterialPurchase.API"
RUN dotnet build "MaterialPurchase.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MaterialPurchase.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MaterialPurchase.API.dll"]