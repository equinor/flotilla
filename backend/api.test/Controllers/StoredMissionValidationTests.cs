using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services.Models;
using Xunit;

namespace Api.Test.Controllers
{
    public class StoredMissionValidationTests
    {
        [Theory]
        [InlineData("", "area", false)]
        [InlineData("DiagnosticRoutine", null, false)]
        [InlineData("DiagnosticRoutine", "area", true)]
        public void StoredDefinitionRejectsInvalidConfiguration(
            string reference,
            string? area,
            bool mixed
        )
        {
            object query = new CreateMissionQuery
            {
                Name = "Diagnostics",
                InstallationCode = "TEST",
                RobotMissionId = reference,
                InspectionAreaId = area,
                Tasks = mixed ? [new TaskQuery()] : [],
            };
            Assert.False(
                Validator.TryValidateObject(query, new ValidationContext(query), [], true)
            );
        }

        [Fact]
        public void StoredDefinitionCannotContainInspectionTasks()
        {
            var definition = new MissionDefinition
            {
                RobotMissionId = "DiagnosticRoutine",
                Tasks = [],
            };
            definition.ValidateTasks();
            definition.Tasks = [new TaskDefinition()];
            Assert.Throws<ArgumentException>(definition.ValidateTasks);
        }

        [Fact]
        public void OrdinaryTaskRequestOmitsStoredReference()
        {
            var mission = new MissionRun
            {
                Tasks =
                [
                    new MissionTask { RobotPose = new Pose(), Status = TaskStatus.NotStarted },
                ],
            };
            using var json = JsonDocument.Parse(
                JsonSerializer.Serialize(new IsarMissionDefinition(mission))
            );
            Assert.False(json.RootElement.TryGetProperty("robot_mission_id", out _));
            Assert.Single(json.RootElement.GetProperty("tasks").EnumerateArray());
        }
    }
}
