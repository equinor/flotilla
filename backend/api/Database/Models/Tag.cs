using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Models;

#nullable disable
namespace Database.Models
{
    public class Tag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [MaxLength(128)]
        [Required]
        public string TagId { get; set; }

        [Url]
        [Required]
        public Uri URL { get; set; }

        [Required]
        public virtual IList<InspectionType> InspectionTypes { get; set; }
    }
}
