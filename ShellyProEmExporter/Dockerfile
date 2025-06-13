FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine

COPY bin/Release/net9.0/publish/ App/
WORKDIR /App

ENV DOTNET_EnableDiagnostics=0
EXPOSE 10011/tcp
VOLUME /Config
VOLUME /Logs

ENTRYPOINT ["dotnet", "ShellyProEmExporter.dll"]