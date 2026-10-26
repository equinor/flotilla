using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Api.Controllers.Models;
using Api.Database.Models;
using Xunit;

namespace Api.Test.Controllers
{
    public class StoredMissionValidationTests
    {
        [Theory]
        [InlineData(null, null, false, false)]
        [InlineData("", "area", false, false)]
        [InlineData(" ", "area", false, false)]
        [InlineData("DiagnosticRoutine", null, false, false)]
        [InlineData("DiagnosticRoutine", "area", true, false)]
        [InlineData("DiagnosticRoutine", "area", false, true)]
        [InlineData(null, null, true, true)]
        public void MissionQueryRequiresEitherInspectionTasksOrStoredReference(
            string? reference,
            string? area,
            bool hasTasks,
            bool valid
        )
        {
            object query = new CreateMissionQuery
            {
                Name = "Mission",
                InstallationCode = "TEST",
                RobotMissionId = reference,
                InspectionAreaId = area,
                Tasks = hasTasks ? [new TaskQuery()] : [],
            };
            var results = new List<ValidationResult>();
            Assert.Equal(
                valid,
                Validator.TryValidateObject(query, new ValidationContext(query), results, true)
            );
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData("", false, false)]
        [InlineData(" ", false, false)]
        [InlineData("DiagnosticRoutine", true, false)]
        [InlineData("DiagnosticRoutine", false, true)]
        [InlineData(null, true, true)]
        public void DefinitionPersistenceEnforcesMissionKind(
            string? reference,
            bool hasTasks,
            bool valid
        )
        {
            var definition = new MissionDefinition
            {
                RobotMissionId = reference,
                Tasks = hasTasks ? [new TaskDefinition()] : [],
            };
            if (valid)
                definition.ValidateTasks();
            else
                Assert.Throws<System.ArgumentException>(definition.ValidateTasks);
        }
    }
}
