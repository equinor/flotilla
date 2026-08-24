using System.ComponentModel.DataAnnotations;

namespace Api.Controllers.Models
{
    public struct FeedbackQuery
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(50)]
        public string Email { get; set; }

        [Required]
        [MaxLength(10)]
        public string ShortName { get; set; }

        [Required]
        [MaxLength(3000)]
        public string Description { get; set; }
        public required FeedbackType Type { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(200)]
        public string Url { get; set; }

        public override string ToString()
        {
            return string.Join(
                Environment.NewLine,
                $"Type: {Type}",
                $"Title: {Title}",
                $"Description: {Description}",
                $"Email: {Email}",
                $"Short name: {ShortName}",
                $"Timestamp: {Timestamp}",
                $"Url: {Url}"
            );
        }
    }

    public enum FeedbackType
    {
        BugReport,
        FeatureRequest,
        Other,
    }
}
