FROM mcr.microsoft.com/dotnet/runtime:6.0.2-alpine3.14-amd64

COPY bin/Release/net6.0/publish/ App/
WORKDIR /App

ENV DOTNET_EnableDiagnostics = 0
EXPOSE 9917/tcp

ENTRYPOINT ["dotnet", "ShellyPlugExporter.dll"]