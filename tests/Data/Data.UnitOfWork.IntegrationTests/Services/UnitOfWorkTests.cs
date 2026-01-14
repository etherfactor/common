using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddDbContext<TestContextA>(opt =>
        {
            opt.UseNpgsql(Setup.PgSqlConnectionString);
        });

        services.AddDbContext<TestContextB>(opt =>
        {
            opt.UseNpgsql(Setup.PgSqlConnectionString);
        });

        services.AddUnitOfWork(opt =>
        {
            opt.BindDbContext<TestContextA>();
            opt.BindDbContext<TestContextB>();
        });

        _rootProvider = services.BuildServiceProvider();

        using var scope = _rootProvider.CreateScope();
        var provider = scope.ServiceProvider;

        using var contextA = provider.GetRequiredService<TestContextA>();
        await contextA.Database.EnsureCreatedAsync();

        using var contextB = provider.GetRequiredService<TestContextB>();
        await contextB.Database.EnsureCreatedAsync();
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
    public void SaveChanges_WithOneContext_ShouldSaveChanges()
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

            testData.HasKey(e => e.Id);
            testData.Property(e => e.Value);
        }
    }

    private class TestEntityB : IEntity
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }
}
