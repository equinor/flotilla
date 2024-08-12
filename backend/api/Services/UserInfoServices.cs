using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IUserInfoService
    {
        public Task<IEnumerable<UserInfo>> ReadAll();

        public Task<UserInfo?> ReadById(string id);

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
    public class UserInfoService(FlotillaDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<UserInfoService> logger) : IUserInfoService
    {
        public async Task<IEnumerable<UserInfo>> ReadAll()
        {
            return await GetUsersInfo().ToListAsync();
        }

        private DbSet<UserInfo> GetUsersInfo()
        {
            return context.UserInfos;
        }

        public async Task<UserInfo?> ReadById(string id)
        {
            return await GetUsersInfo()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<UserInfo?> ReadByOid(string oid)
        {
            return await GetUsersInfo()
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
            var userInfo = await GetUsersInfo()
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
                throw new HttpRequestException("User Info can only be requested in authenticated HTTP requests.");
            var claims = httpContextAccessor.HttpContext.GetRequestedClaims();

            var objectIdClaim = claims.FirstOrDefault(c => c.Type == "oid");
            if (objectIdClaim is null)
            {
                logger.LogWarning("User objectId is null so it will not be added to the database.");
                return null;
            }
            var userInfo = await ReadByOid(objectIdClaim.Value);
            if (userInfo is null)
            {
                var preferredUsernameClaim = claims.FirstOrDefault(c => c.Type == "preferred_username");
                var newUserInfo = new UserInfo
                {
                    Username = preferredUsernameClaim!.Value,
                    Oid = objectIdClaim.Value
                };
                userInfo = await Create(newUserInfo);
            }
            return userInfo;
        }
    }
}
