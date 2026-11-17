FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/InventoryApi/InventoryApi.csproj src/InventoryApi/
RUN dotnet restore src/InventoryApi/InventoryApi.csproj

COPY src/InventoryApi/ src/InventoryApi/
RUN dotnet publish src/InventoryApi/InventoryApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# Pass a real signing key at runtime: -e Jwt__Key=...
ENTRYPOINT ["dotnet", "InventoryApi.dll"]
