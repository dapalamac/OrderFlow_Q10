# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj", "OrderFlow.Orders.Api/"]
COPY ["OrderFlow.Contracts/OrderFlow.Contracts.csproj", "OrderFlow.Contracts/"]

RUN dotnet restore "OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj"

COPY . .

WORKDIR "/src/OrderFlow.Orders.Api"

RUN dotnet publish "OrderFlow.Orders.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderFlow.Orders.Api.dll"]