FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SNUSSensorSystem.ConsensusService/SNUSSensorSystem.ConsensusService.csproj", "SNUSSensorSystem.ConsensusService/"]

RUN dotnet restore \
    "SNUSSensorSystem.ConsensusService/SNUSSensorSystem.ConsensusService.csproj"

COPY . .

WORKDIR "/src/SNUSSensorSystem.ConsensusService"

RUN dotnet publish \
    "SNUSSensorSystem.ConsensusService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SNUSSensorSystem.ConsensusService.dll"]