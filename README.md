# Credit Card Management API

A learning-oriented ASP.NET Core Web API that demonstrates a small credit-card management service organized with Clean Architecture. It uses SQL Server for durable data, Entity Framework Core for persistence, Redis for a short-lived card-detail cache, and JWT bearer tokens with scope-based authorization.

This is a proof of concept, not a payment-processing system or production-ready banking application. It stores a payment-provider token and the last four digits for display; it must not receive or store a full card number (PAN) or CVV.

## What the application does

- Authenticates active users with email and password and issues a signed JWT access token.
- Issues cards against a customer ID, storing a provider token, card metadata, credit limit, balance, status, and expiry.
- Retrieves a card or a customer's cards.
- Blocks, unblocks, and cancels cards subject to domain rules.
- Protects card routes with token scopes and customer ownership checks.
- Persists users and cards in SQL Server and caches individual card responses in Redis.
- Exposes a health endpoint and, in Development, Swagger UI.

Customers and transactions are not separate persisted business modules in this POC. There are no endpoints for creating users, recording transactions or payments, changing credit limits, or managing customer records.

## Technology

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 and SQL Server 2022
- Redis 7 using `IDistributedCache`
- JWT bearer authentication with HMAC-SHA256 signing
- ASP.NET Core `PasswordHasher<T>` for user password hashes
- Serilog console logging
- Swashbuckle / Swagger in the Development environment
- Docker Compose for local SQL Server and Redis

## Repository layout

```text
CreditCardManagement.slnx
Directory.Build.props
docker-compose.yml
src/
  CreditCardManagement.API/
    Authorization/      Scope authorization requirement and handler
    Controllers/         HTTP endpoints for login and cards
    Middleware/          Consistent handling of unhandled exceptions
    Program.cs            Dependency injection, configuration, and HTTP pipeline
  CreditCardManagement.Application/
    Abstractions/         Repository, cache, and token-service interfaces
    Contracts/            API request and response records
    Services/             Authentication and card use cases
  CreditCardManagement.Domain/
    Entities/             User and credit-card models and card lifecycle rules
  CreditCardManagement.Infrastructure/
    Caching/              Redis-backed cache implementation
    Persistence/          EF Core DbContext and SQL Server repositories
    Security/             JWT options and token creation
```

`Directory.Build.props` sets the target framework to `net10.0`, enables nullable reference types and implicit usings, and does not treat warnings as errors.

## Architecture and request flow

The dependency direction is:

```text
API -> Application -> Domain
API -> Infrastructure -> Application and Domain
```

- **Domain** contains `CreditCard` lifecycle logic and the `AppUser` model. It has no application or infrastructure dependency.
- **Application** contains use cases (`AuthService` and `CreditCardService`), request/response contracts, and interfaces for persistence, cache, and token creation. It depends on Domain, not on SQL Server or Redis implementations.
- **Infrastructure** implements those interfaces using EF Core, SQL Server, Redis, and JWT. It references Application and Domain.
- **API** configures dependency injection and middleware, maps HTTP requests to application services, and applies authentication, authorization, and ownership checks.

For example, a card-detail request is authenticated by the JWT bearer middleware, authorized by the `cards.read` scope policy, checked for customer ownership in `CardsController`, then handled by `CreditCardService`. The service checks Redis first and reads SQL Server on a cache miss.

## Data model and card rules

The EF Core context maps two tables:

- **CreditCards**: card and customer IDs, cardholder name, provider token, last four digits, network, credit limit, outstanding balance, status, expiry, and creation/update timestamps. A database index is defined for `CustomerId`.
- **Users**: email, password hash, role, space-delimited scopes, optional customer ID, and active state. Email has a unique index.

The domain starts a newly issued card with `Active` status and a zero outstanding balance. It validates that the customer ID is non-empty, the cardholder name and token are present, the last four characters are digits, the credit limit is positive, and the expiry date is in the future.

Card lifecycle behavior:

- An active card can be blocked. Blocking an already-blocked card leaves it blocked.
- A non-cancelled card can be unblocked, unless it has expired.
- A cancelled card is terminal: it cannot be blocked, unblocked, or cancelled again.
- The domain also has methods for changing a limit and recording a payment, but the API does not currently expose those operations.

The response DTO deliberately does not include `CardToken`; only the last four digits are returned. The token must be a provider token, not a PAN.

## Authentication and authorization

### Login

`POST /api/v1/auth/login` accepts an email and password. If the matching user exists, is active, and the password verifies, the API returns a bearer access token. Invalid credentials produce an unauthorized response.

The token includes the user's ID, email, role, scopes, and optional `customer_id`. Its issuer and audience are validated on subsequent requests. By default, the configured token lifetime is 15 minutes.

### Scopes and ownership

Card routes require both an authenticated token and the indicated scope:

| Scope | Purpose |
| --- | --- |
| `cards.read` | Read a card or list cards for a customer |
| `cards.write` | Issue a card |
| `cards.block` | Block or unblock a card |
| `cards.cancel` | Cancel a card |

The scope handler looks for a space-delimited `scope` claim and requires an exact scope match. For customer-level access, a user can access their own `customer_id`; users with the `Admin` or `Operations` role can access any customer ID. These checks are in addition to the scope requirement.

The API also registers `transactions.read` and `customers.read` scope policies, but there are no transaction or customer endpoints in this project yet.

## HTTP API

Unless noted otherwise, card routes require a valid bearer token.

| Method | Route | Required scope | Result |
| --- | --- | --- | --- |
| `POST` | `/api/v1/auth/login` | None | Issue an access token |
| `POST` | `/api/v1/cards` | `cards.write` | Issue a card; returns `201 Created` |
| `GET` | `/api/v1/cards/{id}` | `cards.read` | Get a card by GUID |
| `GET` | `/api/v1/cards/customer/{customerId}` | `cards.read` | List a customer's cards |
| `POST` | `/api/v1/cards/{id}/block` | `cards.block` | Block a card |
| `POST` | `/api/v1/cards/{id}/unblock` | `cards.block` | Unblock a card |
| `POST` | `/api/v1/cards/{id}/cancel` | `cards.cancel` | Cancel a card |
| `GET` | `/health` | None | Return a basic health response |

Successful block, unblock, and cancel requests return `204 No Content`. Card lookups return `404 Not Found` when the card does not exist and `403 Forbidden` when the caller lacks customer access.

### Example requests

Login:

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@bank.local",
  "password": "<the configured bootstrap password>"
}
```

Issue a card with the returned access token:

```http
POST /api/v1/cards
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "cardholderName": "Example Customer",
  "cardToken": "provider-token-not-a-card-number",
  "lastFourDigits": "1234",
  "network": "ExampleNetwork",
  "creditLimit": 5000.00,
  "expiryDate": "2028-12-31"
}
```

The issuer must have the `cards.write` scope. A non-admin/non-operations user must also have a matching `customer_id` claim.

## Configuration

ASP.NET Core configuration can be supplied through `appsettings.json`, environment-specific settings, environment variables, or other standard configuration providers. Environment variables use double underscores in place of `:`:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__SqlServer` | SQL Server connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__Issuer` | Expected and emitted JWT issuer |
| `Jwt__Audience` | Expected and emitted JWT audience |
| `Jwt__Secret` | HMAC signing secret; required and at least 32 UTF-8 bytes |
| `Jwt__AccessTokenMinutes` | JWT lifetime in minutes |
| `BootstrapAdmin__Email` | Optional first-run administrator email |
| `BootstrapAdmin__Password` | Optional first-run administrator password |

The checked-in settings intentionally leave the JWT secret empty, and the application refuses to start until a sufficiently long secret is provided. Use a unique, cryptographically random secret locally; keep real secrets out of source control. The JWT uses a symmetric key in this demo, which is not a full production key-management strategy.

The connection string in `appsettings.json` does not use the same SQL Server password as the one in `docker-compose.yml`. When using Compose, override the connection string to match the Compose `MSSQL_SA_PASSWORD`, or change both values to a local password of your choice. The Compose credentials are for local development only and must not be used in a deployed environment.

## Run locally (Windows PowerShell)

Prerequisites:

- .NET 10 SDK
- Docker Desktop with Docker Compose

From the repository root:

1. Configure a random JWT secret and a SQL connection string matching the local Compose SQL password. The following connection string uses the current local Compose default:

   ```powershell
   $env:Jwt__Secret = '<at-least-32-bytes-of-random-secret-material>'
   $env:ConnectionStrings__SqlServer = 'Server=localhost,1433;Database=CreditCardPoc;User Id=sa;Password=ChangeThis_Strong!123;TrustServerCertificate=True;Encrypt=False'
   ```

   These environment variables apply only to the current PowerShell session. For non-local usage, store secrets in a managed secret store rather than in source control.

2. Start SQL Server and Redis:

   ```powershell
   docker compose up -d
   ```

3. Restore and build the solution:

   ```powershell
   dotnet restore
   dotnet build
   ```

   `CreditCardManagement.slnx` is the repository's sole solution file, so these commands can run from the repository root without specifying a solution path.

4. Install the EF Core command-line tool if needed:

   ```powershell
   dotnet tool install --global dotnet-ef
   ```

5. Create the initial migration and apply it to SQL Server. The project currently has no checked-in migrations, so create the first migration before updating the database:

   ```powershell
   dotnet ef migrations add InitialCreate --project .\src\CreditCardManagement.Infrastructure --startup-project .\src\CreditCardManagement.API --output-dir Persistence\Migrations
   dotnet ef database update --project .\src\CreditCardManagement.Infrastructure --startup-project .\src\CreditCardManagement.API
   ```

   Commit the generated migration files so other developers and environments can apply the same schema.

6. (Optional) Create a local administrator on first startup by setting bootstrap credentials after applying the migration:

   ```powershell
   $env:BootstrapAdmin__Email = 'admin@bank.local'
   $env:BootstrapAdmin__Password = '<a-strong-unique-password>'
   ```

   If the database is reachable and the email is not already registered, startup creates an active `Admin` user with all currently registered scopes and hashes the password. It does not replace an existing user. Remove these environment variables after provisioning.

7. Start the API:

   ```powershell
   dotnet run --project .\src\CreditCardManagement.API
   ```

In the Development environment, Swagger UI is available at `/swagger`. The `/health` endpoint returns a basic liveness response; it does not verify connectivity to SQL Server or Redis.

To stop the local dependencies:

```powershell
docker compose down
```

To also delete the local SQL Server and Redis data volumes, use `docker compose down -v`. This permanently removes the local database and cache data.

## Data access and caching

SQL Server is the source of truth. EF Core repositories persist card and user data. The application caches an individual card response under `cards:{id}` for five minutes; card details are read from the cache before SQL Server. Block, unblock, and cancel operations remove that card's cached response after saving changes. Customer card lists are queried from SQL Server and are not cached.

Redis is therefore an optimization, not durable storage. The cache currently has no distributed invalidation mechanism beyond these local card operations.

## Errors and operational behavior

- ASP.NET Core model binding and `[ApiController]` handle malformed or invalid request bodies as client errors.
- Authorization failures are handled by ASP.NET Core authentication/authorization middleware.
- Unhandled exceptions are logged and converted by `ExceptionMiddleware` into `application/problem+json` responses with a trace ID. Expected missing-resource and argument/state errors map to client status codes; unexpected errors return a generic `500` detail.
- Serilog writes application and HTTP request logs to the configured sinks; console logging is enabled in the host setup.
- Database schema changes are explicit. The application does not apply migrations automatically at startup.

## Security and production limitations

- `CardToken` must be a token from a PCI-compliant payment provider. Never send or store a full PAN or CVV here.
- This POC uses a symmetric JWT signing key. Production systems should use managed key storage, rotation, and an appropriate signing/verification design.
- The bootstrap administrator mechanism is for local setup, not a production identity-provisioning workflow.
- Refresh-token rotation and revocation, key rotation, rate limiting, account lockout, MFA, audit persistence, distributed tracing, an outbox, and integration tests are not implemented.
- Customer and transaction management are not implemented. Customer IDs are accepted as identifiers; there is no customer table or external identity/customer-service integration in this repository.
- Docker Compose credentials, connection settings, and service exposure are for local development only.

## Suggested extensions

1. Add and commit the initial EF migration and database integration tests.
2. Add customer and transaction use cases, with explicit domain ownership and authorization rules.
3. Add validation and consistent API error contracts.
4. Add refresh-token lifecycle, rate limiting, lockout, MFA, and key management.
5. Add audit records, an outbox, observability, and cache invalidation tests.
6. Add container hardening, automated CI, and production deployment configuration.
