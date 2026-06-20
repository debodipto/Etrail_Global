using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
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

    // ==========================================
    // 1. DASHBOARD & OVERVIEW
    // ==========================================

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

        // New Advanced stats
        var totalEarnings = await _context.Orders.Where(o => o.IsPaid).SumAsync(o => o.TotalAmount);
        var totalCommission = await _context.Orders.Where(o => o.IsPaid && o.OrderType == "Seller").SumAsync(o => o.CommissionEarned);
        var totalPayouts = await _context.PayoutRequests.Where(p => p.Status == "Approved").SumAsync(p => p.Amount);
        var pendingPayoutRequests = await _context.PayoutRequests.CountAsync(p => p.Status == "Pending");

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
            messages,
            totalEarnings = (double)totalEarnings,
            totalCommission = (double)totalCommission,
            totalPayouts = (double)totalPayouts,
            pendingPayoutRequests
        });
    }

    // ==========================================
    // 2. USERS MANAGEMENT
    // ==========================================

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
                u.WalletBalance,
                u.CommissionRate,
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

        var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (user.Email == adminEmail)
            return BadRequest(new { message = "You cannot delete the admin account currently in use." });

        await DeleteUserGraph(id, user.Role);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User and related records deleted successfully" });
    }

    // ==========================================
    // 3. PRODUCTS MANAGEMENT & SUB-FEATURES
    // ==========================================

    [HttpGet("products/all")]
    public async Task<IActionResult> GetAdminProducts()
    {
        var products = await _context.Products.Include(p => p.Seller).OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(products);
    }

    [HttpGet("products/inhouse")]
    public async Task<IActionResult> GetInhouseProducts()
    {
        // Admin products
        var adminIds = await _context.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync();
        var products = await _context.Products
            .Include(p => p.Seller)
            .Where(p => adminIds.Contains(p.SellerId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("products/seller")]
    public async Task<IActionResult> GetSellerProducts()
    {
        // Non-admin products
        var adminIds = await _context.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync();
        var products = await _context.Products
            .Include(p => p.Seller)
            .Where(p => !adminIds.Contains(p.SellerId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("products/digital")]
    public async Task<IActionResult> GetDigitalProducts()
    {
        var products = await _context.Products
            .Include(p => p.Seller)
            .Where(p => p.IsDigital)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(products);
    }

    [HttpPost("products/create")]
    public async Task<IActionResult> CreateProduct([FromBody] AdminCreateProductRequest request)
    {
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
        int targetSellerId = request.SellerId ?? (adminUser?.Id ?? 1);

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
            MediaJson = request.MediaJson,
            SpecificationsJson = request.SpecificationsJson,
            SellerId = targetSellerId,
            IsApproved = true, // Admin creations are auto-approved
            IsDigital = request.IsDigital,
            DigitalFileUrl = request.DigitalFileUrl,
            Brand = request.Brand,
            CustomLabel = request.CustomLabel,
            AttributesJson = request.AttributesJson,
            ColorsJson = request.ColorsJson
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Product created successfully", product });
    }

    [HttpPut("products/{id}/update")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] AdminCreateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

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
        product.MediaJson = request.MediaJson;
        product.SpecificationsJson = request.SpecificationsJson;
        product.IsDigital = request.IsDigital;
        product.DigitalFileUrl = request.DigitalFileUrl;
        product.Brand = request.Brand;
        product.CustomLabel = request.CustomLabel;
        product.AttributesJson = request.AttributesJson;
        product.ColorsJson = request.ColorsJson;
        if (request.SellerId.HasValue)
            product.SellerId = request.SellerId.Value;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Product updated successfully", product });
    }

    // --- BULK IMPORT & EXPORT ---
    [HttpGet("products/export")]
    public async Task<IActionResult> ExportProducts()
    {
        var products = await _context.Products.ToListAsync();
        var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "products_export.json");
    }

    [HttpPost("products/import")]
    public async Task<IActionResult> ImportProducts([FromBody] List<AdminCreateProductRequest> requests)
    {
        if (requests == null || requests.Count == 0)
            return BadRequest(new { message = "Invalid data provided for import." });

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
        int defaultAdminId = adminUser?.Id ?? 1;

        foreach (var req in requests)
        {
            var p = new Product
            {
                Name = req.Name,
                Description = req.Description,
                Category = req.Category,
                Price = req.Price,
                DiscountPrice = req.DiscountPrice,
                MinOrderQuantity = req.MinOrderQuantity,
                Stock = req.Stock,
                LeadTimeDays = req.LeadTimeDays,
                ImageUrl = string.IsNullOrEmpty(req.ImageUrl) ? "/images/placeholder.png" : req.ImageUrl,
                MediaJson = req.MediaJson,
                SpecificationsJson = req.SpecificationsJson,
                SellerId = req.SellerId ?? defaultAdminId,
                IsApproved = true,
                IsDigital = req.IsDigital,
                DigitalFileUrl = req.DigitalFileUrl,
                Brand = req.Brand,
                CustomLabel = req.CustomLabel,
                AttributesJson = req.AttributesJson,
                ColorsJson = req.ColorsJson
            };
            _context.Products.Add(p);
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = $"Successfully imported {requests.Count} products." });
    }

    // --- CATEGORY ---
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return Ok(category);
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = request.Name;
        category.DiscountPercentage = request.DiscountPercentage;
        category.BannerUrl = request.BannerUrl;

        await _context.SaveChangesAsync();
        return Ok(category);
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Category deleted successfully" });
    }

    // --- BRAND ---
    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands()
    {
        var brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        return Ok(brands);
    }

    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand([FromBody] Brand brand)
    {
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();
        return Ok(brand);
    }

    [HttpPut("brands/{id}")]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] Brand request)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand == null) return NotFound();

        brand.Name = request.Name;
        brand.LogoUrl = request.LogoUrl;

        await _context.SaveChangesAsync();
        return Ok(brand);
    }

    [HttpDelete("brands/{id}")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand == null) return NotFound();

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Brand deleted successfully" });
    }

    // --- ATTRIBUTE ---
    [HttpGet("attributes")]
    public async Task<IActionResult> GetAttributes()
    {
        var attributes = await _context.ProductAttributes.OrderBy(a => a.Name).ToListAsync();
        return Ok(attributes);
    }

    [HttpPost("attributes")]
    public async Task<IActionResult> CreateAttribute([FromBody] ProductAttribute attribute)
    {
        _context.ProductAttributes.Add(attribute);
        await _context.SaveChangesAsync();
        return Ok(attribute);
    }

    [HttpPut("attributes/{id}")]
    public async Task<IActionResult> UpdateAttribute(int id, [FromBody] ProductAttribute request)
    {
        var attribute = await _context.ProductAttributes.FindAsync(id);
        if (attribute == null) return NotFound();

        attribute.Name = request.Name;
        attribute.ValuesJson = request.ValuesJson;

        await _context.SaveChangesAsync();
        return Ok(attribute);
    }

    [HttpDelete("attributes/{id}")]
    public async Task<IActionResult> DeleteAttribute(int id)
    {
        var attribute = await _context.ProductAttributes.FindAsync(id);
        if (attribute == null) return NotFound();

        _context.ProductAttributes.Remove(attribute);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Attribute deleted successfully" });
    }

    // --- COLORS ---
    [HttpGet("colors")]
    public async Task<IActionResult> GetColors()
    {
        var colors = await _context.Colors.OrderBy(c => c.Name).ToListAsync();
        return Ok(colors);
    }

    [HttpPost("colors")]
    public async Task<IActionResult> CreateColor([FromBody] Color color)
    {
        _context.Colors.Add(color);
        await _context.SaveChangesAsync();
        return Ok(color);
    }

    [HttpPut("colors/{id}")]
    public async Task<IActionResult> UpdateColor(int id, [FromBody] Color request)
    {
        var color = await _context.Colors.FindAsync(id);
        if (color == null) return NotFound();

        color.Name = request.Name;
        color.Code = request.Code;

        await _context.SaveChangesAsync();
        return Ok(color);
    }

    [HttpDelete("colors/{id}")]
    public async Task<IActionResult> DeleteColor(int id)
    {
        var color = await _context.Colors.FindAsync(id);
        if (color == null) return NotFound();

        _context.Colors.Remove(color);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Color deleted successfully" });
    }

    // --- SMART BAR ---
    [HttpGet("smartbar")]
    public async Task<IActionResult> GetSmartBar()
    {
        var settings = await _context.SmartBarSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SmartBarSetting
            {
                AnnouncementText = "Welcome to E-trail Global Platform Announcement!",
                BgColor = "#146c43",
                TextColor = "#ffffff",
                Link = "",
                IsActive = false
            };
            _context.SmartBarSettings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return Ok(settings);
    }

    [HttpPost("smartbar")]
    public async Task<IActionResult> SaveSmartBar([FromBody] SmartBarSetting request)
    {
        var settings = await _context.SmartBarSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SmartBarSetting();
            _context.SmartBarSettings.Add(settings);
        }

        settings.AnnouncementText = request.AnnouncementText;
        settings.BgColor = request.BgColor;
        settings.TextColor = request.TextColor;
        settings.Link = request.Link;
        settings.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(settings);
    }

    // --- PRODUCT REVIEWS ---
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews()
    {
        var reviews = await _context.ProductReviews
            .Include(r => r.Buyer)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                ProductName = r.Product != null ? r.Product.Name : "Deleted Product",
                BuyerName = r.Buyer != null ? r.Buyer.Username : "Guest Buyer"
            })
            .ToListAsync();
        return Ok(reviews);
    }

    [HttpDelete("reviews/{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null) return NotFound();

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Review deleted successfully" });
    }

    // ==========================================
    // 4. SALES MANAGEMENT
    // ==========================================

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
                o.IsPaid,
                o.PickupPoint,
                o.OrderType,
                o.CommissionEarned,
                CustomerName = o.User != null ? o.User.Username : "Deleted user",
                CustomerEmail = o.User != null ? o.User.Email : string.Empty
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("orders/inhouse")]
    public async Task<IActionResult> GetInhouseOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Where(o => o.OrderType == "InHouse")
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("orders/seller")]
    public async Task<IActionResult> GetSellerOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Where(o => o.OrderType == "Seller")
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("orders/pickup")]
    public async Task<IActionResult> GetPickupPointOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Where(o => o.PickupPoint != null && o.PickupPoint != "")
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("orders/unpaid")]
    public async Task<IActionResult> GetUnpaidOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Where(o => !o.IsPaid)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpPut("orders/{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateRequest request)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = request.Status;
        order.IsPaid = request.IsPaid;
        order.PickupPoint = request.PickupPoint ?? string.Empty;

        await _context.SaveChangesAsync();
        return Ok(order);
    }

    // ==========================================
    // 5. CUSTOMER MANAGEMENT
    // ==========================================

    [HttpGet("customers/list")]
    public async Task<IActionResult> GetCustomerList()
    {
        var customers = await _context.Users
            .Where(u => u.Role == UserRole.Buyer)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        return Ok(customers);
    }

    [HttpGet("customers/unverified")]
    public async Task<IActionResult> GetUnverifiedCustomers()
    {
        // Assume customer verification is a separate flag or email validation. Let's return buyers with IsVerified = false
        var customers = await _context.Users
            .Where(u => u.Role == UserRole.Buyer && !u.IsVerified)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        return Ok(customers);
    }

    [HttpPost("customers/{id}/verify")]
    public async Task<IActionResult> ToggleCustomerVerification(int id, [FromBody] VerifyToggleRequest request)
    {
        var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Buyer);
        if (customer == null) return NotFound();

        customer.IsVerified = request.IsVerified;
        await _context.SaveChangesAsync();
        return Ok(customer);
    }

    // ==========================================
    // 6. SELLER MANAGEMENT
    // ==========================================

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

    [HttpGet("sellers/applied")]
    public async Task<IActionResult> GetAppliedSellers()
    {
        // applied / pending verification sellers
        var sellers = await _context.Users
            .Where(u => u.Role == UserRole.Seller && !u.IsVerified)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        return Ok(sellers);
    }

    [HttpGet("sellers/ratings")]
    public async Task<IActionResult> GetSellerRatingsAndFollowers()
    {
        var sellers = await _context.Users.Where(u => u.Role == UserRole.Seller).ToListAsync();
        var ratings = await _context.SellerReviews.ToListAsync();
        var followers = await _context.FollowedSellers.ToListAsync();

        var result = sellers.Select(s => new {
            s.Id,
            s.Username,
            s.CompanyName,
            AverageRating = ratings.Where(r => r.SellerId == s.Id).Select(r => (double?)r.Rating).Average() ?? 0.0,
            ReviewsCount = ratings.Count(r => r.SellerId == s.Id),
            FollowersCount = followers.Count(f => f.SellerId == s.Id)
        });

        return Ok(result);
    }

    // --- PAYOUTS & REQUESTS ---
    [HttpGet("sellers/payouts")]
    public async Task<IActionResult> GetPayouts()
    {
        var payouts = await _context.PayoutRequests
            .Include(p => p.Seller)
            .Where(p => p.Status == "Approved")
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(payouts);
    }

    [HttpGet("sellers/payouts/requests")]
    public async Task<IActionResult> GetPayoutRequests()
    {
        var requests = await _context.PayoutRequests
            .Include(p => p.Seller)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(requests);
    }

    [HttpPost("sellers/payouts/requests/{id}/approve")]
    public async Task<IActionResult> ApprovePayoutRequest(int id)
    {
        var request = await _context.PayoutRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = "Approved";

        // Update Seller Wallet balance
        var seller = await _context.Users.FindAsync(request.SellerId);
        if (seller != null)
        {
            seller.WalletBalance = Math.Max(0.0m, seller.WalletBalance - request.Amount);
        }

        await _context.SaveChangesAsync();
        return Ok(request);
    }

    [HttpPost("sellers/payouts/requests/{id}/reject")]
    public async Task<IActionResult> RejectPayoutRequest(int id, [FromBody] PayoutRejectRequest rejectRequest)
    {
        var request = await _context.PayoutRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = "Rejected";
        request.Note = rejectRequest.Reason ?? "Rejected by admin";

        await _context.SaveChangesAsync();
        return Ok(request);
    }

    // --- COMMISSION SETTINGS ---
    [HttpGet("sellers/commission/settings")]
    public async Task<IActionResult> GetCommissionSettings()
    {
        var defaultSetting = await _context.SiteSettings.FindAsync("default_commission_rate");
        double defaultRate = defaultSetting != null ? double.Parse(defaultSetting.Value) : 10.0; // default 10%

        var sellerRates = await _context.Users
            .Where(u => u.Role == UserRole.Seller && u.CommissionRate > 0)
            .Select(u => new { u.Id, u.Username, u.CommissionRate })
            .ToListAsync();

        var categoryRates = await _context.CategoryCommissionRules.ToListAsync();

        return Ok(new {
            defaultRate,
            sellerRates,
            categoryRates
        });
    }

    [HttpPut("sellers/commission/default")]
    public async Task<IActionResult> SaveDefaultCommission([FromBody] CommissionRateRequest request)
    {
        var dbSetting = await _context.SiteSettings.FindAsync("default_commission_rate");
        if (dbSetting != null)
        {
            dbSetting.Value = request.Rate.ToString();
        }
        else
        {
            _context.SiteSettings.Add(new SiteSetting
            {
                Key = "default_commission_rate",
                Value = request.Rate.ToString(),
                Description = "Default commission percentage for seller products."
            });
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Default commission rate updated." });
    }

    [HttpPost("sellers/commission/seller")]
    public async Task<IActionResult> SetSellerCommission([FromBody] SellerCommissionRequest request)
    {
        var seller = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.SellerId && u.Role == UserRole.Seller);
        if (seller == null) return NotFound(new { message = "Seller not found." });

        seller.CommissionRate = request.Rate;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Seller commission rate updated." });
    }

    [HttpPost("sellers/commission/category")]
    public async Task<IActionResult> SetCategoryCommission([FromBody] CategoryCommissionRequest request)
    {
        var rule = await _context.CategoryCommissionRules.FirstOrDefaultAsync(c => c.CategoryName == request.CategoryName);
        if (rule != null)
        {
            rule.CommissionPercentage = request.Rate;
        }
        else
        {
            _context.CategoryCommissionRules.Add(new CategoryCommissionRule
            {
                CategoryName = request.CategoryName,
                CommissionPercentage = request.Rate
            });
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Category commission rate updated." });
    }

    // --- VERIFICATION FORM CONFIG ---
    [HttpGet("sellers/verification-form")]
    public async Task<IActionResult> GetVerificationForm()
    {
        var setting = await _context.SiteSettings.FindAsync("seller_verification_form_fields");
        var fieldsJson = setting != null ? setting.Value : "[\"Company Name\", \"Business Type\", \"Tax Number\", \"Trade License Address\", \"WhatsApp Contact\"]";
        return Ok(JsonSerializer.Deserialize<List<string>>(fieldsJson));
    }

    [HttpPut("sellers/verification-form")]
    public async Task<IActionResult> SaveVerificationForm([FromBody] List<string> fields)
    {
        var setting = await _context.SiteSettings.FindAsync("seller_verification_form_fields");
        var json = JsonSerializer.Serialize(fields);
        if (setting != null)
        {
            setting.Value = json;
        }
        else
        {
            _context.SiteSettings.Add(new SiteSetting
            {
                Key = "seller_verification_form_fields",
                Value = json,
                Description = "JSON array of fields required in the seller verification form."
            });
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Verification form fields saved." });
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

    // ==========================================
    // 7. REPORTS MODULE
    // ==========================================

    [HttpGet("reports/earning")]
    public async Task<IActionResult> GetEarningReport()
    {
        var orders = await _context.Orders.Where(o => o.IsPaid).ToListAsync();
        var commissions = await _context.CommissionHistories.ToListAsync();
        var recharges = await _context.WalletRecharges.Where(r => r.Status == "Success").ToListAsync();

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var commissionEarnings = commissions.Sum(c => c.CommissionAmount);
        var totalRecharged = recharges.Sum(r => r.Amount);

        // Group by Date for Charting
        var dailySales = orders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Amount = (double)g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(new {
            totalRevenue = (double)totalRevenue,
            commissionEarnings = (double)commissionEarnings,
            totalRecharged = (double)totalRecharged,
            dailySales
        });
    }

    [HttpGet("reports/inhouse-sales")]
    public async Task<IActionResult> GetInhouseSales()
    {
        var sales = await _context.Orders
            .Where(o => o.IsPaid && o.OrderType == "InHouse")
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(sales);
    }

    [HttpGet("reports/seller-sales")]
    public async Task<IActionResult> GetSellerSales()
    {
        var sales = await _context.Orders
            .Where(o => o.IsPaid && o.OrderType == "Seller")
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(sales);
    }

    [HttpGet("reports/stock")]
    public async Task<IActionResult> GetStockReport()
    {
        var report = await _context.Products
            .Select(p => new {
                p.Id,
                p.Name,
                p.Category,
                p.Stock,
                p.Price,
                Value = p.Stock * p.Price
            })
            .OrderBy(p => p.Stock)
            .ToListAsync();
        return Ok(report);
    }

    [HttpGet("reports/wishlist")]
    public async Task<IActionResult> GetWishlistReport()
    {
        var wishlistCounts = await _context.WishlistItems
            .GroupBy(w => w.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToListAsync();

        var products = await _context.Products.ToListAsync();

        var report = wishlistCounts.Select(wc => {
            var prod = products.FirstOrDefault(p => p.Id == wc.ProductId);
            return new {
                ProductId = wc.ProductId,
                ProductName = prod != null ? prod.Name : "Unknown Product",
                Count = wc.Count
            };
        }).OrderByDescending(x => x.Count).ToList();

        return Ok(report);
    }

    [HttpGet("reports/searches")]
    public async Task<IActionResult> GetSearchesReport()
    {
        var searches = await _context.UserSearches
            .OrderByDescending(s => s.SearchCount)
            .ToListAsync();
        return Ok(searches);
    }

    [HttpGet("reports/commissions")]
    public async Task<IActionResult> GetCommissionsReport()
    {
        var list = await _context.CommissionHistories
            .Include(c => c.Order)
            .Include(c => c.Seller)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new {
                c.Id,
                OrderNumber = c.Order != null ? c.Order.OrderNumber : "N/A",
                SellerName = c.Seller != null ? c.Seller.Username : "N/A",
                c.Amount,
                c.CommissionAmount,
                c.CreatedAt
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("reports/wallet-history")]
    public async Task<IActionResult> GetWalletRecharges()
    {
        var list = await _context.WalletRecharges
            .Include(w => w.User)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new {
                w.Id,
                UserName = w.User != null ? w.User.Username : "N/A",
                w.Amount,
                w.PaymentMethod,
                w.Status,
                w.CreatedAt
            })
            .ToListAsync();
        return Ok(list);
    }

    // ==========================================
    // 8. SUPPORT & SETTINGS (EXISTING LOGIC EXTENDED)
    // ==========================================

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
            _context.PayoutRequests.RemoveRange(_context.PayoutRequests.Where(p => p.SellerId == userId));
        }

        if (role == UserRole.Buyer)
        {
            var rfqIds = await _context.Rfqs.Where(r => r.BuyerId == userId).Select(r => r.Id).ToListAsync();
            _context.Quotes.RemoveRange(_context.Quotes.Where(q => rfqIds.Contains(q.RfqId)));
            _context.Rfqs.RemoveRange(_context.Rfqs.Where(r => r.BuyerId == userId));
            _context.WalletRecharges.RemoveRange(_context.WalletRecharges.Where(w => w.UserId == userId));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
        }
    }
}

public class OrderStatusUpdateRequest
{
    public string Status { get; set; } = "Pending";
    public bool IsPaid { get; set; }
    public string? PickupPoint { get; set; }
}

public class PayoutRejectRequest
{
    public string? Reason { get; set; }
}

public class CommissionRateRequest
{
    public double Rate { get; set; }
}

public class SellerCommissionRequest
{
    public int SellerId { get; set; }
    public double Rate { get; set; }
}

public class CategoryCommissionRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public double Rate { get; set; }
}

public class AdminCreateProductRequest
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

    public bool IsDigital { get; set; }
    public string DigitalFileUrl { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string CustomLabel { get; set; } = string.Empty;
    public string AttributesJson { get; set; } = "[]";
    public string ColorsJson { get; set; } = "[]";

    public int? SellerId { get; set; }
}

public class VerifyToggleRequest
{
    public bool IsVerified { get; set; }
}

public class UpdateTicketStatusRequest
{
    public string Status { get; set; } = "Open";
}
