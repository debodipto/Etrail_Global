using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class Banner
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string ImageUrl { get; set; } = string.Empty;
    
    public string LinkUrl { get; set; } = string.Empty;
    
    public int Position { get; set; } = 1; // 1 = Top banner slider, 2 = Middle banner
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
