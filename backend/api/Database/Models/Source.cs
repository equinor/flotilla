using System.ComponentModel.DataAnnotations;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Source
    {
        [Key]
        [Required]
        [MaxLength(200)]
        public string Id { get; set; }

        [Required]
        public string URL { get; set; }

        [Required]
        public MissionSourceType Type { get; set; }
    }

    public enum MissionSourceType
    {
        Echo, Custom
    }
}
