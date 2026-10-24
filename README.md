# Ecommerce API

A REST API for an ecommerce platform: products, categories, orders, product reviews, and user registration/login with JWT authentication. It is a work in progress.

## Features

- **Products:** add, edit, delete, get by id (with its reviews), and search with filtering, sorting, paging and Redis caching
- **Categories:** add, edit, delete, list, and get by id
- **Orders:** place an order for the signed-in user (with an optional coupon), cancel it before payment, and list the user's own orders
- **Reviews:** rate (1–5) and comment on a product
- **Users:** register and log in (JWT bearer tokens, PBKDF2-hashed passwords)

## Technologies

- **.NET 10**, ASP.NET Core controllers, API versioning (`/api/v1/...`)
- **PostgreSQL** with **EF Core** (write side and migrations) and **Dapper** (read-side queries)
- **Redis** for caching
- **FluentValidation**, **Scrutor** (handler registration and decorators)
- **Serilog** and **OpenTelemetry**, exported through an OTLP collector to Loki, Tempo and Prometheus, and viewed in Grafana
- **xUnit**, **Testcontainers**, **Respawn**, **ArchUnitNET**
- **Docker** / Docker Compose, with nginx as a reverse proxy

**Architecture:** Clean Architecture, Domain-Driven Design, and CQRS through lightweight custom command/query handler interfaces (no MediatR).

```
Ecommerce.Domain            Entities, value objects, domain events
Ecommerce.Application       Commands, queries, handlers, validators, decorators, IApplicationDbContext
Ecommerce.Infrastructure    EF Core, Dapper query repositories, Redis cache, JWT, password hashing
Ecommerce.API               Controllers, error mapping, middleware, startup
Ecommerce.Domain.UnitTests  Domain rules (no database, no mocks)
Ecommerce.ArchitectureTests Layer dependency rules (ArchUnitNET)
Ecommerce.IntegrationTests  HTTP tests against PostgreSQL in Docker
```

## Getting Started

Prerequisites: the .NET 10 SDK and Docker Desktop.

1. Start the infrastructure (PostgreSQL, Redis, and the observability stack). This also creates the shared Docker network:

   ```bash
   docker compose -f infra.compose.yml --env-file .env.dev up -d
   ```

2. Build and start the API:

   ```bash
   docker compose -f docker-compose.yml -f docker-compose.override.yml --env-file .env.dev up --build
   ```

   Or open the solution in Visual Studio and run the **Docker Compose** profile, which reads `.env.dev` (set in `docker-compose.dcproj`) and attaches the debugger to `ecommerce-api`.

   Infrastructure is its own compose project (`ecommerce-infra`), so stopping or rebuilding the API leaves PostgreSQL, Redis and the observability stack running. Stop it with `docker compose -f infra.compose.yml down`.

3. The API is available at `http://localhost:5000` (direct) and `http://localhost:8080` (through nginx). In Development the OpenAPI document is served at `/openapi/v1.json`, and Grafana runs at `http://localhost:3000`.

> In Development, the API recreates the database on every start: it drops it, applies migrations, and seeds sample data. Seeded users have the password `test1234`.

## Running the Tests

```bash
# Domain unit tests
dotnet test Ecommerce.Domain.UnitTests/Ecommerce.Domain.UnitTests.csproj

# Architecture tests
dotnet test Ecommerce.ArchitectureTests/Ecommerce.ArchitectureTests.csproj

# Integration tests (Docker must be running)
dotnet test Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj
```

## API Endpoints

All routes are versioned. The version can also be sent in the `X-Api-Version` header. Endpoints marked 🔒 require a `Bearer` token from `/api/v1/auth/login`.

### Auth

| Method | Route                    | Description                                   |
| ------ | ------------------------ | --------------------------------------------- |
| POST   | `/api/v1/auth/register`  | Register a user; returns the new user id      |
| POST   | `/api/v1/auth/login`     | Log in; returns `{ "accessToken": "..." }`     |

```json
POST /api/v1/auth/register
{ "firstName": "John", "lastName": "Doe", "email": "john.doe@example.com", "password": "Password123!" }
```

### Products

| Method | Route                    | Description                                                    |
| ------ | ------------------------ | -------------------------------------------------------------- |
| GET    | `/api/v1/products`       | Search products (paged)                                         |
| GET    | `/api/v1/products/{id}`  | Get a product with its reviews                                  |
| POST   | `/api/v1/products` 🔒    | Add a product                                                   |
| PUT    | `/api/v1/products/{id}` 🔒 | Edit a product                                                |
| DELETE | `/api/v1/products/{id}` 🔒 | Delete a product                                              |

Search query parameters: `page`, `pageSize`, `categoryId`, `minPrice`, `maxPrice`, `keyword` (case-insensitive name match), `sortColumn` (`Name`, `PriceAmount`, `CreatedOnUtc`) and `sortOrder` (`asc`, `desc`).

```json
POST /api/v1/products
{
  "name": "Laptop",
  "description": "High-performance laptop.",
  "priceCurrency": "USD",
  "priceAmount": 1099.99,
  "quantity": 50,
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

Supported currencies: `USD`, `BDT`.

### Categories

| Method | Route                      | Description         |
| ------ | -------------------------- | ------------------- |
| GET    | `/api/v1/categories`       | List categories     |
| GET    | `/api/v1/categories/{id}`  | Get a category      |
| POST   | `/api/v1/categories` 🔒    | Add a category      |
| PUT    | `/api/v1/categories/{id}` 🔒 | Edit a category   |
| DELETE | `/api/v1/categories/{id}` 🔒 | Delete a category |

```json
POST /api/v1/categories
{ "name": "Electronics", "code": "ELEC" }
```

### Orders

| Method | Route               | Description                              |
| ------ | ------------------- | ---------------------------------------- |
| GET    | `/api/v1/orders` 🔒 | List the signed-in user's orders         |
| POST   | `/api/v1/orders` 🔒 | Place an order for the signed-in user    |
| POST   | `/api/v1/orders/{id}/cancel` 🔒 | Cancel a placed order: stock and the coupon use are given back |

```json
POST /api/v1/orders
{
  "orderItems": [
    { "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 2 }
  ],
  "couponCode": "WELCOME10"
}
```

All products in one order must use the same currency. `couponCode` is optional. A coupon takes a percentage off the order total, is valid for a period, and can be used a limited number of times. Development seeds `WELCOME10` and `FLASH25`. Out of stock or a used-up coupon returns 409.

### Reviews

| Method | Route                | Description          |
| ------ | -------------------- | -------------------- |
| POST   | `/api/v1/reviews` 🔒 | Review a product     |

```json
POST /api/v1/reviews
{ "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "rating": 5, "comment": "Highly recommended!" }
```

### Errors

Failures return an error object `{ "name": "...", "description": "...", "code": ... }` with the matching HTTP status: 400 (validation, including a list of per-field `errors`), 401 (invalid credentials), 404 (not found), 409 (conflict, e.g. email already registered), or 500.
