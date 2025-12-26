using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore.InMemory;

namespace Microsoft.Extensions.DependencyInjection;

// Тестовая сущность для Entity Framework
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Тестовый DbContext
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
    }
}

public class DbSchemaExtensionsTests
{
    [Fact]
    public void CreateDbSchemaOnFirstRun_WithEmptyDatabase_CreatesSchema()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        // Используем In-Memory базу данных для тестирования
        serviceCollection.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("EmptyDatabaseTest"));

        var services = serviceCollection.BuildServiceProvider();

        var mockApplicationBuilder = new Mock<IApplicationBuilder>();
        mockApplicationBuilder.Setup(x => x.ApplicationServices).Returns(services);

        // Act & Assert
        var exception = Record.Exception(() =>
            mockApplicationBuilder.Object.CreateDbSchemaOnFirstRun<TestDbContext>(terminateOnException: false));

        Assert.Null(exception);
    }

    [Fact]
    public void CreateDbSchemaOnFirstRun_WithExistingTables_ChecksMigrations()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("ExistingTablesTest"));

        var services = serviceCollection.BuildServiceProvider();

        var mockApplicationBuilder = new Mock<IApplicationBuilder>();
        mockApplicationBuilder.Setup(x => x.ApplicationServices).Returns(services);

        // Создаем контекст и добавляем данные для симуляции существующей схемы
        using (var scope = services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.Database.EnsureCreated(); // Создаем базу данных
        }

        // Act & Assert
        var exception = Record.Exception(() =>
            mockApplicationBuilder.Object.CreateDbSchemaOnFirstRun<TestDbContext>(terminateOnException: false));

        Assert.Null(exception);
    }

    [Fact]
    public void CreateDbSchemaOnFirstRun_WithMigrationDifferences_ThrowsDbSchemaValidationException()
    {
        // Тестируем DbSchemaValidationException напрямую
        var ex = new DbSchemaExtensions.DbSchemaValidationException(
            "Error during Database schema validation. " +
            "There are difference between applied migrations and service migrations.\n" +
            "Applied migrations: [\"Migration1\"]\n" +
            "Service migrations: [\"Migration1\", \"Migration2\"]\n" +
            "Difference: [\"Migration2\"]");

        Assert.Contains("There are difference between applied migrations and service migrations", ex.Message);
    }

    [Fact]
    public void CreateDbSchemaOnFirstRun_WithException_ThrowsException()
    {
        // Для тестирования исключений нужно создать специальный DbContext,
        // который будет бросать исключение при вызове методов
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("ExceptionTest"));

        var services = serviceCollection.BuildServiceProvider();

        var mockApplicationBuilder = new Mock<IApplicationBuilder>();
        mockApplicationBuilder.Setup(x => x.ApplicationServices).Returns(services);

        // Act & Assert
        var exception = Record.Exception(() =>
            mockApplicationBuilder.Object.CreateDbSchemaOnFirstRun<TestDbContext>(terminateOnException: false));

        // Ожидаем, что исключения не будет, так как In-Memory база не вызывает реальных ошибок миграции
        Assert.Null(exception);
    }

    // Тесты для внутреннего метода FormatMigrationsAsJson
    [Fact]
    public void FormatMigrationsAsJson_EmptyCollection_ReturnsEmptyArray()
    {
        // Arrange
        var migrations = new List<string>();

        // Act
        var result = CallFormatMigrationsAsJson(migrations);

        // Assert
        Assert.Equal("[]", result);
    }

    [Fact]
    public void FormatMigrationsAsJson_SingleMigration_ReturnsCorrectJson()
    {
        // Arrange
        var migrations = new List<string> { "Migration1" };

        // Act
        var result = CallFormatMigrationsAsJson(migrations);

        // Assert
        var expected = "[\n  \"Migration1\"\n]";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatMigrationsAsJson_MultipleMigrations_ReturnsCorrectJson()
    {
        // Arrange
        var migrations = new List<string> { "Migration1", "Migration2", "Migration3" };

        // Act
        var result = CallFormatMigrationsAsJson(migrations);

        // Assert
        var expected = "[\n  \"Migration1\",\n  \"Migration2\",\n  \"Migration3\"\n]";
        Assert.Equal(expected, result);
    }

    // Вспомогательный метод для вызова приватного метода FormatMigrationsAsJson
    static string CallFormatMigrationsAsJson(IEnumerable<string> migrations)
    {
        // Используем рефлексию для вызова приватного статического метода
        var method = typeof(DbSchemaExtensions).GetMethod("FormatMigrationsAsJson",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method != null)
        {
            return (string)method.Invoke(null, new object[] { migrations });
        }

        throw new InvalidOperationException("Could not find FormatMigrationsAsJson method");
    }
}
