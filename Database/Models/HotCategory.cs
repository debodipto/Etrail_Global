using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class HotCategory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string ImageUrl { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
}
