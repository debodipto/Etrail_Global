using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class SmartBarSetting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string AnnouncementText { get; set; } = string.Empty;

    [MaxLength(20)]
    public string BgColor { get; set; } = "#146c43";

    [MaxLength(20)]
    public string TextColor { get; set; } = "#ffffff";

    public string Link { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
