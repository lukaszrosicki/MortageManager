using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MortgagePro.Infrastructure.Data;

public class ScenarioSnapshotEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal BaselineInterest { get; set; }
    public string SerializedSchedule { get; set; }
    public string UserId { get; set; }
}

public class MortgageDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<ScenarioSnapshotEntity> Scenarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=mortgage.db");
    }
}
