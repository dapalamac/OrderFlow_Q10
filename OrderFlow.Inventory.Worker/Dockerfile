# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["OrderFlow.Inventory.Worker/OrderFlow.Inventory.Worker.csproj", "OrderFlow.Inventory.Worker/"]
COPY ["OrderFlow.Contracts/OrderFlow.Contracts.csproj", "OrderFlow.Contracts/"]

RUN dotnet restore "OrderFlow.Inventory.Worker/OrderFlow.Inventory.Worker.csproj"

COPY . .

WORKDIR "/src/OrderFlow.Inventory.Worker"

RUN dotnet publish "OrderFlow.Inventory.Worker.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OrderFlow.Inventory.Worker.dll"]