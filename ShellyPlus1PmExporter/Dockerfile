FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine-arm64v8

COPY bin/Release/net8.0/publish/ App/
WORKDIR /App

ENV DOTNET_EnableDiagnostics=0
EXPOSE 10022/tcp
VOLUME /Config

ENTRYPOINT ["dotnet", "ShellyPlus1PmExporter.dll"]