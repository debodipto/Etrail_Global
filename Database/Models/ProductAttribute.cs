using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class ProductAttribute
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // JSON array of available values, e.g., ["16GB", "32GB"]
    public string ValuesJson { get; set; } = "[]";
}
