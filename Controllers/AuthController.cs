using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using EtrailGlobal.Database;
using EtrailGlobal.Database.Models;
using System.Security.Cryptography;
using System.Text;

namespace EtrailGlobal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Email is already registered" });

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "Username is already taken" });

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = request.Role,
            CompanyName = request.CompanyName,
            BusinessType = request.BusinessType ?? string.Empty,
            ContactInfo = request.ContactInfo,
            WhatsAppNumber = request.WhatsAppNumber ?? string.Empty,
            AlternateEmail = request.AlternateEmail ?? string.Empty,
            TaxNumber = request.TaxNumber ?? string.Empty,
            CompanyAddress = request.CompanyAddress ?? string.Empty,
            YearEstablished = request.YearEstablished ?? string.Empty,
            WebsiteUrl = request.WebsiteUrl ?? string.Empty,
            IsVerified = request.Role == UserRole.Buyer // Buyers auto-verified, Sellers require admin verification
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var passwordHash = HashPassword(request.Password);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.PasswordHash == passwordHash);

        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        // Separate authentication checks
        if (!string.IsNullOrEmpty(request.LoginType))
        {
            if (request.LoginType.Equals("Seller", StringComparison.OrdinalIgnoreCase))
            {
                if (user.Role != UserRole.Seller && user.Role != UserRole.Admin)
                {
                    return BadRequest(new { message = "Access Denied: This login is only for Sellers. Customers, please use the main login." });
                }
            }
            else if (request.LoginType.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                if (user.Role != UserRole.Buyer && user.Role != UserRole.Admin)
                {
                    return BadRequest(new { message = "Access Denied: This login is only for Customers/Admins. Sellers, please use the Seller Portal." });
                }
            }
        }

        // Block unverified sellers
        if (user.Role == UserRole.Seller && !user.IsVerified)
        {
            return BadRequest(new { message = "Your seller application is pending verification by the administrator. You will be able to log in once approved." });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            role = user.Role.ToString(),
            companyName = user.CompanyName,
            isVerified = user.IsVerified,
            contactInfo = user.ContactInfo,
            whatsAppNumber = user.WhatsAppNumber,
            alternateEmail = user.AlternateEmail,
            taxNumber = user.TaxNumber,
            companyAddress = user.CompanyAddress,
            yearEstablished = user.YearEstablished,
            websiteUrl = user.WebsiteUrl,
            billingAddress = user.BillingAddress,
            shippingAddress = user.ShippingAddress
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { message = "Not authenticated" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Invalid token claims" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "User not found" });

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            role = user.Role.ToString(),
            companyName = user.CompanyName,
            isVerified = user.IsVerified,
            contactInfo = user.ContactInfo,
            whatsAppNumber = user.WhatsAppNumber,
            alternateEmail = user.AlternateEmail,
            taxNumber = user.TaxNumber,
            companyAddress = user.CompanyAddress,
            yearEstablished = user.YearEstablished,
            websiteUrl = user.WebsiteUrl,
            billingAddress = user.BillingAddress,
            shippingAddress = user.ShippingAddress
        });
    }

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUserPublicInfo(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            companyName = user.CompanyName,
            role = user.Role.ToString(),
            isVerified = user.IsVerified
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { message = "Not authenticated" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Invalid token claims" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (user.Email != request.Email && await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Email is already taken" });

        if (user.Username != request.Username && await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "Username is already taken" });

        user.Username = request.Username;
        user.Email = request.Email;
        user.ContactInfo = request.ContactInfo ?? string.Empty;
        user.CompanyName = request.CompanyName ?? string.Empty;
        user.BillingAddress = request.BillingAddress ?? string.Empty;
        user.ShippingAddress = request.ShippingAddress ?? string.Empty;
        user.WhatsAppNumber = request.WhatsAppNumber ?? string.Empty;
        user.AlternateEmail = request.AlternateEmail ?? string.Empty;
        user.TaxNumber = request.TaxNumber ?? string.Empty;
        user.CompanyAddress = request.CompanyAddress ?? string.Empty;
        user.YearEstablished = request.YearEstablished ?? string.Empty;
        user.WebsiteUrl = request.WebsiteUrl ?? string.Empty;
        user.BusinessType = request.BusinessType ?? string.Empty;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Profile updated successfully",
            id = user.Id,
            username = user.Username,
            email = user.Email,
            role = user.Role.ToString(),
            companyName = user.CompanyName,
            isVerified = user.IsVerified,
            contactInfo = user.ContactInfo,
            whatsAppNumber = user.WhatsAppNumber,
            alternateEmail = user.AlternateEmail,
            taxNumber = user.TaxNumber,
            companyAddress = user.CompanyAddress,
            yearEstablished = user.YearEstablished,
            websiteUrl = user.WebsiteUrl,
            billingAddress = user.BillingAddress,
            shippingAddress = user.ShippingAddress,
            businessType = user.BusinessType
        });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAccount()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { message = "Not authenticated" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Invalid token claims" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        try
        {
            var cartItems = _context.CartItems.Where(c => c.UserId == userId);
            _context.CartItems.RemoveRange(cartItems);

            var wishlistItems = _context.WishlistItems.Where(w => w.UserId == userId);
            _context.WishlistItems.RemoveRange(wishlistItems);

            var compareItems = _context.CompareItems.Where(c => c.UserId == userId);
            _context.CompareItems.RemoveRange(compareItems);

            var orders = _context.Orders.Where(o => o.UserId == userId);
            _context.Orders.RemoveRange(orders);

            var downloads = _context.Downloads.Where(d => d.UserId == userId);
            _context.Downloads.RemoveRange(downloads);

            var supportTickets = _context.SupportTickets.Where(t => t.UserId == userId);
            _context.SupportTickets.RemoveRange(supportTickets);

            var followedSellers = _context.FollowedSellers.Where(fs => fs.UserId == userId || fs.SellerId == userId);
            _context.FollowedSellers.RemoveRange(followedSellers);

            var messages = _context.Messages.Where(m => m.SenderId == userId || m.ReceiverId == userId);
            _context.Messages.RemoveRange(messages);

            if (user.Role == UserRole.Seller)
            {
                var sellerQuotes = _context.Quotes.Where(q => q.SellerId == userId);
                _context.Quotes.RemoveRange(sellerQuotes);

                var products = _context.Products.Where(p => p.SellerId == userId);
                _context.Products.RemoveRange(products);
            }
            else if (user.Role == UserRole.Buyer)
            {
                var rfqs = _context.Rfqs.Where(r => r.BuyerId == userId);
                _context.Rfqs.RemoveRange(rfqs);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { message = "Account successfully deleted" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error deleting account: " + ex.Message });
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public string? AlternateEmail { get; set; }
    public string? TaxNumber { get; set; }
    public string? CompanyAddress { get; set; }
    public string? YearEstablished { get; set; }
    public string? WebsiteUrl { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? LoginType { get; set; }
}

public class UpdateProfileRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string AlternateEmail { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string YearEstablished { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
}
