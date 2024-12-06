#nullable disable
using System.ComponentModel.DataAnnotations;
using Api.Services.Models;

namespace Api.Services.MissionLoaders
{
    public class EchoTagInspectionMetadata
    {
        [Key]
        public string TagId { get; set; }

        public IsarZoomDescription ZoomDescription { get; set; }
    }
}
