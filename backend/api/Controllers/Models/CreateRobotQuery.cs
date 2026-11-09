using System.ComponentModel.DataAnnotations;
using Api.Database.Models;
using Api.Utilities;

namespace Api.Controllers.Models
{
    public struct CreateRobotQuery : IValidatableObject
    {
        public string Name { get; set; }

        public string IsarId { get; set; }

        public RobotType RobotType { get; set; }

        public bool IsAtex { get; set; }

        public float? LowerPressureWarningThreshold { get; set; }

        public float? UpperPressureWarningThreshold { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            AtexPressureValidation.Validate(
                LowerPressureWarningThreshold,
                UpperPressureWarningThreshold
            );

        public string SerialNumber { get; set; }

        public string CurrentInstallationCode { get; set; }

        public IList<CreateDocumentationQuery> Documentation { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public IList<RobotCapabilitiesEnum> RobotCapabilities { get; set; }

        public RobotStatus Status { get; set; }
    }
}
