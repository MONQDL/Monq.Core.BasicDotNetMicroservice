using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Monq.Core.BasicDotNetMicroservice.WebApp.Configuration;

public sealed class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseNpgsql("host=localhost;database=testdb;username=postgres;password=postgres");
        return new TestDbContext(optionsBuilder.Options);
    }
}
