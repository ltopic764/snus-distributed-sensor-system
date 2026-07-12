FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SNUSSensorSystem.IngestionService/SNUSSensorSystem.IngestionService.csproj", "SNUSSensorSystem.IngestionService/"]
COPY ["SNUSSensorSystem.Shared/SNUSSensorSystem.Shared.csproj", "SNUSSensorSystem.Shared/"]

RUN dotnet restore \
    "SNUSSensorSystem.IngestionService/SNUSSensorSystem.IngestionService.csproj"

COPY . .

WORKDIR "/src/SNUSSensorSystem.IngestionService"

RUN dotnet publish \
    "SNUSSensorSystem.IngestionService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SNUSSensorSystem.IngestionService.dll"]