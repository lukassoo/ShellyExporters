FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine-arm32v7

COPY bin/Release/net8.0/publish/ App/
WORKDIR /App

ENV DOTNET_EnableDiagnostics=0
EXPOSE 9946/tcp
VOLUME /Config

ENTRYPOINT ["dotnet", "Shelly3EmExporter.dll"]
