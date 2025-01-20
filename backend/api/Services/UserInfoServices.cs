using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IUserInfoService
    {
        public Task<IEnumerable<UserInfo>> ReadAll(bool readOnly = true);

        public Task<UserInfo?> ReadById(string id, bool readOnly = true);

        public Task<UserInfo> Create(UserInfo userInfo);

        public Task<UserInfo> Update(UserInfo userInfo);

        public Task<UserInfo?> Delete(string id);

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
        public async Task<IEnumerable<UserInfo>> ReadAll(bool readOnly = true)
        {
            return await GetUsersInfo(readOnly: readOnly).ToListAsync();
        }

        private IQueryable<UserInfo> GetUsersInfo(bool readOnly = true)
        {
            return readOnly ? context.UserInfos.AsNoTracking() : context.UserInfos.AsTracking();
        }

        public async Task<UserInfo?> ReadById(string id, bool readOnly = true)
        {
            return await GetUsersInfo(readOnly: readOnly).FirstOrDefaultAsync(a => a.Id.Equals(id));
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

        public async Task<UserInfo> Update(UserInfo userInfo)
        {
            var entry = context.Update(userInfo);
            await context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<UserInfo?> Delete(string id)
        {
            var userInfo = await GetUsersInfo(readOnly: true)
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (userInfo is null)
            {
                return null;
            }

            context.UserInfos.Remove(userInfo);
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
