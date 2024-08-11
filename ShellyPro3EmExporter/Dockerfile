FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine

COPY bin/Release/net8.0/publish/ App/
WORKDIR /App

ENV DOTNET_EnableDiagnostics = 0
EXPOSE 10011/tcp
VOLUME /Config
VOLUME /Logs

ENTRYPOINT ["dotnet", "ShellyPro3EmExporter.dll"]