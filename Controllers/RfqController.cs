using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EtrailGlobal.Database;
using EtrailGlobal.Database.Models;

namespace EtrailGlobal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RfqController : ControllerBase
{
    private readonly AppDbContext _context;

    public RfqController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRfqs([FromQuery] string? category, [FromQuery] string? search)
    {
        var query = _context.Rfqs.Include(r => r.Buyer).Where(r => r.Status == RfqStatus.Open).AsQueryable();

        if (!string.IsNullOrEmpty(category) && category != "All")
        {
            query = query.Where(r => r.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => r.Title.Contains(search) || r.Description.Contains(search));
        }

        var rfqs = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        var result = rfqs.Select(r => new
        {
            r.Id,
            r.Title,
            r.Description,
            r.Category,
            r.Quantity,
            r.Unit,
            r.BudgetPrice,
            r.ShippingTerms,
            r.ExpiryDate,
            r.Status,
            r.CreatedAt,
            r.BuyerId,
            BuyerName = r.Buyer?.Username ?? "Unknown Buyer",
            BuyerCompany = r.Buyer?.CompanyName ?? "Independent"
        });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyRfqs()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var rfqs = await _context.Rfqs
            .Include(r => r.Buyer)
            .Where(r => r.BuyerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(rfqs);
    }

    [Authorize]
    [HttpGet("my-quotes")]
    public async Task<IActionResult> GetMyQuotes()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var quotes = await _context.Quotes
            .Include(q => q.Rfq)
            .Include(q => q.Seller)
            .Where(q => q.SellerId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var result = quotes.Select(q => new
        {
            q.Id,
            q.RfqId,
            RfqTitle = q.Rfq?.Title ?? "Deleted RFQ",
            q.ProposedPrice,
            q.DeliveryTimeDays,
            q.Notes,
            q.Status,
            q.CreatedAt
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRfqById(int id)
    {
        var rfq = await _context.Rfqs.Include(r => r.Buyer).FirstOrDefaultAsync(r => r.Id == id);
        if (rfq == null)
            return NotFound(new { message = "RFQ not found" });

        var quotes = await _context.Quotes
            .Include(q => q.Seller)
            .Where(q => q.RfqId == id)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdClaim, out int userId);

        var allowedQuotes = quotes.Where(q => 
            userId == rfq.BuyerId || 
            User.IsInRole("Admin") || 
            q.SellerId == userId
        ).Select(q => new
        {
            q.Id,
            q.SellerId,
            SellerName = q.Seller?.Username ?? "Unknown Seller",
            SellerCompany = q.Seller?.CompanyName ?? "Independent",
            SellerVerified = q.Seller?.IsVerified ?? false,
            q.ProposedPrice,
            q.DeliveryTimeDays,
            q.Notes,
            q.Status,
            q.CreatedAt
        });

        return Ok(new
        {
            rfq = new
            {
                rfq.Id,
                rfq.Title,
                rfq.Description,
                rfq.Category,
                rfq.Quantity,
                rfq.Unit,
                rfq.BudgetPrice,
                rfq.ShippingTerms,
                rfq.ExpiryDate,
                rfq.Status,
                rfq.CreatedAt,
                rfq.BuyerId,
                BuyerName = rfq.Buyer?.Username ?? "Unknown Buyer",
                BuyerCompany = rfq.Buyer?.CompanyName ?? "Independent"
            },
            quotes = allowedQuotes
        });
    }

    [Authorize(Roles = "Buyer,Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateRfq([FromBody] CreateRfqRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var rfq = new Rfq
        {
            BuyerId = userId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Quantity = request.Quantity,
            Unit = request.Unit,
            BudgetPrice = request.BudgetPrice,
            ShippingTerms = request.ShippingTerms,
            ExpiryDate = DateTime.UtcNow.AddDays(request.ActiveDays > 0 ? request.ActiveDays : 30)
        };

        _context.Rfqs.Add(rfq);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRfqById), new { id = rfq.Id }, rfq);
    }

    [Authorize(Roles = "Seller,Admin")]
    [HttpPost("{id}/quote")]
    public async Task<IActionResult> SubmitQuote(int id, [FromBody] SubmitQuoteRequest request)
    {
        var rfq = await _context.Rfqs.FindAsync(id);
        if (rfq == null)
            return NotFound(new { message = "RFQ not found" });

        if (rfq.Status == RfqStatus.Closed || rfq.ExpiryDate < DateTime.UtcNow)
            return BadRequest(new { message = "This RFQ is no longer open for quotes" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var seller = await _context.Users.FindAsync(userId);
        if (seller == null || (!seller.IsVerified && seller.Role != UserRole.Admin))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your seller account is not verified by the Admin." });

        var existingQuote = await _context.Quotes.FirstOrDefaultAsync(q => q.RfqId == id && q.SellerId == userId);
        if (existingQuote != null)
        {
            existingQuote.ProposedPrice = request.ProposedPrice;
            existingQuote.DeliveryTimeDays = request.DeliveryTimeDays;
            existingQuote.Notes = request.Notes;
            existingQuote.CreatedAt = DateTime.UtcNow;
            existingQuote.Status = QuoteStatus.Pending;
        }
        else
        {
            var quote = new Quote
            {
                RfqId = id,
                SellerId = userId,
                ProposedPrice = request.ProposedPrice,
                DeliveryTimeDays = request.DeliveryTimeDays,
                Notes = request.Notes
            };
            _context.Quotes.Add(quote);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Quote submitted successfully" });
    }

    [Authorize]
    [HttpPut("quote/{quoteId}/status")]
    public async Task<IActionResult> UpdateQuoteStatus(int quoteId, [FromBody] UpdateQuoteStatusRequest request)
    {
        var quote = await _context.Quotes.Include(q => q.Rfq).FirstOrDefaultAsync(q => q.Id == quoteId);
        if (quote == null)
            return NotFound(new { message = "Quote not found" });

        if (quote.Rfq == null)
            return BadRequest(new { message = "Associated RFQ not found" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        if (quote.Rfq.BuyerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        quote.Status = request.Status;

        if (request.Status == QuoteStatus.Accepted)
        {
            quote.Rfq.Status = RfqStatus.Closed;
            
            var otherQuotes = await _context.Quotes
                .Where(q => q.RfqId == quote.RfqId && q.Id != quoteId && q.Status == QuoteStatus.Pending)
                .ToListAsync();
            foreach (var oq in otherQuotes)
            {
                oq.Status = QuoteStatus.Rejected;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Quote {request.Status.ToString().ToLower()} successfully" });
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRfq(int id)
    {
        var rfq = await _context.Rfqs.FindAsync(id);
        if (rfq == null)
            return NotFound(new { message = "RFQ not found" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        if (rfq.BuyerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        _context.Rfqs.Remove(rfq);
        await _context.SaveChangesAsync();

        return Ok(new { message = "RFQ deleted successfully" });
    }
}

public class CreateRfqRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = "units";
    public decimal BudgetPrice { get; set; }
    public string ShippingTerms { get; set; } = string.Empty;
    public int ActiveDays { get; set; } = 30;
}

public class SubmitQuoteRequest
{
    public decimal ProposedPrice { get; set; }
    public int DeliveryTimeDays { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class UpdateQuoteStatusRequest
{
    public QuoteStatus Status { get; set; }
}
