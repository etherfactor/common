using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using System.Runtime.CompilerServices;

namespace EtherGizmos.Common.Services;

internal class NotificationSaveChangesInterceptorTests
{
    private NotificationSaveChangesInterceptor _interceptor;
    private Mock<IUnitOfWorkFactory> _uowFactoryMock;
    private Mock<IUnitOfWorkAccessor> _uowAccessorMock;
    private Mock<IUnitOfWork> _ownedUowMock;
    private RecordingDomainEventEmitter _eventEmitter;
    private TestDomainEventExtractor _extractor;
    private TestNotificationInterceptorDbContext _context;
    private string _connectionString;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connectionString = await Setup.CreateDatabase("save_changes_interceptor");
    }

    [SetUp]
    public async Task SetUp()
    {
        _uowFactoryMock = new();
        _uowAccessorMock = new();
        _ownedUowMock = new();
        _eventEmitter = new();
        _extractor = new();

        _uowFactoryMock
            .Setup(e => e.Create())
            .Returns(_ownedUowMock.Object);

        _uowAccessorMock
            .SetupGet(e => e.Current)
            .Returns((IUnitOfWork?)null);

        _interceptor = new NotificationSaveChangesInterceptor(
            _uowFactoryMock.Object,
            _uowAccessorMock.Object,
            [_extractor],
            _eventEmitter);

        var options = new DbContextOptionsBuilder<TestNotificationInterceptorDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(_interceptor)
            .EnableSensitiveDataLogging()
            .Options;

        _context = new TestNotificationInterceptorDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task SaveChangesAsync_WhenTrackedEntityMatchesExtractor_ShouldEmitExtractedEvents()
    {
        //Arrange
        var entity = new TestTrackedEntity
        {
            Name = "abc",
        };

        _context.Entities.Add(entity);

        //Act
        await _context.SaveChangesAsync();

        //Assert
        Assert.That(_eventEmitter.Emissions, Has.Count.EqualTo(1));

        var emission = _eventEmitter.Emissions[0];
        Assert.Multiple(() =>
        {
            Assert.That(emission.Event, Is.TypeOf<TestDomainEvent>());
            Assert.That(emission.Audiences, Has.Count.EqualTo(1));
            Assert.That(emission.Audiences[0].Kind, Is.EqualTo("$self"));
            Assert.That(emission.Audiences[0].Id, Is.EqualTo("user-1"));
            Assert.That(emission.Options, Is.Null);
        });
    }

    [Test]
    public void SaveChanges_WhenTrackedEntityMatchesExtractor_ShouldEmitExtractedEvents()
    {
        //Arrange
        var entity = new TestTrackedEntity
        {
            Name = "abc",
        };

        _context.Entities.Add(entity);

        //Act
        _context.SaveChanges();

        //Assert
        Assert.That(_eventEmitter.Emissions, Has.Count.EqualTo(1));

        var emission = _eventEmitter.Emissions[0];
        Assert.Multiple(() =>
        {
            Assert.That(emission.Event, Is.TypeOf<TestDomainEvent>());
            Assert.That(emission.Audiences, Has.Count.EqualTo(1));
            Assert.That(emission.Audiences[0].Kind, Is.EqualTo("$self"));
            Assert.That(emission.Audiences[0].Id, Is.EqualTo("user-1"));
            Assert.That(emission.Options, Is.Null);
        });
    }

    [Test]
    public async Task SaveChangesAsync_WhenNoAmbientUnitOfWorkExists_ShouldCreateSaveAndDisposeOwnedUnitOfWork()
    {
        //Arrange
        _uowAccessorMock
            .SetupGet(e => e.Current)
            .Returns((IUnitOfWork?)null);

        _context.Entities.Add(new TestTrackedEntity
        {
            Name = "abc",
        });

        //Act
        await _context.SaveChangesAsync();

        //Assert
        _uowFactoryMock.Verify(e => e.Create(), Times.Once);
        _ownedUowMock.Verify(e => e.SaveChanges(), Times.Once);
        _ownedUowMock.Verify(e => e.Dispose(), Times.Once);
    }

    [Test]
    public async Task SaveChangesAsync_WhenAmbientUnitOfWorkExists_ShouldUseAmbientUnitOfWorkWithoutCreatingOrDisposingOwnedUnitOfWork()
    {
        //Arrange
        var ambientUowMock = new Mock<IUnitOfWork>();

        _uowAccessorMock
            .SetupGet(e => e.Current)
            .Returns(ambientUowMock.Object);

        _context.Entities.Add(new TestTrackedEntity
        {
            Name = "abc",
        });

        //Act
        await _context.SaveChangesAsync();

        //Assert
        _uowFactoryMock.Verify(e => e.Create(), Times.Never);
        ambientUowMock.Verify(e => e.SaveChanges(), Times.Never);
        ambientUowMock.Verify(e => e.Dispose(), Times.Never);
        _ownedUowMock.Verify(e => e.SaveChanges(), Times.Never);
        _ownedUowMock.Verify(e => e.Dispose(), Times.Never);
    }

    [Test]
    public async Task SaveChangesAsync_WhenMultipleExtractorsExist_ShouldEmitEventsFromMatchingExtractorOnly()
    {
        //Arrange
        var nonMatchingExtractor = new NonMatchingDomainEventExtractor();

        _interceptor = new NotificationSaveChangesInterceptor(
            _uowFactoryMock.Object,
            _uowAccessorMock.Object,
            [nonMatchingExtractor, _extractor],
            _eventEmitter);

        var options = new DbContextOptionsBuilder<TestNotificationInterceptorDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(_interceptor)
            .EnableSensitiveDataLogging()
            .Options;

        await _context.DisposeAsync();
        _context = new TestNotificationInterceptorDbContext(options);

        _context.Entities.Add(new TestTrackedEntity
        {
            Name = "abc",
        });

        //Act
        await _context.SaveChangesAsync();

        //Assert
        Assert.That(_eventEmitter.Emissions, Has.Count.EqualTo(1));
        Assert.That(_eventEmitter.Emissions[0].Event, Is.TypeOf<TestDomainEvent>());
    }

    private sealed class TestNotificationInterceptorDbContext : DbContext
    {
        public TestNotificationInterceptorDbContext(DbContextOptions<TestNotificationInterceptorDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestTrackedEntity> Entities => Set<TestTrackedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestTrackedEntity>(entity =>
            {
                entity.ToTable("test_notification_interceptor_entities");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired(false);
            });
        }
    }

    private sealed class TestTrackedEntity
    {
        public long Id { get; set; }

        public string? Name { get; set; }
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
        public string Value { get; set; } = "value";
    }

    private sealed class TestDomainEventExtractor : IDomainEventExtractor
    {
        public bool CanHandle(EntityEntry entry)
            => entry.Entity is TestTrackedEntity;

        public async IAsyncEnumerable<DomainEventEmission> ExtractAsync(
            EntityEntry entry,
            IUnitOfWork unitOfWork,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new DomainEventEmission(
                new TestDomainEvent(),
                [new AudienceKey("$self", "user-1")]);
        }
    }

    private sealed class NonMatchingDomainEventExtractor : IDomainEventExtractor
    {
        public bool CanHandle(EntityEntry entry)
            => false;

        public async IAsyncEnumerable<DomainEventEmission> ExtractAsync(
            EntityEntry entry,
            IUnitOfWork unitOfWork,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }
    }

    private sealed class RecordingDomainEventEmitter : IDomainEventEmitter
    {
        public List<RecordedEmission> Emissions { get; } = [];

        public Task EmitAsync(
            IDomainEvent @event,
            IEnumerable<AudienceKey> audiences,
            DomainEventEmissionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Emissions.Add(new RecordedEmission(
                @event,
                [.. audiences],
                options));

            return Task.CompletedTask;
        }
    }

    private sealed record RecordedEmission(
        IDomainEvent Event,
        IReadOnlyList<AudienceKey> Audiences,
        DomainEventEmissionOptions? Options);
}
