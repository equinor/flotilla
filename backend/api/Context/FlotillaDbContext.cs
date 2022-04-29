using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();

    private static bool _initialized = false;

    public FlotillaDbContext(DbContextOptions options) : base(options)
    {
        if (_initialized == false)
        {
            InitDb.PopulateDb(this);
            _initialized = true;
        }
    }
}
