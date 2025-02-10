﻿// <auto-generated />
using System;
using Api.Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    [DbContext(typeof(FlotillaDbContext))]
    [Migration("20250312132108_RemoveArea")]
    partial class RemoveArea
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Api.Database.Models.AccessRole", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("AccessLevel")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("InstallationId")
                        .HasColumnType("text");

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("InstallationId");

                    b.ToTable("AccessRoles");
                });

            modelBuilder.Entity("Api.Database.Models.Inspection", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("AnalysisType")
                        .HasColumnType("text");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("InspectionTargetName")
                        .HasColumnType("text");

                    b.Property<string>("InspectionType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("InspectionUrl")
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<string>("IsarInspectionId")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("IsarTaskId")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime?>("StartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float?>("VideoDuration")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.ToTable("Inspections");
                });

            modelBuilder.Entity("Api.Database.Models.InspectionArea", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("AreaPolygonJson")
                        .HasColumnType("text");

                    b.Property<string>("InstallationId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("PlantId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("InstallationId");

                    b.HasIndex("PlantId");

                    b.ToTable("InspectionAreas");
                });

            modelBuilder.Entity("Api.Database.Models.Installation", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("InstallationCode")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.HasKey("Id");

                    b.HasIndex("InstallationCode")
                        .IsUnique();

                    b.ToTable("Installations");
                });

            modelBuilder.Entity("Api.Database.Models.MissionDefinition", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("Comment")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<string>("InspectionAreaId")
                        .HasColumnType("text");

                    b.Property<long?>("InspectionFrequency")
                        .HasColumnType("bigint");

                    b.Property<string>("InstallationCode")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsDeprecated")
                        .HasColumnType("boolean");

                    b.Property<string>("LastSuccessfulRunId")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("SourceId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("InspectionAreaId");

                    b.HasIndex("LastSuccessfulRunId");

                    b.HasIndex("SourceId");

                    b.ToTable("MissionDefinitions");
                });

            modelBuilder.Entity("Api.Database.Models.MissionRun", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("Comment")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<string>("Description")
                        .HasMaxLength(450)
                        .HasColumnType("character varying(450)");

                    b.Property<DateTime>("DesiredStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long?>("EstimatedTaskDuration")
                        .HasColumnType("bigint");

                    b.Property<string>("InspectionAreaId")
                        .HasColumnType("text");

                    b.Property<string>("InstallationCode")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("IsDeprecated")
                        .HasColumnType("boolean");

                    b.Property<string>("IsarMissionId")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("MissionId")
                        .HasColumnType("text");

                    b.Property<string>("MissionRunType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("RobotId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("StartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("StatusReason")
                        .HasMaxLength(450)
                        .HasColumnType("character varying(450)");

                    b.HasKey("Id");

                    b.HasIndex("InspectionAreaId");

                    b.HasIndex("RobotId");

                    b.ToTable("MissionRuns");
                });

            modelBuilder.Entity("Api.Database.Models.MissionTask", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("InspectionId")
                        .HasColumnType("text");

                    b.Property<string>("IsarTaskId")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("MissionRunId")
                        .HasColumnType("text");

                    b.Property<int?>("PoseId")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("StartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TagId")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("TagLink")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<int>("TaskOrder")
                        .HasColumnType("integer");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("InspectionId");

                    b.HasIndex("MissionRunId");

                    b.ToTable("MissionTasks");
                });

            modelBuilder.Entity("Api.Database.Models.Plant", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("InstallationId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("PlantCode")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.HasKey("Id");

                    b.HasIndex("InstallationId");

                    b.HasIndex("PlantCode")
                        .IsUnique();

                    b.ToTable("Plants");
                });

            modelBuilder.Entity("Api.Database.Models.Robot", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<float>("BatteryLevel")
                        .HasColumnType("real");

                    b.Property<string>("BatteryState")
                        .HasColumnType("text");

                    b.Property<string>("CurrentInspectionAreaId")
                        .HasColumnType("text");

                    b.Property<string>("CurrentInstallationId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("CurrentMissionId")
                        .HasColumnType("text");

                    b.Property<bool>("Deprecated")
                        .HasColumnType("boolean");

                    b.Property<string>("FlotillaStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Host")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("IsarConnected")
                        .HasColumnType("boolean");

                    b.Property<string>("IsarId")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("MissionQueueFrozen")
                        .HasColumnType("boolean");

                    b.Property<string>("ModelId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<int>("Port")
                        .HasColumnType("integer");

                    b.Property<float?>("PressureLevel")
                        .HasColumnType("real");

                    b.Property<string>("RobotCapabilities")
                        .HasColumnType("text");

                    b.Property<string>("SerialNumber")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CurrentInspectionAreaId");

                    b.HasIndex("CurrentInstallationId");

                    b.HasIndex("ModelId");

                    b.ToTable("Robots");
                });

            modelBuilder.Entity("Api.Database.Models.RobotModel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<float?>("AverageDurationPerTag")
                        .HasColumnType("real");

                    b.Property<float?>("BatteryMissionStartThreshold")
                        .HasColumnType("real");

                    b.Property<float?>("BatteryWarningThreshold")
                        .HasColumnType("real");

                    b.Property<float?>("LowerPressureWarningThreshold")
                        .HasColumnType("real");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float?>("UpperPressureWarningThreshold")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.HasIndex("Type")
                        .IsUnique();

                    b.ToTable("RobotModels");
                });

            modelBuilder.Entity("Api.Database.Models.Source", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("CustomMissionTasks")
                        .HasColumnType("text");

                    b.Property<string>("SourceId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Sources");
                });

            modelBuilder.Entity("Api.Database.Models.UserInfo", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("Oid")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("UserInfos");
                });

            modelBuilder.Entity("Api.Services.MissionLoaders.TagInspectionMetadata", b =>
                {
                    b.Property<string>("TagId")
                        .HasColumnType("text");

                    b.HasKey("TagId");

                    b.ToTable("TagInspectionMetadata");
                });

            modelBuilder.Entity("Api.Database.Models.AccessRole", b =>
                {
                    b.HasOne("Api.Database.Models.Installation", "Installation")
                        .WithMany()
                        .HasForeignKey("InstallationId");

                    b.Navigation("Installation");
                });

            modelBuilder.Entity("Api.Database.Models.Inspection", b =>
                {
                    b.OwnsOne("Api.Database.Models.Position", "InspectionTarget", b1 =>
                        {
                            b1.Property<string>("InspectionId")
                                .HasColumnType("text");

                            b1.Property<float>("X")
                                .HasColumnType("real");

                            b1.Property<float>("Y")
                                .HasColumnType("real");

                            b1.Property<float>("Z")
                                .HasColumnType("real");

                            b1.HasKey("InspectionId");

                            b1.ToTable("Inspections");

                            b1.WithOwner()
                                .HasForeignKey("InspectionId");
                        });

                    b.Navigation("InspectionTarget")
                        .IsRequired();
                });

            modelBuilder.Entity("Api.Database.Models.InspectionArea", b =>
                {
                    b.HasOne("Api.Database.Models.Installation", "Installation")
                        .WithMany()
                        .HasForeignKey("InstallationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Api.Database.Models.Plant", "Plant")
                        .WithMany()
                        .HasForeignKey("PlantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Installation");

                    b.Navigation("Plant");
                });

            modelBuilder.Entity("Api.Database.Models.MissionDefinition", b =>
                {
                    b.HasOne("Api.Database.Models.InspectionArea", "InspectionArea")
                        .WithMany()
                        .HasForeignKey("InspectionAreaId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Api.Database.Models.MissionRun", "LastSuccessfulRun")
                        .WithMany()
                        .HasForeignKey("LastSuccessfulRunId");

                    b.HasOne("Api.Database.Models.Source", "Source")
                        .WithMany()
                        .HasForeignKey("SourceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("Api.Database.Models.AutoScheduleFrequency", "AutoScheduleFrequency", b1 =>
                        {
                            b1.Property<string>("MissionDefinitionId")
                                .HasColumnType("text");

                            b1.Property<int[]>("DaysOfWeek")
                                .IsRequired()
                                .HasColumnType("integer[]");

                            b1.Property<TimeOnly[]>("TimesOfDay")
                                .IsRequired()
                                .HasColumnType("time without time zone[]");

                            b1.HasKey("MissionDefinitionId");

                            b1.ToTable("MissionDefinitions");

                            b1.WithOwner()
                                .HasForeignKey("MissionDefinitionId");
                        });

                    b.OwnsOne("Api.Database.Models.MapMetadata", "Map", b1 =>
                        {
                            b1.Property<string>("MissionDefinitionId")
                                .HasColumnType("text");

                            b1.Property<string>("MapName")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("character varying(200)");

                            b1.HasKey("MissionDefinitionId");

                            b1.ToTable("MissionDefinitions");

                            b1.WithOwner()
                                .HasForeignKey("MissionDefinitionId");

                            b1.OwnsOne("Api.Database.Models.Boundary", "Boundary", b2 =>
                                {
                                    b2.Property<string>("MapMetadataMissionDefinitionId")
                                        .HasColumnType("text");

                                    b2.Property<double>("X1")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("X2")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("Y1")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("Y2")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("Z1")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("Z2")
                                        .HasColumnType("double precision");

                                    b2.HasKey("MapMetadataMissionDefinitionId");

                                    b2.ToTable("MissionDefinitions");

                                    b2.WithOwner()
                                        .HasForeignKey("MapMetadataMissionDefinitionId");
                                });

                            b1.OwnsOne("Api.Database.Models.TransformationMatrices", "TransformationMatrices", b2 =>
                                {
                                    b2.Property<string>("MapMetadataMissionDefinitionId")
                                        .HasColumnType("text");

                                    b2.Property<double>("C1")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("C2")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("D1")
                                        .HasColumnType("double precision");

                                    b2.Property<double>("D2")
                                        .HasColumnType("double precision");

                                    b2.HasKey("MapMetadataMissionDefinitionId");

                                    b2.ToTable("MissionDefinitions");

                                    b2.WithOwner()
                                        .HasForeignKey("MapMetadataMissionDefinitionId");
                                });

                            b1.Navigation("Boundary")
                                .IsRequired();

                            b1.Navigation("TransformationMatrices")
                                .IsRequired();
                        });

                    b.Navigation("AutoScheduleFrequency");

                    b.Navigation("InspectionArea");

                    b.Navigation("LastSuccessfulRun");

                    b.Navigation("Map");

                    b.Navigation("Source");
                });

            modelBuilder.Entity("Api.Database.Models.MissionRun", b =>
                {
                    b.HasOne("Api.Database.Models.InspectionArea", "InspectionArea")
                        .WithMany()
                        .HasForeignKey("InspectionAreaId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Api.Database.Models.Robot", "Robot")
                        .WithMany()
                        .HasForeignKey("RobotId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("InspectionArea");

                    b.Navigation("Robot");
                });

            modelBuilder.Entity("Api.Database.Models.MissionTask", b =>
                {
                    b.HasOne("Api.Database.Models.Inspection", "Inspection")
                        .WithMany()
                        .HasForeignKey("InspectionId");

                    b.HasOne("Api.Database.Models.MissionRun", null)
                        .WithMany("Tasks")
                        .HasForeignKey("MissionRunId");

                    b.OwnsOne("Api.Services.Models.IsarZoomDescription", "IsarZoomDescription", b1 =>
                        {
                            b1.Property<string>("MissionTaskId")
                                .HasColumnType("text");

                            b1.Property<double>("ObjectHeight")
                                .HasColumnType("double precision")
                                .HasAnnotation("Relational:JsonPropertyName", "objectHeight");

                            b1.Property<double>("ObjectWidth")
                                .HasColumnType("double precision")
                                .HasAnnotation("Relational:JsonPropertyName", "objectWidth");

                            b1.HasKey("MissionTaskId");

                            b1.ToTable("MissionTasks");

                            b1.WithOwner()
                                .HasForeignKey("MissionTaskId");
                        });

                    b.OwnsOne("Api.Database.Models.Pose", "RobotPose", b1 =>
                        {
                            b1.Property<string>("MissionTaskId")
                                .HasColumnType("text");

                            b1.HasKey("MissionTaskId");

                            b1.ToTable("MissionTasks");

                            b1.WithOwner()
                                .HasForeignKey("MissionTaskId");

                            b1.OwnsOne("Api.Database.Models.Orientation", "Orientation", b2 =>
                                {
                                    b2.Property<string>("PoseMissionTaskId")
                                        .HasColumnType("text");

                                    b2.Property<float>("W")
                                        .HasColumnType("real");

                                    b2.Property<float>("X")
                                        .HasColumnType("real");

                                    b2.Property<float>("Y")
                                        .HasColumnType("real");

                                    b2.Property<float>("Z")
                                        .HasColumnType("real");

                                    b2.HasKey("PoseMissionTaskId");

                                    b2.ToTable("MissionTasks");

                                    b2.WithOwner()
                                        .HasForeignKey("PoseMissionTaskId");
                                });

                            b1.OwnsOne("Api.Database.Models.Position", "Position", b2 =>
                                {
                                    b2.Property<string>("PoseMissionTaskId")
                                        .HasColumnType("text");

                                    b2.Property<float>("X")
                                        .HasColumnType("real");

                                    b2.Property<float>("Y")
                                        .HasColumnType("real");

                                    b2.Property<float>("Z")
                                        .HasColumnType("real");

                                    b2.HasKey("PoseMissionTaskId");

                                    b2.ToTable("MissionTasks");

                                    b2.WithOwner()
                                        .HasForeignKey("PoseMissionTaskId");
                                });

                            b1.Navigation("Orientation")
                                .IsRequired();

                            b1.Navigation("Position")
                                .IsRequired();
                        });

                    b.Navigation("Inspection");

                    b.Navigation("IsarZoomDescription");

                    b.Navigation("RobotPose")
                        .IsRequired();
                });

            modelBuilder.Entity("Api.Database.Models.Plant", b =>
                {
                    b.HasOne("Api.Database.Models.Installation", "Installation")
                        .WithMany()
                        .HasForeignKey("InstallationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Installation");
                });

            modelBuilder.Entity("Api.Database.Models.Robot", b =>
                {
                    b.HasOne("Api.Database.Models.InspectionArea", "CurrentInspectionArea")
                        .WithMany()
                        .HasForeignKey("CurrentInspectionAreaId");

                    b.HasOne("Api.Database.Models.Installation", "CurrentInstallation")
                        .WithMany()
                        .HasForeignKey("CurrentInstallationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Api.Database.Models.RobotModel", "Model")
                        .WithMany()
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("Api.Database.Models.DocumentInfo", "Documentation", b1 =>
                        {
                            b1.Property<string>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("text");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("character varying(200)");

                            b1.Property<string>("RobotId")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<string>("Url")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("character varying(200)");

                            b1.HasKey("Id");

                            b1.HasIndex("RobotId");

                            b1.ToTable("DocumentInfo");

                            b1.WithOwner()
                                .HasForeignKey("RobotId");
                        });

                    b.OwnsOne("Api.Database.Models.Pose", "Pose", b1 =>
                        {
                            b1.Property<string>("RobotId")
                                .HasColumnType("text");

                            b1.HasKey("RobotId");

                            b1.ToTable("Robots");

                            b1.WithOwner()
                                .HasForeignKey("RobotId");

                            b1.OwnsOne("Api.Database.Models.Orientation", "Orientation", b2 =>
                                {
                                    b2.Property<string>("PoseRobotId")
                                        .HasColumnType("text");

                                    b2.Property<float>("W")
                                        .HasColumnType("real");

                                    b2.Property<float>("X")
                                        .HasColumnType("real");

                                    b2.Property<float>("Y")
                                        .HasColumnType("real");

                                    b2.Property<float>("Z")
                                        .HasColumnType("real");

                                    b2.HasKey("PoseRobotId");

                                    b2.ToTable("Robots");

                                    b2.WithOwner()
                                        .HasForeignKey("PoseRobotId");
                                });

                            b1.OwnsOne("Api.Database.Models.Position", "Position", b2 =>
                                {
                                    b2.Property<string>("PoseRobotId")
                                        .HasColumnType("text");

                                    b2.Property<float>("X")
                                        .HasColumnType("real");

                                    b2.Property<float>("Y")
                                        .HasColumnType("real");

                                    b2.Property<float>("Z")
                                        .HasColumnType("real");

                                    b2.HasKey("PoseRobotId");

                                    b2.ToTable("Robots");

                                    b2.WithOwner()
                                        .HasForeignKey("PoseRobotId");
                                });

                            b1.Navigation("Orientation")
                                .IsRequired();

                            b1.Navigation("Position")
                                .IsRequired();
                        });

                    b.Navigation("CurrentInspectionArea");

                    b.Navigation("CurrentInstallation");

                    b.Navigation("Documentation");

                    b.Navigation("Model");

                    b.Navigation("Pose")
                        .IsRequired();
                });

            modelBuilder.Entity("Api.Services.MissionLoaders.TagInspectionMetadata", b =>
                {
                    b.OwnsOne("Api.Services.Models.IsarZoomDescription", "ZoomDescription", b1 =>
                        {
                            b1.Property<string>("TagInspectionMetadataTagId")
                                .HasColumnType("text");

                            b1.Property<double>("ObjectHeight")
                                .HasColumnType("double precision")
                                .HasAnnotation("Relational:JsonPropertyName", "objectHeight");

                            b1.Property<double>("ObjectWidth")
                                .HasColumnType("double precision")
                                .HasAnnotation("Relational:JsonPropertyName", "objectWidth");

                            b1.HasKey("TagInspectionMetadataTagId");

                            b1.ToTable("TagInspectionMetadata");

                            b1.WithOwner()
                                .HasForeignKey("TagInspectionMetadataTagId");
                        });

                    b.Navigation("ZoomDescription");
                });

            modelBuilder.Entity("Api.Database.Models.MissionRun", b =>
                {
                    b.Navigation("Tasks");
                });
#pragma warning restore 612, 618
        }
    }
}
