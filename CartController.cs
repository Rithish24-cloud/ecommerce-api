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
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartController(AppDbContext db) => _db = db;

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }

    private static CartDto MapCart(Cart cart)
    {
        var items = cart.CartItems.Select(ci => new CartItemDto(
            ci.ProductId,
            ci.Quantity,
            ci.Product?.Name ?? "",
            ci.Product?.Price ?? 0,
            (ci.Product?.Price ?? 0) * ci.Quantity
        )).ToList();

        return new CartDto(cart.Id, items, items.Sum(i => i.SubTotal));
    }

    /// <summary>Get the current user's cart</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await GetOrCreateCartAsync(GetUserId());
        return Ok(MapCart(cart));
    }

    /// <summary>Add or update an item in the cart</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
    {
        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product is null || !product.IsActive)
            return NotFound(new { message = "Product not found." });

        if (dto.Quantity < 1)
            return BadRequest(new { message = "Quantity must be at least 1." });

        if (product.Stock < dto.Quantity)
            return BadRequest(new { message = $"Only {product.Stock} items in stock." });

        var cart = await GetOrCreateCartAsync(GetUserId());
        var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

        if (existing is not null)
            existing.Quantity = dto.Quantity;
        else
            cart.CartItems.Add(new CartItem { ProductId = dto.ProductId, Quantity = dto.Quantity, CartId = cart.Id });

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Reload with products
        var updated = await GetOrCreateCartAsync(GetUserId());
        return Ok(MapCart(updated));
    }

    /// <summary>Remove an item from the cart</summary>
    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var userId = GetUserId();
        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return NotFound();

        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (item is null) return NotFound(new { message = "Item not in cart." });

        cart.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(MapCart(cart));
    }

    /// <summary>Clear all items from the cart</summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return NotFound();

        cart.CartItems.Clear();
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
