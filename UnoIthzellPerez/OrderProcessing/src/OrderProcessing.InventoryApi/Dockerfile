FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/OrderProcessing.InventoryApi/OrderProcessing.InventoryApi.csproj", "src/OrderProcessing.InventoryApi/"]
COPY ["src/OrderProcessing.Services/OrderProcessing.Services.csproj", "src/OrderProcessing.Services/"]
COPY ["src/OrderProcessing.Data/OrderProcessing.Data.csproj", "src/OrderProcessing.Data/"]
COPY ["src/OrderProcessing.Core/OrderProcessing.Core.csproj", "src/OrderProcessing.Core/"]
RUN dotnet restore "src/OrderProcessing.InventoryApi/OrderProcessing.InventoryApi.csproj"
COPY . .
WORKDIR "/src/src/OrderProcessing.InventoryApi"
RUN dotnet build "OrderProcessing.InventoryApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderProcessing.InventoryApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderProcessing.InventoryApi.dll"]
