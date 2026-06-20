using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using EtrailGlobal.Database;
using EtrailGlobal.Database.Models;

namespace EtrailGlobal.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomerController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int userId))
        {
            throw new UnauthorizedAccessException();
        }
        return userId;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            int userId = GetCurrentUserId();
            var cartCount = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            var wishlistCount = await _context.WishlistItems.Where(w => w.UserId == userId).CountAsync();
            var compareCount = await _context.CompareItems.Where(c => c.UserId == userId).CountAsync();
            var orderCount = await _context.Orders.Where(o => o.UserId == userId).CountAsync();

            return Ok(new
            {
                cartCount,
                wishlistCount,
                compareCount,
                orderCount
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        int userId = GetCurrentUserId();
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("downloads")]
    public async Task<IActionResult> GetDownloads()
    {
        int userId = GetCurrentUserId();
        var downloads = await _context.Downloads
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DownloadDate)
            .ToListAsync();
        return Ok(downloads);
    }

    [HttpGet("wishlist")]
    public async Task<IActionResult> GetWishlist()
    {
        int userId = GetCurrentUserId();
        var wishlist = await _context.WishlistItems
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
            .Select(w => new
            {
                w.Id,
                productId = w.ProductId,
                name = w.Product != null ? w.Product.Name : "Unknown Product",
                category = w.Product != null ? w.Product.Category : "N/A",
                imageUrl = w.Product != null ? w.Product.ImageUrl : "",
                price = w.Product != null ? w.Product.Price : 0.0,
                discountPrice = w.Product != null ? w.Product.DiscountPrice : 0.0,
                minOrderQuantity = w.Product != null ? w.Product.MinOrderQuantity : 1,
                stock = w.Product != null ? w.Product.Stock : 0,
                leadTimeDays = w.Product != null ? w.Product.LeadTimeDays : 0
            })
            .ToListAsync();
        return Ok(wishlist);
    }

    [HttpPost("wishlist")]
    public async Task<IActionResult> AddToWishlist([FromBody] WishlistRequest request)
    {
        int userId = GetCurrentUserId();
        var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == request.ProductId);
        if (exists)
        {
            return BadRequest(new { message = "Product already in wishlist" });
        }

        var item = new WishlistItem
        {
            UserId = userId,
            ProductId = request.ProductId
        };

        _context.WishlistItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Added to wishlist" });
    }

    [HttpDelete("wishlist/{productId}")]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        int userId = GetCurrentUserId();
        var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (item == null)
        {
            return NotFound(new { message = "Item not found in wishlist" });
        }

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Removed from wishlist" });
    }

    [HttpGet("cart")]
    public async Task<IActionResult> GetCart()
    {
        int userId = GetCurrentUserId();
        var cart = await _context.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .Select(c => new
            {
                c.Id,
                productId = c.ProductId,
                name = c.Product != null ? c.Product.Name : "Unknown Product",
                category = c.Product != null ? c.Product.Category : "N/A",
                imageUrl = c.Product != null ? c.Product.ImageUrl : "",
                price = c.Product != null ? c.Product.Price : 0.0,
                discountPrice = c.Product != null ? c.Product.DiscountPrice : 0.0,
                minOrderQuantity = c.Product != null ? c.Product.MinOrderQuantity : 1,
                stock = c.Product != null ? c.Product.Stock : 0,
                leadTimeDays = c.Product != null ? c.Product.LeadTimeDays : 0,
                quantity = c.Quantity
            })
            .ToListAsync();
        return Ok(cart);
    }

    [HttpPost("cart")]
    public async Task<IActionResult> AddToCart([FromBody] CartRequest request)
    {
        int userId = GetCurrentUserId();
        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);
        if (item != null)
        {
            item.Quantity += request.Quantity > 0 ? request.Quantity : 1;
            _context.CartItems.Update(item);
        }
        else
        {
            item = new CartItem
            {
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = request.Quantity > 0 ? request.Quantity : 1
            };
            _context.CartItems.Add(item);
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Added to cart" });
    }

    [HttpPut("cart/{productId}")]
    public async Task<IActionResult> UpdateCartItemQuantity(int productId, [FromBody] UpdateCartQuantityRequest request)
    {
        int userId = GetCurrentUserId();
        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (item == null)
        {
            return NotFound(new { message = "Item not found in cart" });
        }
        if (request.Quantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
            _context.CartItems.Update(item);
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cart updated" });
    }

    [HttpDelete("cart/{productId}")]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        int userId = GetCurrentUserId();
        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (item == null)
        {
            return NotFound(new { message = "Item not found in cart" });
        }
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Removed from cart" });
    }

    [HttpDelete("cart/clear")]
    public async Task<IActionResult> ClearCart()
    {
        int userId = GetCurrentUserId();
        var items = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cart cleared" });
    }

    [HttpGet("compare")]
    public async Task<IActionResult> GetCompare()
    {
        int userId = GetCurrentUserId();
        var compare = await _context.CompareItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .Select(c => new
            {
                c.Id,
                productId = c.ProductId,
                name = c.Product != null ? c.Product.Name : "Unknown Product",
                category = c.Product != null ? c.Product.Category : "N/A",
                imageUrl = c.Product != null ? c.Product.ImageUrl : "",
                price = c.Product != null ? c.Product.Price : 0.0,
                discountPrice = c.Product != null ? c.Product.DiscountPrice : 0.0,
                minOrderQuantity = c.Product != null ? c.Product.MinOrderQuantity : 1,
                stock = c.Product != null ? c.Product.Stock : 0,
                leadTimeDays = c.Product != null ? c.Product.LeadTimeDays : 0,
                description = c.Product != null ? c.Product.Description : ""
            })
            .ToListAsync();
        return Ok(compare);
    }

    [HttpPost("compare")]
    public async Task<IActionResult> AddToCompare([FromBody] CompareRequest request)
    {
        int userId = GetCurrentUserId();
        var exists = await _context.CompareItems.AnyAsync(c => c.UserId == userId && c.ProductId == request.ProductId);
        if (exists)
        {
            return BadRequest(new { message = "Product already in comparison list" });
        }

        var count = await _context.CompareItems.CountAsync(c => c.UserId == userId);
        if (count >= 4)
        {
            return BadRequest(new { message = "You can compare up to 4 products at a time" });
        }

        var item = new CompareItem
        {
            UserId = userId,
            ProductId = request.ProductId
        };

        _context.CompareItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Added to comparison list" });
    }

    [HttpDelete("compare/{productId}")]
    public async Task<IActionResult> RemoveFromCompare(int productId)
    {
        int userId = GetCurrentUserId();
        var item = await _context.CompareItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (item == null)
        {
            return NotFound(new { message = "Item not found in comparison list" });
        }

        _context.CompareItems.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Removed from comparison list" });
    }

    [HttpGet("followed-sellers")]
    public async Task<IActionResult> GetFollowedSellers()
    {
        int userId = GetCurrentUserId();
        var sellers = await _context.FollowedSellers
            .Where(f => f.UserId == userId)
            .Include(f => f.Seller)
            .Select(f => new
            {
                f.Id,
                sellerId = f.SellerId,
                companyName = f.Seller != null ? f.Seller.CompanyName : "Unknown Supplier",
                username = f.Seller != null ? f.Seller.Username : "Supplier",
                businessType = f.Seller != null ? f.Seller.BusinessType : "Manufacturer",
                contactInfo = f.Seller != null ? f.Seller.ContactInfo : "",
                isVerified = f.Seller != null ? f.Seller.IsVerified : false
            })
            .ToListAsync();
        return Ok(sellers);
    }

    [HttpPost("followed-sellers")]
    public async Task<IActionResult> FollowSeller([FromBody] FollowRequest request)
    {
        int userId = GetCurrentUserId();
        var seller = await _context.Users.FindAsync(request.SellerId);
        if (seller == null || seller.Role != UserRole.Seller)
        {
            return NotFound(new { message = "Seller not found" });
        }

        var exists = await _context.FollowedSellers.AnyAsync(f => f.UserId == userId && f.SellerId == request.SellerId);
        if (exists)
        {
            return BadRequest(new { message = "You are already following this seller" });
        }

        var follow = new FollowedSeller
        {
            UserId = userId,
            SellerId = request.SellerId
        };

        _context.FollowedSellers.Add(follow);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Followed seller successfully" });
    }

    [HttpDelete("followed-sellers/{sellerId}")]
    public async Task<IActionResult> UnfollowSeller(int sellerId)
    {
        int userId = GetCurrentUserId();
        var follow = await _context.FollowedSellers.FirstOrDefaultAsync(f => f.UserId == userId && f.SellerId == sellerId);
        if (follow == null)
        {
            return NotFound(new { message = "You are not following this seller" });
        }

        _context.FollowedSellers.Remove(follow);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Unfollowed seller successfully" });
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets()
    {
        int userId = GetCurrentUserId();
        var tickets = await _context.SupportTickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return Ok(tickets);
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] SupportTicketRequest request)
    {
        int userId = GetCurrentUserId();
        var ticket = new SupportTicket
        {
            UserId = userId,
            Subject = request.Subject,
            Description = request.Description,
            Priority = request.Priority,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Ticket submitted successfully", ticketId = ticket.Id });
    }

    [HttpDelete("tickets/{id}")]
    public async Task<IActionResult> CloseTicket(int id)
    {
        int userId = GetCurrentUserId();
        var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);
        if (ticket == null)
        {
            return NotFound(new { message = "Ticket not found" });
        }

        ticket.Status = "Closed";
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Ticket closed successfully" });
    }

    [AllowAnonymous]
    [HttpGet("reviews/seller/{sellerId}")]
    public async Task<IActionResult> GetSellerReviews(int sellerId)
    {
        var reviews = await _context.SellerReviews
            .Where(r => r.SellerId == sellerId)
            .Include(r => r.Buyer)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.BuyerId,
                BuyerName = r.Buyer != null ? r.Buyer.Username : "Anonymous",
                r.SellerId,
                r.Rating,
                r.Comment,
                r.CreatedAt
            })
            .ToListAsync();
        return Ok(reviews);
    }

    [AllowAnonymous]
    [HttpGet("reviews/stats/{sellerId}")]
    public async Task<IActionResult> GetSellerStats(int sellerId)
    {
        var reviews = await _context.SellerReviews.Where(r => r.SellerId == sellerId).ToListAsync();
        var followerCount = await _context.FollowedSellers.CountAsync(f => f.SellerId == sellerId);
        
        var total = reviews.Count;
        var average = total > 0 ? reviews.Average(r => r.Rating) : 0.0;
        
        var ratingBreakout = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
        foreach (var r in reviews)
        {
            if (ratingBreakout.ContainsKey(r.Rating))
            {
                ratingBreakout[r.Rating]++;
            }
        }

        return Ok(new
        {
            sellerId,
            followerCount,
            reviewCount = total,
            averageRating = Math.Round(average, 1),
            ratingBreakout
        });
    }

    [HttpPost("reviews")]
    public async Task<IActionResult> AddReview([FromBody] CreateReviewRequest request)
    {
        int buyerId = GetCurrentUserId();
        if (buyerId == request.SellerId)
        {
            return BadRequest(new { message = "You cannot review your own store." });
        }

        var seller = await _context.Users.FindAsync(request.SellerId);
        if (seller == null || seller.Role != UserRole.Seller)
        {
            return NotFound(new { message = "Seller not found" });
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5 stars." });
        }

        var existing = await _context.SellerReviews
            .FirstOrDefaultAsync(r => r.BuyerId == buyerId && r.SellerId == request.SellerId);
        if (existing != null)
        {
            existing.Rating = request.Rating;
            existing.Comment = request.Comment;
            existing.CreatedAt = DateTime.UtcNow;
            _context.SellerReviews.Update(existing);
        }
        else
        {
            var review = new SellerReview
            {
                BuyerId = buyerId,
                SellerId = request.SellerId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };
            _context.SellerReviews.Add(review);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Review submitted successfully" });
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpGet("seller-followers")]
    public async Task<IActionResult> GetSellerFollowers()
    {
        try
        {
            int sellerId = GetCurrentUserId();
            var followers = await _context.FollowedSellers
                .Where(f => f.SellerId == sellerId)
                .Include(f => f.User)
                .Select(f => new
                {
                    f.Id,
                    buyerId = f.UserId,
                    username = f.User != null ? f.User.Username : "Buyer",
                    email = f.User != null ? f.User.Email : "",
                    contactInfo = f.User != null ? f.User.ContactInfo : "",
                    followedAt = f.CreatedAt
                })
                .ToListAsync();
            return Ok(followers);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpGet("seller-stats")]
    public async Task<IActionResult> GetSellerStatsDashboard()
    {
        try
        {
            int sellerId = GetCurrentUserId();
            
            var totalProducts = await _context.Products.CountAsync(p => p.SellerId == sellerId);
            var approvedProducts = await _context.Products.CountAsync(p => p.SellerId == sellerId && p.IsApproved);
            var pendingProducts = totalProducts - approvedProducts;
            
            var followerCount = await _context.FollowedSellers.CountAsync(f => f.SellerId == sellerId);
            
            var reviews = await _context.SellerReviews.Where(r => r.SellerId == sellerId).ToListAsync();
            var reviewCount = reviews.Count;
            var averageRating = reviewCount > 0 ? reviews.Average(r => r.Rating) : 0.0;
            
            var sellerProductNames = await _context.Products
                .Where(p => p.SellerId == sellerId)
                .Select(p => p.Name)
                .ToListAsync();

            var allOrders = await _context.Orders.Include(o => o.User).ToListAsync();
            var sellerOrders = new List<object>();
            decimal totalRevenue = 0;
            int totalItemsSold = 0;

            foreach (var order in allOrders)
            {
                var items = new List<OrderItemDto>();
                try
                {
                    if (!string.IsNullOrEmpty(order.ItemDetailsJson))
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemDto>>(order.ItemDetailsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (parsed != null)
                        {
                            items = parsed.Where(i => sellerProductNames.Any(spn => spn.Equals(i.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                        }
                    }
                }
                catch {}

                if (items.Count > 0)
                {
                    decimal orderSellerTotal = items.Sum(i => i.Qty * i.Price);
                    totalRevenue += orderSellerTotal;
                    totalItemsSold += items.Sum(i => i.Qty);

                    sellerOrders.Add(new
                    {
                        order.Id,
                        order.OrderNumber,
                        order.OrderDate,
                        order.Status,
                        order.PaymentMethod,
                        order.BillingAddress,
                        order.ShippingAddress,
                        CustomerName = order.User?.Username ?? "Customer",
                        CustomerEmail = order.User?.Email ?? "",
                        SellerItems = items,
                        SellerTotal = orderSellerTotal
                    });
                }
            }

            return Ok(new
            {
                totalProducts,
                approvedProducts,
                pendingProducts,
                followerCount,
                reviewCount,
                averageRating = Math.Round(averageRating, 1),
                totalRevenue,
                totalItemsSold,
                orders = sellerOrders
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        try
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                return BadRequest(new { message = "Your cart is empty" });
            }

            // Create Order
            var orderNumber = "ET-" + new Random().Next(10000, 99999);
            decimal totalAmount = 0;

            var itemsList = new List<OrderItemDto>();
            foreach (var item in cartItems)
            {
                if (item.Product == null) continue;
                
                // calculate price based on product discount
                var hasDiscount = item.Product.DiscountPrice > 0 && item.Product.DiscountPrice < item.Product.Price;
                var activePrice = hasDiscount ? item.Product.DiscountPrice : item.Product.Price;

                totalAmount += (decimal)(activePrice * item.Quantity);

                itemsList.Add(new OrderItemDto
                {
                    Name = item.Product.Name,
                    Qty = item.Quantity,
                    Price = (decimal)activePrice,
                    ImageUrl = item.Product.ImageUrl
                });

                // Deduct stock
                if (item.Product.Stock >= item.Quantity)
                {
                    item.Product.Stock -= item.Quantity;
                }
                else
                {
                    item.Product.Stock = 0;
                }
            }

            var order = new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status = "Pending",
                PaymentMethod = request.PaymentMethod ?? "Cash on Delivery",
                BillingAddress = request.BillingAddress ?? user.BillingAddress ?? "N/A",
                ShippingAddress = request.ShippingAddress ?? user.ShippingAddress ?? "N/A",
                ItemDetailsJson = System.Text.Json.JsonSerializer.Serialize(itemsList)
            };

            // Save order and clear cart
            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order placed successfully", orderNumber = orderNumber });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpPut("orders/{orderId}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(new { message = "Order not found" });
        }

        order.Status = request.Status;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Order status updated successfully", status = order.Status });
    }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class CheckoutRequest
{
    public string? PaymentMethod { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
}

public class WishlistRequest
{
    public int ProductId { get; set; }
}

public class CompareRequest
{
    public int ProductId { get; set; }
}

public class FollowRequest
{
    public int SellerId { get; set; }
}

public class SupportTicketRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Low";
}

public class CartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartQuantityRequest
{
    public int Quantity { get; set; }
}

public class CreateReviewRequest
{
    public int SellerId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

