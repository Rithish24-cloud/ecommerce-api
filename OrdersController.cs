using System.Security.Claims;
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db) => _db = db;

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static OrderDto MapOrder(Order order) => new(
        order.Id,
        order.Status,
        order.TotalAmount,
        order.ShippingAddress,
        order.CreatedAt,
        order.OrderItems.Select(oi => new OrderItemDto(
            oi.ProductId,
            oi.Product?.Name ?? "",
            oi.Quantity,
            oi.UnitPrice,
            oi.UnitPrice * oi.Quantity
        )).ToList()
    );

    /// <summary>Place an order from the current cart</summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        var userId = GetUserId();
        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.CartItems.Any())
            return BadRequest(new { message = "Your cart is empty." });

        // Validate stock
        foreach (var item in cart.CartItems)
        {
            if (item.Product is null || !item.Product.IsActive)
                return BadRequest(new { message = $"Product {item.ProductId} is unavailable." });

            if (item.Product.Stock < item.Quantity)
                return BadRequest(new { message = $"Insufficient stock for '{item.Product.Name}'." });
        }

        // Create order
        var order = new Order
        {
            UserId = userId,
            ShippingAddress = dto.ShippingAddress,
            TotalAmount = cart.CartItems.Sum(ci => ci.Product!.Price * ci.Quantity),
            OrderItems = cart.CartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.Product!.Price
            }).ToList()
        };

        // Deduct stock
        foreach (var item in cart.CartItems)
            item.Product!.Stock -= item.Quantity;

        _db.Orders.Add(order);
        cart.CartItems.Clear();
        await _db.SaveChangesAsync();

        await _db.Entry(order).Collection(o => o.OrderItems).Query()
            .Include(oi => oi.Product).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapOrder(order));
    }

    /// <summary>Get all orders for the current user</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(MapOrder));
    }

    /// <summary>Get an order by ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var order = await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && (isAdmin || o.UserId == userId));

        if (order is null) return NotFound();
        return Ok(MapOrder(order));
    }

    /// <summary>Get all orders (Admin only)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var query = _db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(MapOrder));
    }

    /// <summary>Update order status (Admin only)</summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Invalid status value." });

        var order = await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(MapOrder(order));
    }

    /// <summary>Cancel an order (customer, only if Pending)</summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetUserId();
        var order = await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order is null) return NotFound();
        if (order.Status != "Pending")
            return BadRequest(new { message = "Only pending orders can be cancelled." });

        // Restore stock
        foreach (var item in order.OrderItems)
            if (item.Product is not null)
                item.Product.Stock += item.Quantity;

        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(MapOrder(order));
    }
}
