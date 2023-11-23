using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public enum RoleAccessLevel
    {
        READ_ONLY,
        USER,
        ADMIN
    }

    public class AccessRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public Installation? Installation { get; set; }

        [Required]
        public string RoleName { get; set; }

        [Required]
        public RoleAccessLevel AccessLevel { get; set; }
    }
}
