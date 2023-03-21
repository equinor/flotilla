﻿// <auto-generated />
using System;
using Api.Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Api.Migrations
{
    [DbContext(typeof(FlotillaDbContext))]
    partial class FlotillaDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.13")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Api.Database.Models.Mission", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AssetCode")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Comment")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Description")
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTimeOffset>("DesiredStartTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("EchoMissionId")
                        .HasMaxLength(200)
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("EndTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<TimeSpan>("EstimatedDuration")
                        .HasColumnType("time");

                    b.Property<string>("IsarMissionId")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("MissionStatus")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("RobotId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTimeOffset?>("StartTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("StatusReason")
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RobotId");

                    b.ToTable("Missions");
                });

            modelBuilder.Entity("Api.Database.Models.Robot", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(450)");

                    b.Property<float>("BatteryLevel")
                        .HasColumnType("real");

                    b.Property<bool>("Enabled")
                        .HasColumnType("bit");

                    b.Property<string>("Host")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("IsarId")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Logs")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("Model")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("Port")
                        .HasColumnType("int");

                    b.Property<float?>("PressureLevel")
                        .HasColumnType("real");

                    b.Property<string>("SerialNumber")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Robots");
                });

            modelBuilder.Entity("Api.Database.Models.Mission", b =>
                {
                    b.HasOne("Api.Database.Models.Robot", "Robot")
                        .WithMany()
                        .HasForeignKey("RobotId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("Api.Database.Models.IsarTask", "Tasks", b1 =>
                        {
                            b1.Property<string>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("nvarchar(450)");

                            b1.Property<string>("EchoLink")
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.Property<string>("IsarTaskId")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.Property<string>("MissionId")
                                .IsRequired()
                                .HasColumnType("nvarchar(450)");

                            b1.Property<string>("TagId")
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.Property<int>("TaskStatus")
                                .HasColumnType("int");

                            b1.Property<DateTimeOffset>("Time")
                                .HasColumnType("datetimeoffset");

                            b1.HasKey("Id");

                            b1.HasIndex("MissionId");

                            b1.ToTable("IsarTask");

                            b1.WithOwner("Mission")
                                .HasForeignKey("MissionId");

                            b1.OwnsMany("Api.Database.Models.IsarStep", "Steps", b2 =>
                                {
                                    b2.Property<string>("Id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<string>("FileLocation")
                                        .HasMaxLength(200)
                                        .HasColumnType("nvarchar(200)");

                                    b2.Property<int>("InspectionType")
                                        .HasColumnType("int");

                                    b2.Property<string>("IsarStepId")
                                        .IsRequired()
                                        .HasMaxLength(200)
                                        .HasColumnType("nvarchar(200)");

                                    b2.Property<int>("StepStatus")
                                        .HasColumnType("int");

                                    b2.Property<int>("StepType")
                                        .HasColumnType("int");

                                    b2.Property<string>("TagId")
                                        .HasMaxLength(200)
                                        .HasColumnType("nvarchar(200)");

                                    b2.Property<string>("TaskId")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<DateTimeOffset>("Time")
                                        .HasColumnType("datetimeoffset");

                                    b2.HasKey("Id");

                                    b2.HasIndex("TaskId");

                                    b2.ToTable("IsarStep");

                                    b2.WithOwner("Task")
                                        .HasForeignKey("TaskId");

                                    b2.Navigation("Task");
                                });

                            b1.Navigation("Mission");

                            b1.Navigation("Steps");
                        });

                    b.OwnsOne("Api.Database.Models.MissionMap", "Map", b1 =>
                        {
                            b1.Property<string>("MissionId")
                                .HasColumnType("nvarchar(450)");

                            b1.Property<string>("MapName")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.HasKey("MissionId");

                            b1.ToTable("Missions");

                            b1.WithOwner()
                                .HasForeignKey("MissionId");

                            b1.OwnsOne("Api.Database.Models.Boundary", "Boundary", b2 =>
                                {
                                    b2.Property<string>("MissionMapMissionId")
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<double>("X1")
                                        .HasColumnType("float");

                                    b2.Property<double>("X2")
                                        .HasColumnType("float");

                                    b2.Property<double>("Y1")
                                        .HasColumnType("float");

                                    b2.Property<double>("Y2")
                                        .HasColumnType("float");

                                    b2.Property<double>("Z1")
                                        .HasColumnType("float");

                                    b2.Property<double>("Z2")
                                        .HasColumnType("float");

                                    b2.HasKey("MissionMapMissionId");

                                    b2.ToTable("Missions");

                                    b2.WithOwner()
                                        .HasForeignKey("MissionMapMissionId");
                                });

                            b1.OwnsOne("Api.Database.Models.TransformationMatrices", "TransformationMatrices", b2 =>
                                {
                                    b2.Property<string>("MissionMapMissionId")
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<double>("C1")
                                        .HasColumnType("float");

                                    b2.Property<double>("C2")
                                        .HasColumnType("float");

                                    b2.Property<double>("D1")
                                        .HasColumnType("float");

                                    b2.Property<double>("D2")
                                        .HasColumnType("float");

                                    b2.HasKey("MissionMapMissionId");

                                    b2.ToTable("Missions");

                                    b2.WithOwner()
                                        .HasForeignKey("MissionMapMissionId");
                                });

                            b1.Navigation("Boundary")
                                .IsRequired();

                            b1.Navigation("TransformationMatrices")
                                .IsRequired();
                        });

                    b.OwnsMany("Api.Database.Models.PlannedTask", "PlannedTasks", b1 =>
                        {
                            b1.Property<string>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("nvarchar(450)");

                            b1.Property<string>("MissionId")
                                .IsRequired()
                                .HasColumnType("nvarchar(450)");

                            b1.Property<int>("PlanOrder")
                                .HasColumnType("int");

                            b1.Property<int>("PoseId")
                                .HasColumnType("int");

                            b1.Property<string>("TagId")
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.Property<string>("URL")
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.HasKey("Id");

                            b1.HasIndex("MissionId");

                            b1.ToTable("PlannedTask");

                            b1.WithOwner()
                                .HasForeignKey("MissionId");

                            b1.OwnsMany("Api.Database.Models.PlannedInspection", "Inspections", b2 =>
                                {
                                    b2.Property<string>("Id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<string>("AnalysisTypes")
                                        .HasMaxLength(250)
                                        .HasColumnType("nvarchar(250)");

                                    b2.Property<int>("InspectionType")
                                        .HasColumnType("int");

                                    b2.Property<string>("PlannedTaskId")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<float?>("TimeInSeconds")
                                        .HasColumnType("real");

                                    b2.HasKey("Id");

                                    b2.HasIndex("PlannedTaskId");

                                    b2.ToTable("PlannedInspection");

                                    b2.WithOwner()
                                        .HasForeignKey("PlannedTaskId");
                                });

                            b1.OwnsOne("Api.Database.Models.Pose", "Pose", b2 =>
                                {
                                    b2.Property<string>("PlannedTaskId")
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<string>("Frame")
                                        .HasMaxLength(200)
                                        .HasColumnType("nvarchar(200)");

                                    b2.HasKey("PlannedTaskId");

                                    b2.ToTable("PlannedTask");

                                    b2.WithOwner()
                                        .HasForeignKey("PlannedTaskId");

                                    b2.OwnsOne("Api.Database.Models.Orientation", "Orientation", b3 =>
                                        {
                                            b3.Property<string>("PosePlannedTaskId")
                                                .HasColumnType("nvarchar(450)");

                                            b3.Property<float>("W")
                                                .HasColumnType("real");

                                            b3.Property<float>("X")
                                                .HasColumnType("real");

                                            b3.Property<float>("Y")
                                                .HasColumnType("real");

                                            b3.Property<float>("Z")
                                                .HasColumnType("real");

                                            b3.HasKey("PosePlannedTaskId");

                                            b3.ToTable("PlannedTask");

                                            b3.WithOwner()
                                                .HasForeignKey("PosePlannedTaskId");
                                        });

                                    b2.OwnsOne("Api.Database.Models.Position", "Position", b3 =>
                                        {
                                            b3.Property<string>("PosePlannedTaskId")
                                                .HasColumnType("nvarchar(450)");

                                            b3.Property<float>("X")
                                                .HasColumnType("real");

                                            b3.Property<float>("Y")
                                                .HasColumnType("real");

                                            b3.Property<float>("Z")
                                                .HasColumnType("real");

                                            b3.HasKey("PosePlannedTaskId");

                                            b3.ToTable("PlannedTask");

                                            b3.WithOwner()
                                                .HasForeignKey("PosePlannedTaskId");
                                        });

                                    b2.Navigation("Orientation")
                                        .IsRequired();

                                    b2.Navigation("Position")
                                        .IsRequired();
                                });

                            b1.OwnsOne("Api.Database.Models.Position", "TagPosition", b2 =>
                                {
                                    b2.Property<string>("PlannedTaskId")
                                        .HasColumnType("nvarchar(450)");

                                    b2.Property<float>("X")
                                        .HasColumnType("real");

                                    b2.Property<float>("Y")
                                        .HasColumnType("real");

                                    b2.Property<float>("Z")
                                        .HasColumnType("real");

                                    b2.HasKey("PlannedTaskId");

                                    b2.ToTable("PlannedTask");

                                    b2.WithOwner()
                                        .HasForeignKey("PlannedTaskId");
                                });

                            b1.Navigation("Inspections");

                            b1.Navigation("Pose")
                                .IsRequired();

                            b1.Navigation("TagPosition")
                                .IsRequired();
                        });

                    b.Navigation("Map")
                        .IsRequired();

                    b.Navigation("PlannedTasks");

                    b.Navigation("Robot");

                    b.Navigation("Tasks");
                });

            modelBuilder.Entity("Api.Database.Models.Robot", b =>
                {
                    b.OwnsOne("Api.Database.Models.Pose", "Pose", b1 =>
                        {
                            b1.Property<string>("RobotId")
                                .HasColumnType("nvarchar(450)");

                            b1.Property<string>("Frame")
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.HasKey("RobotId");

                            b1.ToTable("Robots");

                            b1.WithOwner()
                                .HasForeignKey("RobotId");

                            b1.OwnsOne("Api.Database.Models.Orientation", "Orientation", b2 =>
                                {
                                    b2.Property<string>("PoseRobotId")
                                        .HasColumnType("nvarchar(450)");

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
                                        .HasColumnType("nvarchar(450)");

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

                    b.OwnsMany("Api.Database.Models.VideoStream", "VideoStreams", b1 =>
                        {
                            b1.Property<string>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("nvarchar(450)");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.Property<string>("RobotId")
                                .IsRequired()
                                .HasColumnType("nvarchar(450)");

                            b1.Property<bool>("ShouldRotate270Clockwise")
                                .HasColumnType("bit");

                            b1.Property<string>("Type")
                                .IsRequired()
                                .HasMaxLength(64)
                                .HasColumnType("nvarchar(64)");

                            b1.Property<string>("Url")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("nvarchar(200)");

                            b1.HasKey("Id");

                            b1.HasIndex("RobotId");

                            b1.ToTable("VideoStream");

                            b1.WithOwner()
                                .HasForeignKey("RobotId");
                        });

                    b.Navigation("Pose")
                        .IsRequired();

                    b.Navigation("VideoStreams");
                });
#pragma warning restore 612, 618
        }
    }
}
