using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IUserInfoService
    {
        public Task<UserInfo?> GetRequestedUserInfo();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class UserInfoService(
        FlotillaDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserInfoService> logger
    ) : IUserInfoService
    {
        private IQueryable<UserInfo> GetUsersInfo(bool readOnly = true)
        {
            return readOnly ? context.UserInfos.AsNoTracking() : context.UserInfos.AsTracking();
        }

        public async Task<UserInfo?> ReadByOid(string oid, bool readOnly = true)
        {
            return await GetUsersInfo(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Oid.Equals(oid));
        }

        public async Task<UserInfo> Create(UserInfo userInfo)
        {
            await context.UserInfos.AddAsync(userInfo);
            await context.SaveChangesAsync();
            return userInfo;
        }

        public async Task<UserInfo?> GetRequestedUserInfo()
        {
            if (httpContextAccessor.HttpContext == null)
                return null;

            string? objectId = httpContextAccessor.HttpContext.GetUserObjectId();
            if (objectId is null)
            {
                logger.LogWarning("User objectId is null so it will not be added to the database.");
                return null;
            }
            var userInfo = await ReadByOid(objectId, readOnly: true);
            if (userInfo is null)
            {
                var newUserInfo = new UserInfo { Oid = objectId };
                userInfo = await Create(newUserInfo);
            }
            return userInfo;
        }
    }
}
