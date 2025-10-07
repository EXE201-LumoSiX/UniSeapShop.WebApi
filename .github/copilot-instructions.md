# UniSeapShop.API Agent Instructions

## Project Overview
UniSeapShop is a second-hand marketplace web API built with ASP.NET Core. The platform allows users to register/login, browse products, add to cart, checkout, and track orders. Suppliers can view basic revenue. Admins manage users, products, and orders.

## Architecture

### Key Components
- **API Layer** (`UniSeapShop.API`): Controllers, middleware configuration
- **Application Layer** (`UniSeapShop.Application`): Services, interfaces, business logic
- **Domain Layer** (`UniSeapShop.Domain`): Entity models, DTOs, database context
- **Infrastructure Layer** (`UniSeapShop.Infrastructure`): Repositories, UnitOfWork pattern

### Data Flow
1. Controllers receive requests and call appropriate services
2. Services implement business logic using repositories
3. Repositories access data through EF Core and the DB context
4. Results are mapped to DTOs and returned via ApiResult wrapper

## Key Patterns and Conventions

### API Response Format
All API endpoints return responses wrapped in an `ApiResult<T>` structure:
```csharp
{
  "isSuccess": true,
  "isFailure": false,
  "value": {
    "code": "200", 
    "message": "Operation successful",
    "data": { /* actual response data */ }
  },
  "error": null
}
```

### Exception Handling
- Services throw exceptions using `ErrorHelper` methods
- Controllers catch exceptions and convert them to appropriate API responses using `ExceptionUtils`
- Global exception handler in Program.cs formats uncaught exceptions

### Authentication
- JWT-based auth with role-based access control
- User roles: Customer (User), Supplier, Admin
- Email verification with OTP via Redis cache

## Development Workflows

### Running the Application
```powershell
# Run using Docker Compose
docker compose up -d --build

# View API logs
docker compose logs -f uniseapshop.webapi

# Stop the application
docker compose down
```

### Database Access
- SQL Server is accessed at `103.211.201.141,1433`
- Database: `UniSeapShopDB`
- Username: `sa`
- Password: `YourStrong!Passw0rd`

### Critical Setup Steps
1. Before testing CRUD operations, call `/api/system/seed-all-data` to initialize sample data
2. Migrations are automatically applied on startup via `app.ApplyMigrations()` in Program.cs

## Git Workflow
1. Always update from master before creating a new branch:
   ```
   git checkout master && git pull
   ```
2. Create a feature branch:
   ```
   git checkout -b feature/feature-name
   ```
3. Never push directly to master; always create pull requests

## Important Notes
- Redis is used for caching and OTP storage
- Environment variables are configured in docker-compose.yml
- API runs on port 5000 and uses Swagger UI for documentation
- Email verification uses Resend API for sending emails

### Payment Flow
1. User adds products to shopping cart
2. User clicks "Checkout Now" from the cart page
3. System generates PayOS payment link from cart information
4. User is redirected to PayOS payment page
5. After successful payment, the system automatically creates an Order and updates its status