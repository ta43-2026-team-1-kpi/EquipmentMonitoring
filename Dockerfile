FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EquipmentMonitoring.slnx ./
COPY EquipmentMonitoring.API/EquipmentMonitoring.API.csproj EquipmentMonitoring.API/
COPY EquipmentMonitoring.Application/EquipmentMonitoring.Application.csproj EquipmentMonitoring.Application/
COPY EquipmentMonitoring.Domain/EquipmentMonitoring.Domain.csproj EquipmentMonitoring.Domain/
COPY EquipmentMonitoring.Infrastructure/EquipmentMonitoring.Infrastructure.csproj EquipmentMonitoring.Infrastructure/

RUN dotnet restore EquipmentMonitoring.API/EquipmentMonitoring.API.csproj

COPY . .
RUN dotnet publish EquipmentMonitoring.API/EquipmentMonitoring.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "EquipmentMonitoring.API.dll"]
