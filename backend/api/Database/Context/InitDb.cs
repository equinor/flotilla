using Api.Database.Models;

namespace Api.Database.Context
{
    public static class InitDb
    {
        private const string KaarstoInspectionArea = "K-Lab";
        private const string KaarstoInspectionAreaWithoutRobot = "Area Without Robot";
        private const string NorthernLightsInspectionArea = "Northern Lights Inspection Area";

        // Single-inspection missions avoid grouped SARA analyses.
        private sealed record MissionScenario(
            string MissionName,
            string TagId,
            string Description,
            SensorType SensorType,
            AnalysisType[] AnalysisTypes,
            string Comment,
            float? VideoDuration = null,
            Func<AcousticInspectionMetadata>? AcousticInspectionMetadata = null
        );

        private static readonly MissionScenario[] scenarios =
        [
            new MissionScenario(
                "CLOE - Empty glass",
                "cloe-empty",
                "Constant level oiler with an empty sight glass",
                SensorType.Image,
                [AnalysisType.CLOE],
                "Oil level should read at or near 0 %, which is below the alert bound and should raise an alert in SARA."
            ),
            new MissionScenario(
                "CLOE - Low level",
                "cloe-low",
                "Constant level oiler with a low oil level",
                SensorType.Image,
                [AnalysisType.CLOE],
                "Oil level should land between the warning and alert bounds and should raise a warning in SARA."
            ),
            new MissionScenario(
                "CLOE - Normal level",
                "cloe-normal",
                "Constant level oiler with a normal oil level",
                SensorType.Image,
                [AnalysisType.CLOE],
                "Oil level should read above the warning bound and should not raise anything in SARA."
            ),
            new MissionScenario(
                "CLOE - Rain drops on lens",
                "cloe-rain-drops",
                "Constant level oiler seen through a lens covered in rain drops",
                SensorType.Image,
                [AnalysisType.CLOE],
                "Verifies that CLOE behaves sensibly when the image is degraded rather than simply absent."
            ),
            new MissionScenario(
                "Fencilla - Intact fence",
                "fence-intact",
                "Fence in good condition",
                SensorType.Image,
                [AnalysisType.Fencilla],
                "Fencilla should report no break, so nothing should be raised in SARA."
            ),
            new MissionScenario(
                "Fencilla - Fence with hole",
                "fence-hole",
                "Fence with a hole in it",
                SensorType.Image,
                [AnalysisType.Fencilla],
                "Fencilla should report a break, which should raise an alert in SARA."
            ),
            new MissionScenario(
                "Fencilla - Rain drops on lens",
                "fence-rain-drops",
                "Fence seen through a lens covered in rain drops",
                SensorType.Image,
                [AnalysisType.Fencilla],
                "Verifies that Fencilla behaves sensibly when the image is degraded rather than simply absent."
            ),
            new MissionScenario(
                "Thermal reading - Normal temperature",
                "thermal-normal",
                "Thermal reading of equipment at a normal temperature",
                SensorType.ThermalImage,
                [AnalysisType.ThermalReading],
                "Verifies that a thermal reading is produced and stored. Requires a reference polygon seeded in SARA for this tag."
            ),
            new MissionScenario(
                "Thermal reading - Hot spot",
                "thermal-hot-spot",
                "Thermal reading of equipment with a hot spot",
                SensorType.ThermalImage,
                [AnalysisType.ThermalReading],
                "Verifies that an elevated thermal reading is produced. Note that SARA has no thermal bounds configured, so this cannot raise an alarm yet."
            ),
            new MissionScenario(
                "CO2 measurement",
                "co2-measurement",
                "CO2 measurement",
                SensorType.CO2Measurement,
                [AnalysisType.CO2],
                "Verifies that a CO2 measurement arrives and is stored as a time series. CO2 has no analyzer and cannot raise an alarm."
            ),
            new MissionScenario(
                "Image without analysis",
                "image-no-analysis",
                "Image taken without any analysis requested",
                SensorType.Image,
                [],
                "Verifies that an image is stored and displayed when no analysis is requested."
            ),
            new MissionScenario(
                "Failed task",
                "task-failure",
                "Image inspection that deliberately fails in isar-robot",
                SensorType.Image,
                [],
                "Expected to fail. The task-failure tag makes isar-robot fail the task rather than an analysis or upload."
            ),
            new MissionScenario(
                "Video inspection",
                "video",
                "Video recording",
                SensorType.Video,
                [],
                "Verifies that a video arrives and can be played back.",
                VideoDuration: 10f
            ),
            new MissionScenario(
                "Thermal video inspection",
                "thermal-video",
                "Thermal video recording",
                SensorType.ThermalVideo,
                [],
                "Verifies that a thermal video arrives and can be played back.",
                VideoDuration: 10f
            ),
            new MissionScenario(
                "Acoustic measurement",
                "acoustic",
                "Acoustic leak measurement",
                SensorType.AcousticMeasurement,
                [],
                "Verifies that an acoustic measurement and its metadata arrive.",
                AcousticInspectionMetadata: () =>
                    new AcousticInspectionMetadata(
                        frequencyFrom: 20_000f,
                        frequencyTo: 60_000f,
                        snrValueThreshold: 3f,
                        detectionType: AcousticDetectionType.Leak
                    )
            ),
            new MissionScenario(
                "Audio recording",
                "audio",
                "Audio recording",
                SensorType.Audio,
                [],
                "Verifies that an audio recording arrives and can be played back.",
                VideoDuration: 10f
            ),
        ];

        private static readonly MissionScenario inspectionAreaGuardScenario = new(
            "Blocked - Mission in area without robot",
            "blocked-area",
            "Image in an inspection area that holds no robot",
            SensorType.Image,
            [],
            "Expected to be rejected. Verifies that a mission cannot be started in an inspection area the robot is not in."
        );

        private static Plant CreatePlant(string installationCode, string name)
        {
            return new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Installation = new Installation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    InstallationCode = installationCode,
                },
                Name = name,
                PlantCode = installationCode,
            };
        }

        private static InspectionArea CreateInspectionArea(Plant plant, string name)
        {
            return new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plant,
                Installation = plant.Installation,
                Name = name,
            };
        }

        private static List<AccessRole> GetAccessRoles(List<Installation> installations)
        {
            return
            [
                .. installations.SelectMany(installation =>
                    new[]
                    {
                        new AccessRole
                        {
                            Installation = installation,
                            AccessLevel = RoleAccessLevel.USER,
                            RoleName = $"Role.User.{installation.InstallationCode}",
                        },
                        new AccessRole
                        {
                            Installation = installation,
                            AccessLevel = RoleAccessLevel.READ_ONLY,
                            RoleName = $"Role.ReadOnly.{installation.InstallationCode}",
                        },
                    }
                ),
            ];
        }

        private static List<MissionDefinition> GetMissionDefinitions(
            List<InspectionArea> inspectionAreas
        )
        {
            var areasWithRobot = inspectionAreas.Where(area =>
                area.Name != KaarstoInspectionAreaWithoutRobot
            );

            var definitions = areasWithRobot
                .SelectMany(area => scenarios.Select(scenario => CreateMission(scenario, area)))
                .ToList();

            definitions.AddRange(areasWithRobot.Select(CreatePartiallySuccessfulMission));
            definitions.AddRange(areasWithRobot.Select(CreateAllTasksFailedMission));
            definitions.Add(
                CreateMission(
                    inspectionAreaGuardScenario,
                    inspectionAreas.Single(area => area.Name == KaarstoInspectionAreaWithoutRobot)
                )
            );
            definitions.Add(
                CreateMixedInspectionMission(
                    inspectionAreas.Single(area => area.Name == KaarstoInspectionArea)
                )
            );

            return definitions;
        }

        private static MissionDefinition CreateAllTasksFailedMission(InspectionArea inspectionArea)
        {
            var scenario = scenarios.Single(scenario => scenario.TagId == "task-failure");

            return new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "All tasks failed",
                InstallationCode = inspectionArea.Installation.InstallationCode,
                InspectionArea = inspectionArea,
                Comment =
                    "Expected to fail with isar-robot: both image inspection tasks fail. "
                    + "Verifies that a mission fails when none of its tasks succeed.",
                LastSuccessfulRun = null,
                Tasks = [CreateTask(scenario, 1), CreateTask(scenario, 2)],
            };
        }

        private static MissionDefinition CreatePartiallySuccessfulMission(
            InspectionArea inspectionArea
        )
        {
            string[] tags =
            [
                "image-no-analysis",
                "task-failure",
                "image-no-analysis",
                "task-failure",
            ];

            return new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Partially successful mission",
                InstallationCode = inspectionArea.Installation.InstallationCode,
                InspectionArea = inspectionArea,
                Comment =
                    "Expected to be partially successful with isar-robot: tasks 1 and 3 succeed, "
                    + "and tasks 2 and 4 fail. Verifies that execution continues after a failed task.",
                LastSuccessfulRun = null,
                Tasks = tags.Select(
                        (tag, index) =>
                            CreateTask(
                                scenarios.Single(scenario => scenario.TagId == tag),
                                index + 1
                            )
                    )
                    .ToList(),
            };
        }

        private static MissionDefinition CreateMixedInspectionMission(InspectionArea inspectionArea)
        {
            string[] tags =
            [
                "image-no-analysis",
                "cloe-normal",
                "thermal-normal",
                "fence-intact",
                "cloe-low",
                "image-no-analysis",
                "fence-hole",
                "thermal-hot-spot",
                "cloe-empty",
                "video",
                "cloe-rain-drops",
                "fence-rain-drops",
                "co2-measurement",
                "thermal-normal",
                "fence-hole",
                "image-no-analysis",
            ];

            return new MissionDefinition
            {
                Id = "97c0c59d-64ab-4f7b-b89e-51ae2b61b070",
                Name = "Mixed inspections",
                InstallationCode = inspectionArea.Installation.InstallationCode,
                InspectionArea = inspectionArea,
                Comment =
                    "A varied inspection round combining oil level, fence, thermal, image, video and CO2 inspections, "
                    + "including repeated observations.",
                LastSuccessfulRun = null,
                Tasks = tags.Select(
                        (tag, index) =>
                            CreateTask(
                                scenarios.Single(scenario => scenario.TagId == tag),
                                index + 1
                            )
                    )
                    .ToList(),
            };
        }

        private static TaskDefinition CreateTask(MissionScenario scenario, int index)
        {
            return new TaskDefinition
            {
                Index = index,
                TagId = scenario.TagId,
                Description = scenario.Description,
                RobotPose = new Pose(),
                TargetPosition = new Position(),
                SensorType = scenario.SensorType,
                AnalysisTypes = [.. scenario.AnalysisTypes],
                VideoDuration = scenario.VideoDuration,
                AcousticInspectionMetadata = scenario.AcousticInspectionMetadata?.Invoke(),
            };
        }

        private static MissionDefinition CreateMission(
            MissionScenario scenario,
            InspectionArea inspectionArea
        )
        {
            return new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = scenario.MissionName,
                InstallationCode = inspectionArea.Installation.InstallationCode,
                InspectionArea = inspectionArea,
                Comment = scenario.Comment,
                LastSuccessfulRun = null,
                Tasks = [CreateTask(scenario, 1)],
            };
        }

        public static void PopulateDb(FlotillaDbContext context)
        {
            if (context.Installations.Any())
            {
                return;
            }

            var kaarsto = CreatePlant("KAA", "Kårstø");
            var northernLights = CreatePlant("NLS", "Northern Lights");
            var klab = CreateInspectionArea(kaarsto, KaarstoInspectionArea);
            List<InspectionArea> inspectionAreas =
            [
                klab,
                CreateInspectionArea(kaarsto, KaarstoInspectionAreaWithoutRobot),
                CreateInspectionArea(northernLights, NorthernLightsInspectionArea),
            ];

            context.AddRange(inspectionAreas);
            context.AddRange(GetAccessRoles([kaarsto.Installation, northernLights.Installation]));
            context.AddRange(GetMissionDefinitions(inspectionAreas));
            context.Robots.Add(
                new Robot
                {
                    Id = Guid.NewGuid().ToString(),
                    IsarId = "00000000-0000-0000-0000-000000000000",
                    Name = "Placebot",
                    SerialNumber = "0001",
                    CurrentInstallation = kaarsto.Installation,
                    CurrentInspectionAreaId = klab.Id,
                    Status = RobotStatus.Offline,
                    Host = "localhost",
                    Port = 3000,
                    RobotCapabilities = [],
                }
            );

            context.SaveChanges();
            context.ChangeTracker.Clear();
        }
    }
}
