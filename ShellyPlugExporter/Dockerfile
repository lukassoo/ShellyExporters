FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine-arm32v7

COPY bin/Release/net8.0/publish/ App/
WORKDIR /App

ENV DOTNET_EnableDiagnostics=0
EXPOSE 9918/tcp
VOLUME /Config

ENTRYPOINT ["dotnet", "ShellyPlugExporter.dll"]