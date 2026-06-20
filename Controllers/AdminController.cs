using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EtrailGlobal.Database;
using EtrailGlobal.Database.Models;

namespace EtrailGlobal.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var totalUsers = await _context.Users.CountAsync();
        var buyers = await _context.Users.CountAsync(u => u.Role == UserRole.Buyer);
        var sellers = await _context.Users.CountAsync(u => u.Role == UserRole.Seller);
        var pendingSellers = await _context.Users.CountAsync(u => u.Role == UserRole.Seller && !u.IsVerified);
        var products = await _context.Products.CountAsync();
        var orders = await _context.Orders.CountAsync();
        var tickets = await _context.SupportTickets.CountAsync();
        var openTickets = await _context.SupportTickets.CountAsync(t => t.Status != "Closed");
        var messages = await _context.Messages.CountAsync();

        return Ok(new
        {
            totalUsers,
            buyers,
            sellers,
            pendingSellers,
            products,
            orders,
            tickets,
            openTickets,
            messages
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] UserRole? role)
    {
        var query = _context.Users.AsQueryable();
        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                Role = u.Role.ToString(),
                u.CompanyName,
                u.BusinessType,
                u.ContactInfo,
                u.WhatsAppNumber,
                u.AlternateEmail,
                u.TaxNumber,
                u.CompanyAddress,
                u.YearEstablished,
                u.WebsiteUrl,
                u.IsVerified,
                u.BillingAddress,
                u.ShippingAddress,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (User.Identity?.Name == user.Username)
            return BadRequest(new { message = "You cannot delete the admin account currently in use." });

        await DeleteUserGraph(id, user.Role);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User and related records deleted successfully" });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.PaymentMethod,
                o.BillingAddress,
                o.ShippingAddress,
                o.ItemDetailsJson,
                CustomerName = o.User != null ? o.User.Username : "Deleted user",
                CustomerEmail = o.User != null ? o.User.Email : string.Empty
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets()
    {
        var tickets = await _context.SupportTickets
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Subject,
                t.Description,
                t.Priority,
                t.Status,
                t.CreatedAt,
                CustomerName = t.User != null ? t.User.Username : "Deleted user",
                CustomerEmail = t.User != null ? t.User.Email : string.Empty
            })
            .ToListAsync();

        return Ok(tickets);
    }

    [HttpPut("tickets/{id}/status")]
    public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] UpdateTicketStatusRequest request)
    {
        var ticket = await _context.SupportTickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { message = "Ticket not found" });

        ticket.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ticket status updated successfully", ticket });
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages()
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderByDescending(m => m.Timestamp)
            .Take(200)
            .Select(m => new
            {
                m.Id,
                m.Content,
                m.Timestamp,
                m.IsRead,
                SenderName = m.Sender != null ? m.Sender.Username : "Deleted user",
                ReceiverName = m.Receiver != null ? m.Receiver.Username : "Deleted user"
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpDelete("messages/{id}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message == null)
            return NotFound(new { message = "Message not found" });

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Message deleted successfully" });
    }

    [HttpGet("sellers")]
    public async Task<IActionResult> GetSellers([FromQuery] bool? verifiedOnly)
    {
        var query = _context.Users.Where(u => u.Role == UserRole.Seller).AsQueryable();

        if (verifiedOnly.HasValue)
        {
            query = query.Where(u => u.IsVerified == verifiedOnly.Value);
        }

        var sellers = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        return Ok(sellers);
    }

    [HttpPost("sellers/{id}/verify")]
    public async Task<IActionResult> ToggleVerification(int id, [FromBody] VerifyToggleRequest request)
    {
        var seller = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Seller);
        if (seller == null)
            return NotFound(new { message = "Seller not found" });

        seller.IsVerified = request.IsVerified;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Seller verification status updated to {request.IsVerified}", seller });
    }

    [HttpPut("products/{id}/flags")]
    public async Task<IActionResult> UpdateProductFlags(int id, [FromBody] UpdateProductFlagsRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        product.IsFeatured = request.IsFeatured;
        product.IsBestSeller = request.IsBestSeller;
        product.IsTodayDeal = request.IsTodayDeal;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Product flags updated successfully", product });
    }

    [HttpPost("products/{id}/approve")]
    public async Task<IActionResult> ApproveProduct(int id, [FromBody] ApproveProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        product.IsApproved = request.IsApproved;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Product approval status updated to {request.IsApproved}", product });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, string> settings)
    {
        foreach (var kv in settings)
        {
            var dbSetting = await _context.SiteSettings.FindAsync(kv.Key);
            if (dbSetting != null)
            {
                dbSetting.Value = kv.Value;
            }
            else
            {
                _context.SiteSettings.Add(new SiteSetting
                {
                    Key = kv.Key,
                    Value = kv.Value,
                    Description = "Custom admin configuration setting."
                });
            }
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Site settings updated successfully" });
    }

    private async Task DeleteUserGraph(int userId, UserRole role)
    {
        _context.CartItems.RemoveRange(_context.CartItems.Where(c => c.UserId == userId));
        _context.WishlistItems.RemoveRange(_context.WishlistItems.Where(w => w.UserId == userId));
        _context.CompareItems.RemoveRange(_context.CompareItems.Where(c => c.UserId == userId));
        _context.Orders.RemoveRange(_context.Orders.Where(o => o.UserId == userId));
        _context.Downloads.RemoveRange(_context.Downloads.Where(d => d.UserId == userId));
        _context.SupportTickets.RemoveRange(_context.SupportTickets.Where(t => t.UserId == userId));
        _context.FollowedSellers.RemoveRange(_context.FollowedSellers.Where(f => f.UserId == userId || f.SellerId == userId));
        _context.Messages.RemoveRange(_context.Messages.Where(m => m.SenderId == userId || m.ReceiverId == userId));
        _context.SellerReviews.RemoveRange(_context.SellerReviews.Where(sr => sr.BuyerId == userId || sr.SellerId == userId));

        if (role == UserRole.Seller)
        {
            _context.Quotes.RemoveRange(_context.Quotes.Where(q => q.SellerId == userId));
            _context.Products.RemoveRange(_context.Products.Where(p => p.SellerId == userId));
        }

        if (role == UserRole.Buyer)
        {
            var rfqIds = await _context.Rfqs.Where(r => r.BuyerId == userId).Select(r => r.Id).ToListAsync();
            _context.Quotes.RemoveRange(_context.Quotes.Where(q => rfqIds.Contains(q.RfqId)));
            _context.Rfqs.RemoveRange(_context.Rfqs.Where(r => r.BuyerId == userId));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
        }
    }
}

public class UpdateProductFlagsRequest
{
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsTodayDeal { get; set; }
}

public class VerifyToggleRequest
{
    public bool IsVerified { get; set; }
}

public class UpdateTicketStatusRequest
{
    public string Status { get; set; } = "Open";
}

public class ApproveProductRequest
{
    public bool IsApproved { get; set; }
}
