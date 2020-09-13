FROM mcr.microsoft.com/dotnet/core/sdk:3.1

WORKDIR /src

COPY src .

RUN dotnet build

ENTRYPOINT ["dotnet", "run", "--"]
