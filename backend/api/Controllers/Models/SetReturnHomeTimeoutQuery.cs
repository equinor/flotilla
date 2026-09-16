using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Controllers.Models
{
    public class SetReturnHomeTimeoutQuery
    {
        [Required]
        [Range(1, int.MaxValue)]
        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public int Seconds { get; set; }
    }
}
