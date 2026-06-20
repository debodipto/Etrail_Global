using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EtrailGlobal.Database;
using EtrailGlobal.Database.Models;

namespace EtrailGlobal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("banners")]
    public async Task<IActionResult> GetBanners()
    {
        var banners = await _context.Banners
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(banners);
    }

    [HttpGet("hot-categories")]
    public async Task<IActionResult> GetHotCategories()
    {
        var cats = await _context.HotCategories
            .Where(c => c.IsActive)
            .ToListAsync();
        return Ok(cats);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("banners")]
    public async Task<IActionResult> AddBanner([FromBody] CreateBannerRequest request)
    {
        var banner = new Banner
        {
            Title = request.Title,
            ImageUrl = request.ImageUrl,
            LinkUrl = request.LinkUrl,
            Position = request.Position,
            IsActive = true
        };
        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();
        return Ok(banner);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("banners/{id}")]
    public async Task<IActionResult> DeleteBanner(int id)
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner == null) return NotFound();
        _context.Banners.Remove(banner);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Banner deleted successfully" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("hot-categories")]
    public async Task<IActionResult> AddHotCategory([FromBody] CreateHotCatRequest request)
    {
        var cat = new HotCategory
        {
            Name = request.Name,
            ImageUrl = request.ImageUrl,
            IsActive = true
        };
        _context.HotCategories.Add(cat);
        await _context.SaveChangesAsync();
        return Ok(cat);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("hot-categories/{id}")]
    public async Task<IActionResult> DeleteHotCategory(int id)
    {
        var cat = await _context.HotCategories.FindAsync(id);
        if (cat == null) return NotFound();
        _context.HotCategories.Remove(cat);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Hot category deleted successfully" });
    }
}

public class CreateBannerRequest
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class CreateHotCatRequest
{
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
