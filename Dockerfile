FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BlockchainTracker.Api/BlockchainTracker.Api.csproj", "src/BlockchainTracker.Api/"]
COPY ["src/BlockchainTracker.Application/BlockchainTracker.Application.csproj", "src/BlockchainTracker.Application/"]
COPY ["src/BlockchainTracker.Infrastructure/BlockchainTracker.Infrastructure.csproj", "src/BlockchainTracker.Infrastructure/"]
COPY ["src/BlockchainTracker.Domain/BlockchainTracker.Domain.csproj", "src/BlockchainTracker.Domain/"]
RUN dotnet restore "src/BlockchainTracker.Api/BlockchainTracker.Api.csproj"
COPY . .
RUN dotnet publish "src/BlockchainTracker.Api/BlockchainTracker.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlockchainTracker.Api.dll"]
