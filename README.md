# 🚀 **"Real-Time Order Processing & Analytics Platform"**  
## *Week-Long Sprint (5 Evenings)*

---

## 📆 **Daily Breakdown**

---

### **Day 1: Foundation & Data Models** *(~2.5 hrs)*

#### **Tasks:**
- [ ] **Docker Compose** - Spin up SQL Server, MongoDB, and Redis containers
- [ ] **SQL Schema** - Create `Orders`, `OrderItems`, `Customers`, `Inventory` tables with proper indexes
- [ ] **NoSQL Schema** - Design MongoDB `OrderAnalytics` document (denormalized for fast reads)
- [ ] **.NET Solution** - Create 3 class libraries (Core, Data, Services) and 2 Web API projects
- [ ] **Entity Framework** - Code-first migrations for SQL Server with seed data (10 products, 5 customers)

**Deliverable:** `docker-compose.yml` + EF Core models + basic API stubs returning mock data.

**Acceptance:**
- `docker-compose up` runs all containers without errors
- `GET /api/orders/ping` returns `{"status":"ok"}`

---

### **Day 2: Order Service Core Logic** *(~2.5 hrs)*

#### **Tasks:**
- [ ] Implement **POST /api/orders** endpoint:
  - Validate customer exists (mock check)
  - **Call Inventory Service** via `HttpClient` to check stock (synchronous for simplicity)
  - Calculate totals with discount logic (10% off > $500, 20% off > $1000)
  - Save to SQL with `Status = "Pending"`
  - Publish **Domain Event** `OrderCreated` to in-memory event bus (MediatR)
- [ ] Implement **GET /api/orders/{id}** with eager loading of items
- [ ] Add **FluentValidation** for request validation
- [ ] Implement **Global Exception Handling Middleware**

**Deliverable:** Working order creation endpoint with integration call simulation.

**Acceptance:**
- POST returns `201 Created` with order ID
- Order total calculated correctly
- Inventory service called with proper payload

---

### **Day 3: Inventory Service & Concurrency** *(~2.5 hrs)*

#### **Tasks:**
- [ ] Implement **Inventory Service** API:
  - `GET /api/inventory/{productId}` - returns `{ productId, quantity, reserved }`
  - `PUT /api/inventory/reserve` - row version or timestamp-based optimistic locking
- [ ] **Concurrency Handling**: Use SQL `ROWVERSION` to detect conflicts
- [ ] Implement **reserve timeout background job**:
  - `IHostedService` that runs every 5 minutes
  - Releases reservations older than 15 minutes
- [ ] **Idempotency**: Reserve endpoint accepts `orderId` to prevent double-reservation

**Deliverable:** Complete inventory service with concurrency conflict handling.

**Acceptance:**
- Two simultaneous reserve requests for same product: one succeeds, one throws `DbUpdateConcurrencyException`
- Background job releases expired reservations

---

### **Day 4: Analytics Service & MongoDB Integration** *(~2.5 hrs)*

#### **Tasks:**
- [ ] **Analytics API**:
  - `GET /api/analytics/daily-sales?days=30` - aggregate from MongoDB
  - `GET /api/analytics/top-products?limit=5` - most ordered products
- [ ] **Event Consumer**: Listen to `OrderCreated` events and upsert MongoDB documents:
  ```json
  {
    "_id": "2024-01-15",
    "totalSales": 1247.50,
    "orderCount": 18,
    "topProducts": [
      { "productId": 101, "name": "Wireless Mouse", "quantity": 12 }
    ]
  }
  ```
- [ ] **Performance**: Create MongoDB indexes on `date` and `topProducts.productId`
- [ ] Implement **caching** with Redis for frequent queries (TTL: 5 minutes)

**Deliverable:** Analytics endpoint returning real aggregated data.

**Acceptance:**
- Creating an order updates the daily aggregate within 2 seconds
- Repeated query hits Redis cache (check via logs)

---

### **Day 5: Polish, Integration Tests & Docker** *(~2.5 hrs)*

#### **Tasks:**
- [ ] **Status Update Endpoint**: `PUT /api/orders/{id}/status` with validation rules:
  - `Pending` → `Confirmed` → `Shipped` → `Delivered`
  - Cannot skip or go backward (use State Machine pattern)
- [ ] **Integration Tests** (xUnit + Testcontainers):
  - End-to-end order flow: Create → Reserve → Confirm → Status update
  - Test concurrency conflict scenario
- [ ] **Dockerize** all services:
  - Multi-stage builds for .NET APIs
  - Use environment variables for connection strings
- [ ] **README.md** with:
  - Setup instructions
  - API documentation (Swagger already enabled)
  - Architecture diagram (simple ASCII or Mermaid)
- [ ] **Bonus**: Add health checks (`/health`) for all dependencies

**Deliverable:** Fully containerized solution with passing integration tests.

**Acceptance:**
- `docker-compose up --build` starts entire stack
- All endpoints functional via Swagger UI
- Integration tests pass (run with `dotnet test`)

---

## 📊 **Evaluation Criteria**

| Category | Weight | What We're Looking For |
|----------|--------|------------------------|
| **Code Quality** | 25% | Clean architecture, SOLID principles, meaningful naming, proper logging |
| **Database Design** | 20% | Normalization (SQL), denormalization strategy (NoSQL), proper indexing, concurrency handling |
| **API Design** | 20% | RESTful conventions, status codes, validation, error messages |
| **Architecture** | 15% | Service boundaries, event-driven communication, caching strategy |
| **DevOps** | 10% | Docker setup, health checks, environment configuration |
| **Testing** | 10% | Integration tests covering critical flows, test readability |

---

## 🛠️ **Tech Stack Constraints**

| Layer | Tech | Notes |
|-------|------|-------|
| Backend | **.NET 8** | Minimal APIs or Controllers (your choice) |
| ORM | **EF Core** | Use code-first migrations |
| SQL | **SQL Server** (Docker) | Linux container supported |
| NoSQL | **MongoDB** (Docker) | Use official driver |
| Cache | **Redis** (Docker) | StackExchange.Redis |
| Messaging | **MediatR** (in-memory) | For domain events (no external broker needed) |
| Validation | **FluentValidation** | Request DTO validation |
| Frontend | **Angular 17+** (optional stretch) | Only if they finish early - standalone components, Signals |
| Testing | **xUnit + Testcontainers** | Spin up dependencies in tests |
| Container | **Docker + Docker Compose** | Multi-container setup |

---

## 📝 **Starter Code Template** (provided to candidate)

```csharp
// Domain/Entities/Order.cs
public class Order
{
    public Guid Id { get; private set; }
    public string CustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    private List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    public void ApplyDiscount(decimal percentage) { /* implement */ }
    public bool CanTransitionTo(OrderStatus newStatus) { /* implement state rules */ }
}
```

---

## ✅ **Minimum Viable Product (MVP) Checklist**

- [ ] Can create an order via API
- [ ] Order creation checks inventory
- [ ] Order status updates are validated
- [ ] Analytics API returns daily aggregates
- [ ] All services run in Docker containers
- [ ] At least one integration test passes
- [ ] README explains how to run the project

---

## 🚧 **Optional Stretch Goals** (if time permits)

- Add **JWT Authentication** across all services
- Implement **gRPC** between Order Service and Inventory Service instead of HTTP
- Add **Serilog** with structured logging to console + file
- Build a simple **Angular dashboard** with:
  - Order list with status filters
  - Analytics chart (using Chart.js)
- Add **Polly retry policies** with exponential backoff for service calls
- Implement **OpenTelemetry** for distributed tracing

This exercise balances depth with practicality - it's challenging enough to showcase real skills but scoped to fit around a full-time job. The daily structure also lets you track progress and identify stuck points early.
