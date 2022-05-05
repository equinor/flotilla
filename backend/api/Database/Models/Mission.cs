using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Database.Models
{
    public class Mission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [MaxLength(128)]
        [Required]
        public string Name { get; set; }

        [Url]
        [Required]
        public Uri URL { get; set; }

        [Required]
        public virtual IList<Tag> Tags { get; set; }
    }
}
