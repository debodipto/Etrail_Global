using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public double DiscountPercentage { get; set; } = 0.0;

    public string BannerUrl { get; set; } = string.Empty;
}
