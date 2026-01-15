using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EtherGizmos.Common.Services;

internal class UnitOfWorkTests
{
    private IServiceProvider _rootProvider;
    private IServiceProvider _scopeProvider;
    private IUnitOfWorkFactory _uowFactory;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var services = new ServiceCollection();

        //We don't bind this one to the unit of work, we just want it to create tables
        services.AddDbContext<BuildContext>(opt =>
        {
            opt.UseNpgsql(Setup.PgSqlConnectionString);
        });

        //We do want to bind these, though
        services.AddDbContext<TestContextA>(opt =>
        {
            opt.UseNpgsql(Setup.PgSqlConnectionString);
        });

        services.AddDbContext<TestContextB>(opt =>
        {
            opt.UseNpgsql(Setup.PgSqlConnectionString);
        });

        services.AddDbContext<TestContextF>(opt =>
        {
            opt.UseNpgsql(Setup.PgSqlConnectionString);
        });

        services.AddUnitOfWork(opt =>
        {
            opt.BindDbContext<TestContextA>();
            opt.BindDbContext<TestContextB>();
            opt.BindDbContext<TestContextF>();
        });

        _rootProvider = services.BuildServiceProvider();

        using var scope = _rootProvider.CreateScope();
        var provider = scope.ServiceProvider;

        using var connection = new NpgsqlConnection(Setup.PgSqlConnectionString);

        using var context = provider.GetRequiredService<BuildContext>();
        await context.Database.EnsureCreatedAsync();

        //We DO NOT want to create the database for TestContextF; that one is intended to fail
    }

    [SetUp]
    public void SetUp()
    {
        _scopeProvider = _rootProvider.CreateScope().ServiceProvider;
        _uowFactory = _scopeProvider.GetRequiredService<IUnitOfWorkFactory>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_scopeProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_rootProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    public void Self_WhenCreated_ShouldBeUnitOfWork()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create();

        //Assert
        Assert.That(uow, Is.InstanceOf<UnitOfWork>());
    }

    [Test]
    public async Task SaveChanges_WithOneContext_ShouldSaveChanges()
    {
        //Arrange
        using var uow = _uowFactory.Create();

        var repository = uow.Repository<TestEntityA>();
        var entity = new TestEntityA()
        {
            Value = "Value",
        };

        repository.Add(entity);

        //Act & Assert
        Assert.DoesNotThrow(() =>
        {
            uow.SaveChanges();
        });

        using var uow2 = _uowFactory.Create();
        var repository2 = uow2.Repository<TestEntityA>();

        var entity2 = await repository2.Data.SingleOrDefaultAsync(e => e.Id == entity.Id);
        Assert.That(entity2, Is.Not.Null);
    }

    [Test]
    public async Task SaveChangesAsync_WithOneContext_ShouldSaveChanges()
    {
        //Arrange
        using var uow = _uowFactory.Create();

        var repository = uow.Repository<TestEntityA>();
        var entity = new TestEntityA()
        {
            Value = "Value",
        };

        repository.Add(entity);

        //Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await uow.SaveChangesAsync();
        });

        using var uow2 = _uowFactory.Create();
        var repository2 = uow2.Repository<TestEntityA>();

        var entity2 = await repository2.Data.SingleOrDefaultAsync(e => e.Id == entity.Id);
        Assert.That(entity2, Is.Not.Null);
    }

    [Test]
    public async Task SaveChanges_WithTwoContexts_ShouldSaveChanges()
    {
        //Arrange
        using var uow = _uowFactory.Create();

        var repositoryA = uow.Repository<TestEntityA>();
        var entityA = new TestEntityA()
        {
            Value = "Value",
        };

        repositoryA.Add(entityA);

        var repositoryB = uow.Repository<TestEntityB>();
        var entityB = new TestEntityB()
        {
            Value = "Value",
        };

        repositoryB.Add(entityB);

        //Act & Assert
        Assert.DoesNotThrow(() =>
        {
            uow.SaveChanges();
        });

        using var uow2 = _uowFactory.Create();
        var repository2A = uow2.Repository<TestEntityA>();
        var repository2B = uow2.Repository<TestEntityB>();

        var entity2A = await repository2A.Data.SingleOrDefaultAsync(e => e.Id == entityA.Id);
        Assert.That(entity2A, Is.Not.Null);

        var entity2B = await repository2B.Data.SingleOrDefaultAsync(e => e.Id == entityB.Id);
        Assert.That(entity2B, Is.Not.Null);
    }

    [Test]
    public async Task SaveChangesAsync_WithTwoContexts_ShouldSaveChanges()
    {
        //Arrange
        using var uow = _uowFactory.Create();

        var repositoryA = uow.Repository<TestEntityA>();
        var entityA = new TestEntityA()
        {
            Value = "Value",
        };

        repositoryA.Add(entityA);

        var repositoryB = uow.Repository<TestEntityB>();
        var entityB = new TestEntityB()
        {
            Value = "Value",
        };

        repositoryB.Add(entityB);

        //Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await uow.SaveChangesAsync();
        });

        using var uow2 = _uowFactory.Create();
        var repository2A = uow2.Repository<TestEntityA>();
        var repository2B = uow2.Repository<TestEntityB>();

        var entity2A = await repository2A.Data.SingleOrDefaultAsync(e => e.Id == entityA.Id);
        Assert.That(entity2A, Is.Not.Null);

        var entity2B = await repository2B.Data.SingleOrDefaultAsync(e => e.Id == entityB.Id);
        Assert.That(entity2B, Is.Not.Null);
    }

    [Test]
    public async Task SaveChanges_WithTwoContextsAndError_ShouldNotSaveChanges()
    {
        //Arrange
        using var uow = _uowFactory.Create();

        var repositoryA = uow.Repository<TestEntityA>();
        var entityA = new TestEntityA()
        {
            Value = "Value",
        };

        repositoryA.Add(entityA);

        var repositoryF = uow.Repository<TestEntityF>();
        var entityF = new TestEntityF()
        {
            Value = "Value",
        };

        repositoryF.Add(entityF);

        //Act & Assert
        Assert.Throws<AggregateException>(() =>
        {
            uow.SaveChanges();
        });

        using var uow2 = _uowFactory.Create();
        var repository2A = uow2.Repository<TestEntityA>();
        var repository2F = uow2.Repository<TestEntityF>();

        var entity2A = await repository2A.Data.SingleOrDefaultAsync(e => e.Id == entityA.Id);
        Assert.That(entity2A, Is.Null);
    }

    [Test]
    public async Task SaveChangesAsync_WithTwoContextsAndError_ShouldNotSaveChanges()
    {
        //Arrange
        using var uow = _uowFactory.Create();

        var repositoryA = uow.Repository<TestEntityA>();
        var entityA = new TestEntityA()
        {
            Value = "Value",
        };

        repositoryA.Add(entityA);

        var repositoryF = uow.Repository<TestEntityF>();
        var entityF = new TestEntityF()
        {
            Value = "Value",
        };

        repositoryF.Add(entityF);

        //Act & Assert
        Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await uow.SaveChangesAsync();
        });

        using var uow2 = _uowFactory.Create();
        var repository2A = uow2.Repository<TestEntityA>();
        var repository2F = uow2.Repository<TestEntityF>();

        var entity2A = await repository2A.Data.SingleOrDefaultAsync(e => e.Id == entityA.Id);
        Assert.That(entity2A, Is.Null);
    }

    private class BuildContext : DbContext
    {
        public DbSet<TestEntityA> TestDataA { get; set; }

        public DbSet<TestEntityB> TestDataB { get; set; }

        public BuildContext(
            DbContextOptions<BuildContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var testDataA = modelBuilder.Entity<TestEntityA>();

            testDataA.ToTable("test_a");

            testDataA.HasKey(e => e.Id);
            testDataA.Property(e => e.Value);

            var testDataB = modelBuilder.Entity<TestEntityB>();

            testDataB.ToTable("test_b");

            testDataB.HasKey(e => e.Id);
            testDataB.Property(e => e.Value);
        }
    }

    private class TestContextA : DbContext
    {
        public DbSet<TestEntityA> TestData { get; set; }

        public TestContextA(
            DbContextOptions<TestContextA> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var testData = modelBuilder.Entity<TestEntityA>();

            testData.ToTable("test_a");

            testData.HasKey(e => e.Id);
            testData.Property(e => e.Value);
        }
    }

    private class TestEntityA : IEntity
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }

    private class TestContextB : DbContext
    {
        public DbSet<TestEntityB> TestData { get; set; }

        public TestContextB(
            DbContextOptions<TestContextB> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var testData = modelBuilder.Entity<TestEntityB>();

            testData.ToTable("test_b");

            testData.HasKey(e => e.Id);
            testData.Property(e => e.Value);
        }
    }

    private class TestEntityB : IEntity
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }

    private class TestContextF : DbContext
    {
        public DbSet<TestEntityF> TestData { get; set; }

        public TestContextF(
            DbContextOptions<TestContextF> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var testData = modelBuilder.Entity<TestEntityF>();

            testData.ToTable("test_f");

            testData.HasKey(e => e.Id);
            testData.Property(e => e.Value);
        }
    }

    private class TestEntityF : IEntity
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }
}
