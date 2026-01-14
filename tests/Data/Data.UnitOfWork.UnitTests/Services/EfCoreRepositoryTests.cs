using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class EfCoreRepositoryTests
{
    private IServiceProvider _rootProvider;
    private IServiceProvider _scopeProvider;
    private EfCoreRepository<TestEntity> _repository;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        services.AddDbContext<TestContext>(opt =>
        {
            opt.UseInMemoryDatabase(nameof(EfCoreRepositoryTests));
        });

        services.AddUnitOfWork(opt =>
        {
            opt.BindDbContext<TestContext>();
        });

        _rootProvider = services.BuildServiceProvider();
        _scopeProvider = _rootProvider.CreateScope().ServiceProvider;

        _repository = new EfCoreRepository<TestEntity>(_scopeProvider, _scopeProvider.GetRequiredService<TestContext>().TestData);
    }

    [TearDown]
    public void TearDown()
    {
        if (_rootProvider is IDisposable disposableRoot)
            disposableRoot.Dispose();

        if (_scopeProvider is IDisposable disposableScope)
            disposableScope.Dispose();
    }

    [Test]
    public void Add_WhenCalled_ShouldAddEntity()
    {
        //Arrange
        var set = _scopeProvider.GetRequiredService<TestContext>().TestData;

        var entity = new TestEntity()
        {
            Value = "value"
        };

        var entry = set.Entry(entity);

        //Act
        _repository.Add(entity);

        //Assert
        Assert.That(entry.State, Is.EqualTo(EntityState.Added));
    }

    [Test]
    public async Task Remove_WhenCalled_ShouldRemoveEntity()
    {
        //Arrange
        var context = _scopeProvider.GetRequiredService<TestContext>();
        var set = context.TestData;

        var entity = new TestEntity()
        {
            Value = "value"
        };

        set.Add(entity);
        await context.SaveChangesAsync();

        var entry = set.Entry(entity);

        //Act
        _repository.Remove(entity);

        //Assert
        Assert.That(entry.State, Is.EqualTo(EntityState.Deleted));
    }

    [Test]
    public async Task ReloadAsync_WhenExists_ShouldReloadEntity()
    {
        //Arrange
        var context = _scopeProvider.GetRequiredService<TestContext>();
        var set = context.TestData;

        var entity = new TestEntity()
        {
            Value = "value"
        };

        set.Add(entity);
        await context.SaveChangesAsync();

        //Act
        var copy = await _repository.ReloadAsync(entity);

        //Assert
        Assert.That(copy.Id, Is.EqualTo(entity.Id));
        Assert.That(copy.Value, Is.EqualTo(entity.Value));
    }

    private class TestContext : DbContext
    {
        public DbSet<TestEntity> TestData { get; set; }

        public TestContext(
            DbContextOptions<TestContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var testData = modelBuilder.Entity<TestEntity>();

            testData.HasKey(e => e.Id);
            testData.Property(e => e.Value);
        }
    }

    private class TestEntity : IEntity
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }
}
