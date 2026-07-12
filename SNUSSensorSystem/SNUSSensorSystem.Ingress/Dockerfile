FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SNUSSensorSystem.Ingress/SNUSSensorSystem.Ingress.csproj", "SNUSSensorSystem.Ingress/"]

RUN dotnet restore \
    "SNUSSensorSystem.Ingress/SNUSSensorSystem.Ingress.csproj"

COPY . .

WORKDIR "/src/SNUSSensorSystem.Ingress"

RUN dotnet publish \
    "SNUSSensorSystem.Ingress.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SNUSSensorSystem.Ingress.dll"]