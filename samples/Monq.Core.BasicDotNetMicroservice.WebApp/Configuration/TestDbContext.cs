using Microsoft.EntityFrameworkCore;
using Monq.Core.BasicDotNetMicroservice.WebApp.Models;

namespace Monq.Core.BasicDotNetMicroservice.WebApp.Configuration;

public sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();
        });
    }
    }
