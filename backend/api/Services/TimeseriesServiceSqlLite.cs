using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    /// <summary>
    ///     Uses only dotnet ef core, instead of the Npgsql package needed for the PostgreSql database
    ///     Cannot insert because it is a keyless entity
    /// </summary>
    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class TimeseriesServiceSqlLite(FlotillaDbContext context) : ITimeseriesService
    {
        public async Task<IEnumerable<T>> ReadAll<T>(
            TimeseriesQueryStringParameters queryStringParameters
        ) where T : TimeseriesBase
        {
            var query = context.Set<T>().AsQueryable();
            var filter = ConstructFilter<T>(queryStringParameters);

            query = query.Where(filter);
            query = query.OrderByDescending(timeseries => timeseries.Time);

            return await PagedList<T>.ToPagedListAsync(
                query.OrderByDescending(timeseries => timeseries.Time),
                queryStringParameters.PageNumber,
                queryStringParameters.PageSize
            );
        }

        public async Task AddBatteryEntry(string currentMissionId, float batteryLevel, string robotId) { await Task.CompletedTask; }

        public async Task AddPressureEntry(string currentMissionId, float pressureLevel, string robotId) { await Task.CompletedTask; }

        public async Task AddPoseEntry(string currentMissionId, Pose robotPose, string robotId) { await Task.CompletedTask; }

        public async Task<T> Create<T>(T newTimeseries) where T : TimeseriesBase
        {
            await Task.CompletedTask;
            return newTimeseries;
        }

        private static Expression<Func<T, bool>> ConstructFilter<T>(
            TimeseriesQueryStringParameters parameters
        ) where T : TimeseriesBase
        {
            Expression<Func<T, bool>> robotIdFilter = parameters.RobotId is null
                ? timeseries => true
                : timeseries => timeseries.RobotId.Equals(parameters.RobotId);

            Expression<Func<T, bool>> missionIdFilter = parameters.MissionId is null
                ? timeseries => true
                : timeseries =>
                    timeseries.MissionId == null
                    || timeseries.MissionId.Equals(parameters.MissionId);

            var minStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MinTime);
            var maxStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MaxTime);
            Expression<Func<T, bool>> timeFilter = timeseries =>
                DateTime.Compare(timeseries.Time, minStartTime) >= 0
                && DateTime.Compare(timeseries.Time, maxStartTime) <= 0;

            // The parameter of the filter expression
            var timeseries = Expression.Parameter(typeof(T));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.AndAlso(
                Expression.Invoke(robotIdFilter, timeseries),
                Expression.AndAlso(
                    Expression.Invoke(missionIdFilter, timeseries),
                    Expression.Invoke(timeFilter, timeseries)
                )
            );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<T, bool>>(body, timeseries);
        }
    }
}
