using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();

    public static bool Initialized = false;

    public FlotillaDbContext(DbContextOptions options) : base(options)
    {
        if (Initialized == false)
        {
            InitDb.PopulateDb(this);
            Initialized = true;
        }
    }
}
