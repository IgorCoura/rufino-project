FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BillPayment.API/BillPayment.API.csproj", "BillPayment.API/"]
COPY ["BillPayment.Application/BillPayment.Application.csproj", "BillPayment.Application/"]
COPY ["BillPayment.Domain/BillPayment.Domain.csproj", "BillPayment.Domain/"]
COPY ["BillPayment.Infra/BillPayment.Infra.csproj", "BillPayment.Infra/"]
RUN dotnet restore "BillPayment.API/BillPayment.API.csproj"
COPY . .
WORKDIR "/src/BillPayment.API"
RUN dotnet build "BillPayment.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BillPayment.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BillPayment.API.dll"]
