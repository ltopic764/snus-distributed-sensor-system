FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SNUSSensorSystem.NotificationService/SNUSSensorSystem.NotificationService.csproj", "SNUSSensorSystem.NotificationService/"]

RUN dotnet restore \
    "SNUSSensorSystem.NotificationService/SNUSSensorSystem.NotificationService.csproj"

COPY . .

WORKDIR "/src/SNUSSensorSystem.NotificationService"

RUN dotnet publish \
    "SNUSSensorSystem.NotificationService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SNUSSensorSystem.NotificationService.dll"]