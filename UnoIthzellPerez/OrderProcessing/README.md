# UnoIthzellPerez

Real-time order processing platform — three .NET 8 APIs backed by SQL Server, MongoDB, and Redis.

**Solution:** `UnoIthzellPerez.sln`

```
Order API (5001) ──HTTP──> Inventory API (5002)
       │                          │
       │                          └── SQL Server
       ├── SQL Server
       └── HTTP ──> Analytics API (5003) ──> MongoDB + Redis
```

## Build

```bash
dotnet build UnoIthzellPerez.sln
```

## Run with Docker

```bash
docker-compose up --build
```

Swagger: `:5001/swagger`, `:5002/swagger`, `:5003/swagger`

## Local dev

```bash
docker-compose up sqlserver mongodb redis -d

dotnet run --project src/OrderProcessing.InventoryApi
dotnet run --project src/OrderProcessing.AnalyticsApi
dotnet run --project src/OrderProcessing.OrderApi
```

Migrations and seed data run automatically on startup.

## Main endpoints

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/orders/ping` | Health stub |
| GET | `/api/orders?status=Pending` | List/filter orders |
| GET | `/api/orders/{id}` | Order by id |
| POST | `/api/orders` | Creates order, reserves inventory, fires analytics event |
| PUT | `/api/orders/{id}/status` | Pending → Confirmed → Shipped → Delivered |
| GET | `/api/inventory/{productId}` | Stock levels |
| PUT | `/api/inventory/reserve` | Optimistic concurrency via ROWVERSION |
| GET | `/api/analytics/daily-sales?days=30` | Cached 5 min (Redis) |
| GET | `/api/analytics/top-products?limit=5` | Cached 5 min |

Discounts: 10% over $500, 20% over $1000.

Seed customers `CUST-001`–`CUST-005`. Products are seede by name; SQL Server assigns `ProductId` values starting at 1 in seed order.

## Tests

Requires Docker:

```bash
dotnet test UnoIthzellPerez.sln
```

## Layout

```
src/
  OrderProcessing.Core/           entities, events, repo interfaces
  OrderProcessing.Data/           EF Core, migrations, seed
  OrderProcessing.Services/       business logic, MediatR handlers
  OrderProcessing.OrderApi/       orders (port 5001)
  OrderProcessing.InventoryApi/   inventory (port 5002)
  OrderProcessing.AnalyticsApi/   analytics (port 5003)
tests/
  OrderProcessing.IntegrationTests/
  OrderProcessing.InventoryApi.Tests/
```

## Diagrams
### Application startup and database seeding
This diagram covers the initial setup, dependency registration, running migrations, and seeding the database with default customer and inventory data.
```mermaid
sequenceDiagram
  participant InventoryAPI as "Inventory API\n(src/OrderProcessing.InventoryApi)"
  participant DbSeeder as "DbSeeder\n(OrderProcessing.Data.DbSeeder)"
  participant OrderDbContext as "OrderDbContext"
  participant SQL as "🛢️ SQL Server\n(OrderProcessing DB)"

  rect rgb(240,240,255)
    InventoryAPI ->> InventoryAPI: Build host, read ConnectionStrings
    InventoryAPI ->> InventoryAPI: AddOrderData(connectionString) -> register `OrderDbContext`
    InventoryAPI ->> InventoryAPI: AddHealthChecks().AddSqlServer(connectionString)
    InventoryAPI ->> InventoryAPI: Build()
  end

  InventoryAPI ->> DbSeeder: CreateScope + SeedAsync(GetRequiredService<OrderDbContext>())
  DbSeeder ->> OrderDbContext: Database.MigrateAsync()
  OrderDbContext ->> SQL: Connect / run migrations
  SQL -->> OrderDbContext: OK / schema updated
  DbSeeder ->> OrderDbContext: Customers.AnyAsync(), Inventory.AnyAsync()
  OrderDbContext ->> SQL: SELECT / INSERT
  SQL -->> OrderDbContext: Results
  DbSeeder ->> OrderDbContext: SaveChangesAsync()
  DbSeeder -->> InventoryAPI: Seed completed

  Note right of SQL: In Docker use host sqlserver and locally use localhost. Ensure credentials and SA_PASSWORD match container config.
```

### Inventory query flow (`GET /inventory`)
This diagram maps out the standard read operation when a user requests the current stock data.
```mermaid
sequenceDiagram
  actor User
  participant InventoryAPI as "Inventory API\n(src/OrderProcessing.InventoryApi)"
  participant GlobalExceptionMiddleware as "GlobalExceptionMiddleware"
  participant InventoryController as "InventoryController"
  participant InventoryService as "InventoryService"
  participant InventoryRepository as "InventoryRepository\n(OrderProcessing.Data)"
  participant OrderDbContext as "OrderDbContext"
  participant SQL as "🛢️ SQL Server\n(OrderProcessing DB)"

  User ->> InventoryAPI: GET /inventory
  InventoryAPI ->> GlobalExceptionMiddleware: pipeline
  GlobalExceptionMiddleware ->> InventoryController: invoke
  InventoryController ->> InventoryService: GetInventory()
  InventoryService ->> InventoryRepository: GetAll()
  InventoryRepository ->> OrderDbContext: Inventory.ToListAsync()
  OrderDbContext ->> SQL: SELECT ...
  SQL -->> OrderDbContext: rows
  OrderDbContext -->> InventoryRepository: entities
  InventoryRepository -->> InventoryService: data
  InventoryService -->> InventoryController: DTO
  InventoryController -->> User: 200 OK {inventory}
```

### Create order flow (`POST /orders`)
This diagram shows the complex write path, including inter-service HTTP communication to reserve stock, updating the SQL database, and broadcasting async events to MongoDB and Redis cache.
```mermaid
sequenceDiagram
  actor User
  participant OrderAPI as "Order API\n(src/OrderProcessing.OrderApi)"
  participant OrderController as "OrderController"
  participant OrderService as "OrderService"
  participant InventoryAPI as "Inventory API\n(src/OrderProcessing.InventoryApi)"
  participant InventoryService as "InventoryService"
  participant InventoryRepository as "InventoryRepository\n(OrderProcessing.Data)"
  participant OrderRepository as "OrderRepository"
  participant OrderDbContext as "OrderDbContext"
  participant SQL as "🛢️ SQL Server\n(OrderProcessing DB)"
  participant AnalyticsAPI as "Analytics API\n(src/OrderProcessing.AnalyticsApi)"
  participant AnalyticsService as "AnalyticsService"
  participant Mongo as "MongoDB"
  participant Redis as "Redis"

  User ->> OrderAPI: POST /orders
  OrderAPI ->> OrderController: invoke
  OrderController ->> OrderService: CreateOrder()
  
  %% Inter-service Stock Reservation
  OrderService ->> InventoryAPI: HTTP POST /reserve (inter-service)
  InventoryAPI ->> InventoryService: Reserve logic
  InventoryService ->> InventoryRepository: Reserve -> OrderDbContext -> SQL
  SQL -->> OrderDbContext: reservation result
  InventoryAPI -->> OrderAPI: 200 OK (reserve success)
  
  %% Order Persistence
  OrderService ->> OrderRepository: Save order -> OrderDbContext -> SQL
  
  %% Analytics & Caching Event Pipeline
  OrderService ->> AnalyticsAPI: POST /events (order.created)
  AnalyticsAPI ->> AnalyticsService: process event
  AnalyticsService ->> Mongo: write event
  AnalyticsService ->> Redis: update cache
  AnalyticsAPI -->> OrderService: 202 Accepted
```
