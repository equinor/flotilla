using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class ReportServiceTest : IDisposable
    {
        private readonly FlotillaDbContext _context;
        private readonly Mock<ILogger<ReportService>> _logger;
        private readonly ReportService _reportService;

        public ReportServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<ReportService>>();
            _reportService = new ReportService(_context, _logger.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadAll()
        {
            var reports = await _reportService.ReadAll();
            Assert.True(reports.Any());
        }

        [Fact]
        public async Task Read()
        {
            var reports = await _reportService.ReadAll();
            var firstReport = reports.First();
            var reportById = await _reportService.Read(firstReport.Id);

            Assert.Equal(firstReport, reportById);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var report = await _reportService.Read("some_id_that_does_not_exist");
            Assert.Null(report);
        }

        [Fact]
        public async Task Create()
        {
            var robot = _context.Robots.First();
            int nReportsBefore = _reportService.ReadAll().Result.Count;
            await _reportService.Create(
                isarMissionId: "",
                echoMissionId: 0,
                log: "",
                status: ReportStatus.InProgress,
                robot: robot
            );
            int nReportsAfter = _reportService.ReadAll().Result.Count;

            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
