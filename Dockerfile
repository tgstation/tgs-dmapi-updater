FROM mcr.microsoft.com/dotnet/core/sdk:3.1

WORKDIR /src

COPY src/Tgstation.Server.DMApiUpdater .

RUN dotnet publish -o /app

WORKDIR /app

ENTRYPOINT ["dotnet", "run", "/app/Tgstation.Server.DMApiUpdater.dll", "--"]
