using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Report> Reports => Set<Report>();

    public DbSet<Event> Events => Set<Event>();

    private static bool initialized;

    public FlotillaDbContext(DbContextOptions options) : base(options)
    {
        if (initialized == false)
        {
            InitDb.PopulateDb(this);
            initialized = true;
        }
    }
}
