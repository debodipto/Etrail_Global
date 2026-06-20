using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using EtrailGlobal.Database;
using EtrailGlobal.Database.Models;

namespace EtrailGlobal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadMedia([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { message = "No files uploaded" });
        }

        var uploadedUrls = new List<string>();
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                uploadedUrls.Add("/uploads/" + uniqueFileName);
            }
        }

        return Ok(new { urls = uploadedUrls });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProducts(
        [FromQuery] string? search, 
        [FromQuery] string? category, 
        [FromQuery] int? sellerId,
        [FromQuery] bool? isFeatured,
        [FromQuery] bool? isBestSeller,
        [FromQuery] bool? isTodayDeal,
        [FromQuery] bool? isApproved)
    {
        var query = _context.Products.Include(p => p.Seller).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        }

        if (!string.IsNullOrEmpty(category) && category != "All")
        {
            query = query.Where(p => p.Category == category);
        }

        if (sellerId.HasValue)
        {
            query = query.Where(p => p.SellerId == sellerId.Value);
        }

        if (isFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == isFeatured.Value);
        }

        if (isBestSeller.HasValue)
        {
            query = query.Where(p => p.IsBestSeller == isBestSeller.Value);
        }

        if (isTodayDeal.HasValue)
        {
            query = query.Where(p => p.IsTodayDeal == isTodayDeal.Value);
        }

        // Apply IsApproved filtering:
        // - Admin/Seller querying themselves can see unapproved.
        // - Everyone else only sees approved.
        var currentUserId = 0;
        var isAdmin = false;
        var authUserClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(authUserClaim) && int.TryParse(authUserClaim, out int loggedInId))
        {
            currentUserId = loggedInId;
            isAdmin = User.IsInRole("Admin");
        }

        if (isApproved.HasValue)
        {
            if (isAdmin)
            {
                query = query.Where(p => p.IsApproved == isApproved.Value);
            }
            else if (sellerId.HasValue && sellerId.Value == currentUserId)
            {
                query = query.Where(p => p.IsApproved == isApproved.Value && p.SellerId == currentUserId);
            }
            else
            {
                query = query.Where(p => p.IsApproved == true);
            }
        }
        else
        {
            if (isAdmin)
            {
                // Admin sees all by default
            }
            else if (sellerId.HasValue && sellerId.Value == currentUserId)
            {
                // Seller sees all their own products
                query = query.Where(p => p.SellerId == sellerId.Value);
            }
            else
            {
                // Normal users only see approved products
                query = query.Where(p => p.IsApproved == true);
            }
        }

        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        
        var result = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Category,
            p.Price,
            p.DiscountPrice,
            p.MinOrderQuantity,
            p.Stock,
            p.LeadTimeDays,
            p.ImageUrl,
            p.MediaJson,
            p.CreatedAt,
            p.SellerId,
            p.IsFeatured,
            p.IsBestSeller,
            p.IsTodayDeal,
            p.IsApproved,
            p.SpecificationsJson,
            SellerName = p.Seller?.Username ?? "Unknown Seller",
            SellerCompany = p.Seller?.CompanyName ?? "Independent",
            SellerVerified = p.Seller?.IsVerified ?? false
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _context.Products.Include(p => p.Seller).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        return Ok(new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Category,
            product.Price,
            product.DiscountPrice,
            product.MinOrderQuantity,
            product.Stock,
            product.LeadTimeDays,
            product.ImageUrl,
            product.MediaJson,
            product.CreatedAt,
            product.IsApproved,
            product.SpecificationsJson,
            SellerName = product.Seller?.Username ?? "Unknown Seller",
            SellerCompany = product.Seller?.CompanyName ?? "Independent",
            SellerVerified = product.Seller?.IsVerified ?? false,
            SellerContact = product.Seller?.ContactInfo ?? string.Empty,
            SellerWhatsApp = product.Seller?.WhatsAppNumber ?? string.Empty,
            SellerAddress = product.Seller?.CompanyAddress ?? string.Empty,
            SellerYearEstablished = product.Seller?.YearEstablished ?? string.Empty,
            SellerWebsiteUrl = product.Seller?.WebsiteUrl ?? string.Empty
        });
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Unauthorized user" });

        var seller = await _context.Users.FindAsync(userId);
        if (seller == null)
            return Unauthorized(new { message = "Seller profile not found" });

        if (!seller.IsVerified && seller.Role != UserRole.Admin)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your seller account is pending admin verification." });

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            MinOrderQuantity = request.MinOrderQuantity,
            Stock = request.Stock,
            LeadTimeDays = request.LeadTimeDays,
            ImageUrl = string.IsNullOrEmpty(request.ImageUrl) ? "/images/placeholder.png" : request.ImageUrl,
            MediaJson = string.IsNullOrEmpty(request.MediaJson) ? "[]" : request.MediaJson,
            SellerId = userId,
            IsApproved = User.IsInRole("Admin"), // admin is auto-approved, seller needs verification
            SpecificationsJson = string.IsNullOrEmpty(request.SpecificationsJson) ? "{}" : request.SpecificationsJson
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Unauthorized" });

        if (product.SellerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        product.Name = request.Name;
        product.Description = request.Description;
        product.Category = request.Category;
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.MinOrderQuantity = request.MinOrderQuantity;
        product.Stock = request.Stock;
        product.LeadTimeDays = request.LeadTimeDays;
        if (!string.IsNullOrEmpty(request.ImageUrl))
            product.ImageUrl = request.ImageUrl;
        if (!string.IsNullOrEmpty(request.MediaJson))
            product.MediaJson = request.MediaJson;
        
        product.SpecificationsJson = string.IsNullOrEmpty(request.SpecificationsJson) ? "{}" : request.SpecificationsJson;
        
        // Reset approval status if updated by a Seller (Admin keeps approval state)
        if (!User.IsInRole("Admin"))
        {
            product.IsApproved = false;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Product updated successfully. It will be live once approved by an admin.", product });
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Unauthorized" });

        if (product.SellerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product deleted successfully" });
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpPut("{id}/stock")]
    public async Task<IActionResult> UpdateProductStock(int id, [FromBody] UpdateStockRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Unauthorized" });

        if (product.SellerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        product.Stock = request.Stock;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product stock updated successfully", stock = product.Stock });
    }
}

public class UpdateStockRequest
{
    public int Stock { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Price { get; set; }
    public double DiscountPrice { get; set; }
    public int MinOrderQuantity { get; set; }
    public int Stock { get; set; }
    public int LeadTimeDays { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string MediaJson { get; set; } = "[]";
    public string SpecificationsJson { get; set; } = "{}";
}
