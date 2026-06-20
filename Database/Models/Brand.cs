using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class Brand
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string LogoUrl { get; set; } = string.Empty;
}
