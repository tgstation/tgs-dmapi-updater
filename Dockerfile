FROM mcr.microsoft.com/dotnet/core/sdk:3.1

WORKDIR /src

COPY src/Tgstation.Server.DMApiUpdater .

RUN dotnet build

ENTRYPOINT ["dotnet", "run", "--no-build", "--project", "/src/Tgstation.Server.DMApiUpdater.csproj", "--"]
