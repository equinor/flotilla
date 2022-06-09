using System.Linq;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test
{
    [Collection("Database collection")]
    public class ReportServiceTest
    {
        private readonly DatabaseFixture _fixture;
        private readonly Mock<ILogger<ReportService>> _logger;

        public ReportServiceTest(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _logger = new Mock<ILogger<ReportService>>();
        }

        [Fact]
        public async Task ReadAll()
        {
            var reportService = new ReportService(_fixture.Context, _logger.Object);
            var reports = await reportService.ReadAll();
            Assert.True(reports.Any());
        }

        [Fact]
        public async Task Read()
        {
            var reportService = new ReportService(_fixture.Context, _logger.Object);
            var reports = await reportService.ReadAll();
            var firstReport = reports.First();
            var reportById = await reportService.Read(firstReport.Id);

            Assert.Equal(firstReport, reportById);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var reportService = new ReportService(_fixture.Context, _logger.Object);
            var report = await reportService.Read("some_id_that_does_not_exist");
            Assert.Null(report);
        }

        [Fact]
        public async Task Create()
        {
            var robot = _fixture.Context.Robots.First();
            var reportService = new ReportService(_fixture.Context, _logger.Object);
            int nReportsBefore = reportService.ReadAll().Result.Count();
            await reportService.Create(
                isarMissionId: "",
                echoMissionId: "",
                log: "",
                status: ReportStatus.InProgress,
                robot: robot
            );
            int nReportsAfter = reportService.ReadAll().Result.Count();

            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
