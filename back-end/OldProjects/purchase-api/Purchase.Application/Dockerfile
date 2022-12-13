#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Purchase.Application/Purchase.Application.csproj", "Purchase.Application/"]
RUN dotnet restore "Purchase.Application/Purchase.Application.csproj"
COPY . .
WORKDIR "/src/Purchase.Application"
RUN dotnet build "Purchase.Application.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Purchase.Application.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Purchase.Application.dll"]