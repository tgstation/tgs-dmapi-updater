FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim

WORKDIR /src

COPY src/Tgstation.Server.DMApiUpdater .

RUN dotnet build -c Release

ENTRYPOINT ["dotnet", "run", "-c", "Release", "--no-build", "--project", "/src/Tgstation.Server.DMApiUpdater.csproj", "--"]
