#nullable disable
using System.ComponentModel.DataAnnotations;
using Api.Services.Models;

namespace Api.Database.Models
{
    public class TagInspectionMetadata
    {
        [Key]
        public string TagId { get; set; }

        public IsarZoomDescription ZoomDescription { get; set; }
    }
}
