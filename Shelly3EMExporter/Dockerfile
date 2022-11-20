FROM mcr.microsoft.com/dotnet/runtime:7.0.0-alpine3.16-amd64

COPY bin/Release/net7.0/publish/ App/
WORKDIR /App

ENV DOTNET_EnableDiagnostics = 0
EXPOSE 9946/tcp
VOLUME /Config

ENTRYPOINT ["dotnet", "Shelly3EMExporter.dll"]
