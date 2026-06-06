namespace ECommerceAPI.DTOs;

// ─── Auth ────────────────────────────────────────────────────────────────────

public record RegisterDto(string FullName, string Email, string Password);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(string Token, string Email, string FullName, string Role);

// ─── Category ────────────────────────────────────────────────────────────────

public record CategoryDto(int Id, string Name, string Description);
public record CreateCategoryDto(string Name, string Description);

// ─── Product ─────────────────────────────────────────────────────────────────

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string ImageUrl,
    bool IsActive,
    int CategoryId,
    string CategoryName
);

public record CreateProductDto(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string ImageUrl,
    int CategoryId
);

public record UpdateProductDto(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string ImageUrl,
    bool IsActive,
    int CategoryId
);

// ─── Cart ────────────────────────────────────────────────────────────────────

public record CartItemDto(int ProductId, int Quantity, string ProductName, decimal UnitPrice, decimal SubTotal);
public record CartDto(int Id, List<CartItemDto> Items, decimal Total);
public record AddToCartDto(int ProductId, int Quantity);
public record UpdateCartItemDto(int Quantity);

// ─── Order ───────────────────────────────────────────────────────────────────

public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal SubTotal);
public record OrderDto(int Id, string Status, decimal TotalAmount, string ShippingAddress, DateTime CreatedAt, List<OrderItemDto> Items);
public record PlaceOrderDto(string ShippingAddress);
public record UpdateOrderStatusDto(string Status);
