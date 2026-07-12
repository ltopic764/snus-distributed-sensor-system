FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SNUSSensorSystem.SensorClient/SNUSSensorSystem.SensorClient.csproj", "SNUSSensorSystem.SensorClient/"]
COPY ["SNUSSensorSystem.Shared/SNUSSensorSystem.Shared.csproj", "SNUSSensorSystem.Shared/"]

RUN dotnet restore \
    "SNUSSensorSystem.SensorClient/SNUSSensorSystem.SensorClient.csproj"

COPY . .

WORKDIR "/src/SNUSSensorSystem.SensorClient"

RUN dotnet publish \
    "SNUSSensorSystem.SensorClient.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SNUSSensorSystem.SensorClient.dll"]