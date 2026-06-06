# ECommerce ASP.NET Core Web API

A full-featured e-commerce REST API built with ASP.NET Core 8, Entity Framework Core, SQL Server, and JWT Authentication.

---

## Features

- **JWT Authentication** — Register, login, role-based access (Admin / Customer)
- **Products** — CRUD with category filtering and search
- **Categories** — Manage product categories (Admin only)
- **Cart** — Per-user cart with add, update, remove, and clear
- **Orders** — Place orders from cart, track status, cancel, and admin management

---

## Project Structure

```
ECommerceAPI/
├── Controllers/
│   ├── AuthController.cs       # Register & Login
│   ├── CategoriesController.cs # Category CRUD
│   ├── ProductsController.cs   # Product CRUD
│   ├── CartController.cs       # Cart management
│   └── OrdersController.cs     # Order management
├── Models/
│   ├── User.cs
│   ├── Category.cs
│   ├── Product.cs
│   ├── Cart.cs                 # Cart + CartItem
│   └── Order.cs                # Order + OrderItem
├── DTOs/
│   └── Dtos.cs                 # All request/response DTOs
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext + seed data
├── Services/
│   └── JwtService.cs           # JWT token generation
├── Program.cs                  # App entry point & DI
└── appsettings.json            # Config (DB, JWT)
```

---

## Setup & Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or Docker)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`

### 1. Clone & Configure

Update `appsettings.json` with your SQL Server connection string and a strong JWT secret key:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ECommerceDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_SUPER_SECRET_KEY_CHANGE_THIS_IN_PRODUCTION_MIN_32_CHARS"
  }
}
```

### 2. Create & Apply Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> The app also auto-migrates on startup (see `Program.cs`).

### 3. Run the API

```bash
dotnet run
```

Navigate to: `https://localhost:5001/swagger`

---

## API Endpoints

### Auth
| Method | Endpoint             | Access  | Description     |
|--------|----------------------|---------|-----------------|
| POST   | /api/auth/register   | Public  | Register user   |
| POST   | /api/auth/login      | Public  | Login, get JWT  |

### Categories
| Method | Endpoint              | Access  |
|--------|-----------------------|---------|
| GET    | /api/categories       | Public  |
| GET    | /api/categories/{id}  | Public  |
| POST   | /api/categories       | Admin   |
| PUT    | /api/categories/{id}  | Admin   |
| DELETE | /api/categories/{id}  | Admin   |

### Products
| Method | Endpoint             | Access  |
|--------|----------------------|---------|
| GET    | /api/products        | Public  |
| GET    | /api/products/{id}   | Public  |
| POST   | /api/products        | Admin   |
| PUT    | /api/products/{id}   | Admin   |
| DELETE | /api/products/{id}   | Admin   |

### Cart
| Method | Endpoint                     | Access        |
|--------|------------------------------|---------------|
| GET    | /api/cart                    | Authenticated |
| POST   | /api/cart/items              | Authenticated |
| DELETE | /api/cart/items/{productId}  | Authenticated |
| DELETE | /api/cart                    | Authenticated |

### Orders
| Method | Endpoint                  | Access        |
|--------|---------------------------|---------------|
| POST   | /api/orders               | Authenticated |
| GET    | /api/orders/my            | Authenticated |
| GET    | /api/orders/{id}          | Authenticated |
| GET    | /api/orders               | Admin         |
| PATCH  | /api/orders/{id}/status   | Admin         |
| POST   | /api/orders/{id}/cancel   | Authenticated |

---

## Creating an Admin User

Register normally, then update the role directly in the database:

```sql
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@example.com';
```

---

## Notes

- Passwords are hashed using BCrypt
- Products use soft delete (IsActive = false)
- Orders restore stock automatically on cancellation
- CORS is open for development — restrict in production
