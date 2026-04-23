using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class NativeAotDbSchemaExtensionsTests
{
    [Fact]
    public void ParseMigrationsFromSql_WithValidMetadata_ReturnsMigrations()
    {
        var sql = "-- MONQ_MIGRATIONS: [\"20240101000001_Initial\", \"20240102000002_AddUsers\"]\nCREATE TABLE ...";

        var result = CallParseMigrationsFromSql(sql);

        Assert.Equal(2, result.Count);
        Assert.Equal("20240101000001_Initial", result[0]);
        Assert.Equal("20240102000002_AddUsers", result[1]);
    }

    [Fact]
    public void ParseMigrationsFromSql_WithEmptyMigrations_ReturnsEmptyList()
    {
        var sql = "-- MONQ_MIGRATIONS: []\nCREATE TABLE ...";

        var result = CallParseMigrationsFromSql(sql);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseMigrationsFromSql_WithoutMetadata_ThrowsInvalidOperationException()
    {
        var sql = "CREATE TABLE ...";

        var ex = Assert.ThrowsAny<Exception>(() => CallParseMigrationsFromSql(sql));
        Assert.Contains("SQL script does not contain migration metadata", ex.InnerException?.Message ?? ex.Message);
    }

    [Fact]
    public void ParseMigrationsFromSql_InvalidJson_ThrowsException()
    {
        var sql = "-- MONQ_MIGRATIONS: [invalid json]\nCREATE TABLE ...";

        Assert.ThrowsAny<Exception>(() => CallParseMigrationsFromSql(sql));
    }

    [Fact]
    public void CreateDbSchemaOnFirstRunNative_WithFunc_ReturnsSqlFromDelegate()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("NativeAotFuncTest"));

        var services = serviceCollection.BuildServiceProvider();
        var mockApplicationBuilder = new Mock<IApplicationBuilder>();
        mockApplicationBuilder.Setup(x => x.ApplicationServices).Returns(services);

        var sql = "-- MONQ_MIGRATIONS: []\nCREATE TABLE ...";
        var exception = Record.Exception(() =>
            mockApplicationBuilder.Object.CreateDbSchemaOnFirstRunNative<TestDbContext>(
                () => sql,
                terminateOnException: false));

        Assert.Null(exception);
    }

    [Fact]
    public void CreateDbSchemaOnFirstRunNative_WithAssembly_ResourceNotFound_ThrowsException()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("NativeAotAssemblyTest"));

        var services = serviceCollection.BuildServiceProvider();
        var mockApplicationBuilder = new Mock<IApplicationBuilder>();
        mockApplicationBuilder.Setup(x => x.ApplicationServices).Returns(services);

        Func<string> getSql = () =>
        {
            var stream = typeof(NativeAotDbSchemaExtensionsTests).Assembly.GetManifestResourceStream("NonExistentResource.sql")
                ?? throw new InvalidOperationException(
                    $"Embedded resource 'NonExistentResource.sql' not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        };

        var exception = Record.Exception(() => getSql());

        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void CreateDbSchemaOnFirstRunNative_WithAssembly_ResourceExists_LoadsSql()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("NativeAotAssemblyTest2"));

        var services = serviceCollection.BuildServiceProvider();
        var mockApplicationBuilder = new Mock<IApplicationBuilder>();
        mockApplicationBuilder.Setup(x => x.ApplicationServices).Returns(services);

        var sql = "-- MONQ_MIGRATIONS: []\nCREATE TABLE ...";

        var exception = Record.Exception(() =>
            mockApplicationBuilder.Object.CreateDbSchemaOnFirstRunNative<TestDbContext>(
                () => sql,
                terminateOnException: false));

        Assert.Null(exception);
    }

    [Fact]
    public void FormatMigrationsAsJson_EmptyCollection_ReturnsEmptyArray()
    {
        var migrations = new List<string>();

        var result = CallFormatMigrationsAsJson(migrations);

        Assert.Equal("[]", result);
    }

    [Fact]
    public void FormatMigrationsAsJson_SingleMigration_ReturnsCorrectJson()
    {
        var migrations = new List<string> { "Migration1" };

        var result = CallFormatMigrationsAsJson(migrations);

        var expected = "[\n  \"Migration1\"\n]";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatMigrationsAsJson_MultipleMigrations_ReturnsCorrectJson()
    {
        var migrations = new List<string> { "Migration1", "Migration2", "Migration3" };

        var result = CallFormatMigrationsAsJson(migrations);

        var expected = "[\n  \"Migration1\",\n  \"Migration2\",\n  \"Migration3\"\n]";
        Assert.Equal(expected, result);
    }

    static List<string> CallParseMigrationsFromSql(string sql)
    {
        var method = typeof(NativeAotDbSchemaExtensions).GetMethod("ParseMigrationsFromSql",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method != null)
        {
            return (List<string>)method.Invoke(null, new object[] { sql });
        }

        throw new InvalidOperationException("Could not find ParseMigrationsFromSql method");
    }

    static string CallFormatMigrationsAsJson(IEnumerable<string> migrations)
    {
        var method = typeof(NativeAotDbSchemaExtensions).GetMethod("FormatMigrationsAsJson",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method != null)
        {
            return (string)method.Invoke(null, new object[] { migrations });
        }

        throw new InvalidOperationException("Could not find FormatMigrationsAsJson method");
    }
}
