using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Database.Context
{
    public static class InitDb
    {
        private static readonly List<Inspection> inspections = GetInspections();
        private static readonly List<Installation> installations = GetInstallations();
        private static readonly List<Plant> plants = GetPlants();
        private static readonly List<InspectionArea> inspectionAreas = GetInspectionAreas();
        private static readonly List<Robot> robots = GetRobots();
        private static readonly List<Source> sources = GetSources();
        private static readonly List<MissionTask> tasks = GetMissionTasks();
        private static readonly List<MissionDefinition> missionDefinitions =
            GetMissionDefinitions();
        private static readonly List<MissionRun> missionRuns = GetMissionRuns();
        private static readonly List<AccessRole> accessRoles = GetAccessRoles();

        private static List<Inspection> GetInspections()
        {
            return new List<Inspection>([]);
        }

        private static List<AccessRole> GetAccessRoles()
        {
            var userAccessRole = new AccessRole
            {
                Installation = installations[0],
                AccessLevel = RoleAccessLevel.ADMIN,
                RoleName = "Role.User.HUA",
            };

            var readOnlyAccessRole = new AccessRole
            {
                Installation = installations[0],
                AccessLevel = RoleAccessLevel.READ_ONLY,
                RoleName = "Role.ReadOnly.HUA",
            };

            return new List<AccessRole>([userAccessRole, readOnlyAccessRole]);
        }

        private static List<Installation> GetInstallations()
        {
            var installation1 = new Installation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "JARVIS - Sintef lab",
                InstallationCode = "JAR",
            };

            return new List<Installation>([installation1]);
        }

        private static List<Plant> GetPlants()
        {
            var plant1 = new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                Name = "JARVIS",
                PlantCode = "JAR",
            };

            return new List<Plant>([plant1]);
        }

        private static List<InspectionArea> GetInspectionAreas()
        {
            var inspectionAreaSintefLab = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                Name = "Sintef Lab",
            };

            return new List<InspectionArea>([
                inspectionAreaSintefLab,
            ]);
        }

        private static List<Source> GetSources()
        {
            var source1 = new Source { SourceId = "1", CustomMissionTasks = "[{\"TaskOrder\":0,\"TagId\":\"(3,0)\",\"RobotPose\":{\"Position\":{\"X\":3,\"Y\":0,\"Z\":0},\"Orientation\":{\"X\":0,\"Y\":0,\"Z\":0,\"W\":1}},\"Status\":2,\"Inspection\":{\"InspectionTarget\":{\"X\":3,\"Y\":1,\"Z\":0},\"InspectionType\":0}},{\"TaskOrder\":1,\"TagId\":\"(-1,-1)\",\"RobotPose\":{\"Position\":{\"X\":-1,\"Y\":-1,\"Z\":0},\"Orientation\":{\"X\":0,\"Y\":0,\"Z\":0,\"W\":1}},\"Status\":2,\"Inspection\":{\"InspectionTarget\":{\"X\":-1,\"Y\":-2,\"Z\":0},\"InspectionType\":0}}]" };

            return new List<Source>([source1]);
        }

        private static List<Robot> GetRobots()
        {
            var pantherRobot = new Robot
            {
                Name = "Panther",
                IsarId = "00000000-0000-0000-0000-000000000000",
                SerialNumber = "Panther jarvis",
                Status = RobotStatus.Available,
                Type = RobotType.Robot,
                Host = "localhost",
                Port = 3000,
                CurrentInstallation = installations[0],
                CurrentInspectionAreaId = inspectionAreas[0].Id,
                Documentation = [],
            };

            return new List<Robot>([pantherRobot]);
        }

        private static List<MissionDefinition> GetMissionDefinitions()
        {
            var missionDefinition1 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default mission",
                InstallationCode = inspectionAreas[0].Installation!.InstallationCode,
                InspectionArea = inspectionAreas[0],
                Source = sources[0],
                Comment = "Interesting comment",
                LastSuccessfulRun = null,
            };

            return new List<MissionDefinition>([
                missionDefinition1
            ]);
        }

        private static List<MissionTask> GetMissionTasks()
        {
            return new List<MissionTask>([]);
        }

        private static List<MissionRun> GetMissionRuns()
        {

            return new List<MissionRun>([]);
        }

        public static void PopulateDb(FlotillaDbContext context)
        {
            // To make sure we are not trying to initialize database more than once during tests
            if (context.Robots.Any())
            {
                return;
            }

            context.AddRange(inspections);
            context.AddRange(installations);
            context.SaveChanges();

            context.AddRange(robots);
            context.AddRange(plants);
            context.AddRange(inspectionAreas);
            context.AddRange(sources);

            context.AddRange(tasks);
            context.AddRange(missionDefinitions);
            context.AddRange(missionRuns);
            context.AddRange(accessRoles);

            context.SaveChanges();
            context.ChangeTracker.Clear();
        }
    }
}
