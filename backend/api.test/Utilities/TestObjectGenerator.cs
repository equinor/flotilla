using System;
using System.Collections.Generic;
using Api.Database.Models;

namespace Api.Test.Utilities;

public static class TestObjectGenerator
{
    public static Installation NewInstallation(
        string? id = null,
        string name = "TestInst",
        string installationCode = "TestCode"
    )
    {
        return new Installation
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name,
            InstallationCode = installationCode,
        };
    }

    public static Plant NewPlant(
        Installation installation,
        string? id = null,
        string name = "TestPlant",
        string plantCode = "TestCode"
    )
    {
        return new Plant
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Installation = installation,
            PlantCode = plantCode,
            Name = name,
        };
    }

    public static InspectionArea NewInspectionArea(
        Installation installation,
        Plant plant,
        string? id = null,
        string name = "TestName"
    )
    {
        return new InspectionArea
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Plant = plant,
            Installation = installation,
            Name = name,
        };
    }

    public static Robot NewRobot(
        Installation currentInstallation,
        string currentInspectionAreaId,
        RobotModel? robotModel = null,
        RobotStatus robotStatus = RobotStatus.Available,
        string name = "Test Robot",
        string? id = null,
        string? isarId = null,
        string serialNumber = "TestSerialNumber",
        float batteryLevel = 100,
        BatteryState batteryState = BatteryState.Normal,
        float pressureLevel = 90,
        string host = "localhost",
        int port = 8080,
        IList<RobotCapabilitiesEnum>? capabilities = null,
        bool isarConnected = true,
        bool deprecated = false,
        bool missionQueueFrozen = false,
        RobotFlotillaStatus flotillaStatus = RobotFlotillaStatus.Normal,
        Pose? pose = null,
        string? currentMissionId = null
    )
    {
        return new Robot
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name,
            IsarId = isarId ?? Guid.NewGuid().ToString(),
            Model = robotModel ?? new RobotModel(),
            SerialNumber = serialNumber,
            CurrentInstallation = currentInstallation,
            CurrentInspectionAreaId = currentInspectionAreaId,
            BatteryLevel = batteryLevel,
            BatteryState = batteryState,
            PressureLevel = pressureLevel,
            Host = host,
            Port = port,
            RobotCapabilities = capabilities ?? [],
            IsarConnected = isarConnected,
            Deprecated = deprecated,
            MissionQueueFrozen = missionQueueFrozen,
            Status = robotStatus,
            FlotillaStatus = flotillaStatus,
            Pose = pose ?? new Pose(),
            CurrentMissionId = currentMissionId,
        };
    }

    public static MissionRun NewMissionRun(
        string installationCode,
        Robot robot,
        InspectionArea inspectionArea,
        string name = "TestName",
        string? id = null,
        string? missionId = null,
        MissionStatus missionStatus = MissionStatus.Pending,
        DateTime? startTime = null,
        IList<MissionTask>? tasks = null,
        MissionRunType missionRunType = MissionRunType.Normal,
        string? isarMissionId = null,
        bool isDeprecated = false
    )
    {
        return new MissionRun
        {
            Id = id ?? Guid.NewGuid().ToString(),
            MissionId = missionId ?? Guid.NewGuid().ToString(),
            Status = missionStatus,
            InstallationCode = installationCode,
            DesiredStartTime = startTime ?? DateTime.UtcNow,
            Robot = robot,
            Tasks = tasks ?? [],
            MissionRunType = missionRunType,
            IsarMissionId = isarMissionId ?? Guid.NewGuid().ToString(),
            InspectionArea = inspectionArea,
            Name = name,
            IsDeprecated = isDeprecated,
        };
    }
}
