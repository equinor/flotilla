using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Plant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public virtual Installation Installation { get; set; }

        [Required]
        [MaxLength(10)]
        public string PlantCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
    }
}
