FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
COPY Directory.Packages.props ./
COPY src/TracezillaShopify/TracezillaShopify.csproj src/TracezillaShopify/
COPY tests/TracezillaShopify.Tests/TracezillaShopify.Tests.csproj tests/TracezillaShopify.Tests/
RUN dotnet restore tests/TracezillaShopify.Tests/TracezillaShopify.Tests.csproj --use-lock-file

COPY src ./src
COPY tests ./tests
RUN dotnet build tests/TracezillaShopify.Tests/TracezillaShopify.Tests.csproj --no-restore --configuration Release \
    && dotnet test tests/TracezillaShopify.Tests/TracezillaShopify.Tests.csproj --no-build --configuration Release

ENTRYPOINT ["dotnet", "run", "--project", "src/TracezillaShopify", "--configuration", "Release", "--no-build", "--"]
