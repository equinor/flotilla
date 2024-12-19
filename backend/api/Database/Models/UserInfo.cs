using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class UserInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public string Oid { get; set; }
    }
}
