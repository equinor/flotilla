using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services
{
    public interface ITimeseriesService
    {
        public Task<IEnumerable<T>> ReadAll<T>(
            TimeseriesQueryStringParameters queryStringParameters
        )
            where T : TimeseriesBase;

        public Task AddBatteryEntry(string currentMissionId, float batteryLevel, string robotId);
        public Task AddPressureEntry(string currentMissionId, float pressureLevel, string robotId);
        public Task AddPoseEntry(string currentMissionId, Pose robotPose, string robotId);
        public Task<T> Create<T>(T newTimeseries)
            where T : TimeseriesBase;
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class TimeseriesService : ITimeseriesService
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<TimeseriesService> _logger;
        private readonly NpgsqlDataSource _dataSource;

        public TimeseriesService(FlotillaDbContext context, ILogger<TimeseriesService> logger)
        {
            string? connectionString =
                context.Database.GetConnectionString()
                ?? throw new NotSupportedException(
                    "Could not get connection string from EF core Database context - Cannot connect to Timeseries"
                );
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            _dataSource = dataSourceBuilder.Build();
            _logger = logger;
            _context = context;
        }

        public async Task AddBatteryEntry(
            string currentMissionId,
            float batteryLevel,
            string robotId
        )
        {
            try
            {
                await Create(
                    new RobotBatteryTimeseries
                    {
                        MissionId = currentMissionId,
                        BatteryLevel = batteryLevel,
                        RobotId = robotId,
                        Time = DateTime.UtcNow,
                    }
                );
            }
            catch (NpgsqlException e)
            {
                _logger.LogError(
                    e,
                    "An error occurred setting battery level while connecting to the timeseries database"
                );
            }
        }

        public async Task AddPressureEntry(
            string currentMissionId,
            float pressureLevel,
            string robotId
        )
        {
            try
            {
                await Create(
                    new RobotPressureTimeseries
                    {
                        MissionId = currentMissionId,
                        Pressure = pressureLevel,
                        RobotId = robotId,
                        Time = DateTime.UtcNow,
                    }
                );
            }
            catch (NpgsqlException e)
            {
                _logger.LogError(
                    e,
                    "An error occurred setting pressure level while connecting to the timeseries database"
                );
            }
        }

        public async Task AddPoseEntry(string currentMissionId, Pose robotPose, string robotId)
        {
            try
            {
                await Create(
                    new RobotPoseTimeseries(robotPose)
                    {
                        MissionId = currentMissionId,
                        RobotId = robotId,
                        Time = DateTime.UtcNow,
                    }
                );
            }
            catch (NpgsqlException e)
            {
                _logger.LogError(
                    e,
                    "An error occurred setting pose while connecting to the timeseries database"
                );
            }
        }

        public async Task<IEnumerable<T>> ReadAll<T>(
            TimeseriesQueryStringParameters queryStringParameters
        )
            where T : TimeseriesBase
        {
            var query = _context.Set<T>().AsQueryable();
            var filter = ConstructFilter<T>(queryStringParameters);

            query = query.Where(filter);
            query = query.OrderByDescending(timeseries => timeseries.Time);

            return await PagedList<T>.ToPagedListAsync(
                query.OrderByDescending(timeseries => timeseries.Time),
                queryStringParameters.PageNumber,
                queryStringParameters.PageSize
            );
        }

        // Cannot use Entity framework to insert keyless entities into the timeseries database
        // https://gibinfrancis.medium.com/timescale-db-with-ef-core-94c948829608
        // https://github.com/npgsql/npgsql
        // Unfortunately need to use npgsql framework with heavy statements for this.
        public async Task<T> Create<T>(T newTimeseries)
            where T : TimeseriesBase
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            string tableName =
                _context.Set<T>().EntityType.GetTableName()
                ?? throw new NotImplementedException(
                    $"Class '{nameof(T)}' is not mapped to a table"
                );

            await using var command = new NpgsqlCommand();
            command.Connection = connection;
            if (newTimeseries.MissionId is not null)
            {
                command.CommandText =
                    $"INSERT INTO \"{tableName}\""
                    + $"(\"{nameof(newTimeseries.Time)}\","
                    + GetColumnNames(newTimeseries)
                    + $"\"{nameof(newTimeseries.RobotId)}\","
                    + $"\"{nameof(newTimeseries.MissionId)}\") "
                    + "VALUES "
                    + $"(@{nameof(newTimeseries.Time)}, "
                    + GetValueNames(newTimeseries)
                    + $"@{nameof(newTimeseries.RobotId)}, "
                    + $"@{nameof(newTimeseries.MissionId)})";

                command.Parameters.AddWithValue(
                    nameof(newTimeseries.MissionId),
                    newTimeseries.MissionId
                );
            }
            else
            {
                command.CommandText =
                    $"INSERT INTO \"{tableName}\""
                    + $"(\"{nameof(newTimeseries.Time)}\","
                    + GetColumnNames(newTimeseries)
                    + $"\"{nameof(newTimeseries.RobotId)}\") "
                    + "VALUES "
                    + $"(@{nameof(newTimeseries.Time)}, "
                    + GetValueNames(newTimeseries)
                    + $"@{nameof(newTimeseries.RobotId)})";
            }

            command.Parameters.AddWithValue(nameof(newTimeseries.RobotId), newTimeseries.RobotId);
            command.Parameters.AddWithValue(nameof(newTimeseries.Time), newTimeseries.Time);

            AddParameterValues(command.Parameters, newTimeseries);

            await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();

            return newTimeseries;
        }

        private static Expression<Func<T, bool>> ConstructFilter<T>(
            TimeseriesQueryStringParameters parameters
        )
            where T : TimeseriesBase
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

        private static void AddParameterValues<T>(NpgsqlParameterCollection parameters, T entity)
        {
            if (entity is RobotPressureTimeseries robotPressureTimeseries)
            {
                parameters.AddWithValue(
                    nameof(robotPressureTimeseries.Pressure),
                    robotPressureTimeseries.Pressure
                );
            }
            else if (entity is RobotBatteryTimeseries robotBatteryTimeseries)
            {
                parameters.AddWithValue(
                    nameof(robotBatteryTimeseries.BatteryLevel),
                    robotBatteryTimeseries.BatteryLevel
                );
            }
            else if (entity is RobotPoseTimeseries robotPoseTimeseries)
            {
                // Position
                parameters.AddWithValue(
                    nameof(robotPoseTimeseries.PositionX),
                    robotPoseTimeseries.PositionX
                );
                parameters.AddWithValue(
                    nameof(robotPoseTimeseries.PositionY),
                    robotPoseTimeseries.PositionY
                );
                parameters.AddWithValue(
                    nameof(robotPoseTimeseries.PositionZ),
                    robotPoseTimeseries.PositionZ
                );

                // Orientation
                parameters.AddWithValue(
                    nameof(robotPoseTimeseries.OrientationX),
                    robotPoseTimeseries.OrientationX
                );
                parameters.AddWithValue(
                    nameof(robotPoseTimeseries.OrientationY),
                    robotPoseTimeseries.OrientationY
                );
                parameters.AddWithValue(
                    nameof(robotPoseTimeseries.OrientationZ),
                    robotPoseTimeseries.OrientationZ
                );
                parameters.AddWithValue(
                    nameof(robotPoseTimeseries.OrientationW),
                    robotPoseTimeseries.OrientationW
                );
            }
            else
            {
                throw new NotImplementedException(
                    $"No parameter values defined for timeseries type '{nameof(T)}'"
                );
            }
        }

        private static string GetColumnNames<T>(T entity)
            where T : TimeseriesBase
        {
            return GetContentNames(entity, "\"", "\"");
        }

        private static string GetValueNames<T>(T entity)
            where T : TimeseriesBase
        {
            return GetContentNames(entity, "@");
        }

        private static string GetContentNames<T>(
            T entity,
            string namePrefix = "",
            string namePostfix = ""
        )
            where T : TimeseriesBase
        {
            if (entity is RobotPressureTimeseries robotPressureTimeseries)
            {
                return $"{namePrefix}{nameof(robotPressureTimeseries.Pressure)}{namePostfix},";
            }

            if (entity is RobotBatteryTimeseries robotBatteryTimeseries)
            {
                return $"{namePrefix}{nameof(robotBatteryTimeseries.BatteryLevel)}{namePostfix},";
            }

            if (entity is RobotPoseTimeseries robotPoseTimeseries)
            {
                return $"{namePrefix}{nameof(robotPoseTimeseries.PositionX)}{namePostfix},{namePrefix}{nameof(robotPoseTimeseries.PositionY)}{namePostfix},{namePrefix}{nameof(robotPoseTimeseries.PositionZ)}{namePostfix},"
                    + $"{namePrefix}{nameof(robotPoseTimeseries.OrientationX)}{namePostfix},{namePrefix}{nameof(robotPoseTimeseries.OrientationY)}{namePostfix},{namePrefix}{nameof(robotPoseTimeseries.OrientationZ)}{namePostfix},{namePrefix}{nameof(robotPoseTimeseries.OrientationW)}{namePostfix},";
            }

            throw new NotImplementedException(
                $"No content names defined for timeseries type '{nameof(T)}'"
            );
        }
    }
}
