using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable
namespace Api.Database.Models
{
    [Owned]
    public class VideoStream : IEquatable<VideoStream>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(200)]
        public string Url { get; set; }

        [MaxLength(64)]
        [Required]
        public string Type { get; set; }

        public bool ShouldRotate270Clockwise { get; set; }

        public bool Equals(VideoStream other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name && Url == other.Url && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((VideoStream)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Url, Type);
        }
    }
}
