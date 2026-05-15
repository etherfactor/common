using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace EtherGizmos.Common.Services;

internal class UnitOfWorkFactoryTests
{
    private IServiceProvider _serviceProvider;
    private UnitOfWorkFactory _uowFactory;
    private UnitOfWorkAccessor _uowAccessor;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        var httpAccessorMock = new Mock<IHttpContextAccessor>();
        httpAccessorMock.Setup(@interface =>
            @interface.HttpContext)
            .Returns(() => new DefaultHttpContext()
            {
                RequestServices = _serviceProvider,
            });

        services.AddSingleton(httpAccessorMock.Object);

        _uowAccessor = new();
        services.AddSingleton<IUnitOfWorkAccessor>(_uowAccessor);

        _serviceProvider = services.BuildServiceProvider();
        _uowFactory = new(new Mock<IOptions<UnitOfWorkOptions>>().Object, _serviceProvider);
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    public void Create_WithNoArguments_ShouldUseNewScope()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create();

        //Assert
        Assert.That(uow.Services, Is.Not.EqualTo(_serviceProvider));
    }

    [Test]
    public void Create_WithScope_ShouldUseExistingScope()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(_serviceProvider);

        //Assert
        Assert.That(uow.Services, Is.EqualTo(_serviceProvider));
    }

    [Test]
    public void Create_WithScopeModeRequestScope_ShouldUseRequestScope()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(new()
        {
            SccopeMode = UnitOfWorkScopeMode.RequestScope,
        });

        //Assert
        Assert.That(uow.Services, Is.EqualTo(_serviceProvider));
    }

    [Test]
    public void Create_WithScopeModeNewScope_ShouldUseNewScope()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(new()
        {
            SccopeMode = UnitOfWorkScopeMode.NewScope,
        });

        //Assert
        Assert.That(uow.Services, Is.Not.EqualTo(_serviceProvider));
    }

    [Test]
    public void Create_WithAmbientModeCreateNewAndSetAmbient_ShouldSetAmbient()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(new()
        {
            AmbientMode = UnitOfWorkAmbientMode.CreateAmbient,
        });

        //Assert
        Assert.That(_uowAccessor.Current, Is.EqualTo(uow));
    }

    [Test]
    public void Create_WithAmbientModeJoinOrCreateAmbientWithAmbient_ShouldWrapAmbient()
    {
        //Arrange & Act
        using var ambient = _uowFactory.Create();
        using var uow = _uowFactory.Create(new()
        {
            AmbientMode = UnitOfWorkAmbientMode.JoinOrCreateAmbient,
        });

        //Assert
        var current = _uowAccessor.Current;
        Assert.That(current, Is.Not.EqualTo(uow));
        Assert.That((uow as UnitOfWorkReference)?.Inner, Is.EqualTo(ambient));
    }

    [Test]
    public void Create_WithAmbientModeJoinOrCreateAmbientWithoutAmbient_ShouldSetAmbient()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(new()
        {
            AmbientMode = UnitOfWorkAmbientMode.CreateAmbient,
        });

        //Assert
        Assert.That(_uowAccessor.Current, Is.EqualTo(uow));
    }

    [Test]
    public void Create_WithAmbientModeRequireAmbientWithAmbient_ShouldWrapAmbient()
    {
        //Arrange & Act
        using var ambient = _uowFactory.Create();
        using var uow = _uowFactory.Create(new()
        {
            AmbientMode = UnitOfWorkAmbientMode.RequireAmbient,
        });

        //Assert
        var current = _uowAccessor.Current;
        Assert.That(current, Is.Not.EqualTo(uow));
        Assert.That((uow as UnitOfWorkReference)?.Inner, Is.EqualTo(ambient));
    }

    [Test]
    public void Create_WithAmbientModeRequireAmbientWithoutAmbient_ShouldThrowInvalidOperationException()
    {
        //Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var uow = _uowFactory.Create(new()
            {
                AmbientMode = UnitOfWorkAmbientMode.RequireAmbient,
            });
        });
    }

    [Test]
    public void Create_WithAmbientModeSuppressAmbient_ShouldNotSetAmbient()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(new()
        {
            AmbientMode = UnitOfWorkAmbientMode.SuppressAmbient,
        });

        //Assert
        Assert.That(_uowAccessor.Current, Is.Null);
    }

    [Test]
    public void Create_WithAmbientAndDispose_ShouldNotDisposeAmbient()
    {
        //Arrange
        using var ambient = _uowFactory.Create();
        using var uow = _uowFactory.Create(new()
        {
            AmbientMode = UnitOfWorkAmbientMode.RequireAmbient,
        });

        //Act
        uow.Dispose();

        //Assert
        var current = _uowAccessor.Current!;
        Assert.DoesNotThrow(() =>
        {
            current.SaveChanges();
        });
    }

    [Test]
    public void Create_WithFirstAndDisposeThenNewAmbient_ShouldReturnNewAmbient()
    {
        //Arrange
        using var first = _uowFactory.Create();
        var firstAccessor = _uowAccessor.Current;
        first.Dispose();

        //Act
        using var ambient = _uowFactory.Create();
        using var uow = _uowFactory.Create(new()
        {
            AmbientMode = UnitOfWorkAmbientMode.RequireAmbient,
        });
        var secondAccessor = _uowAccessor.Current;

        //Assert
        Assert.That(secondAccessor, Is.Not.EqualTo(firstAccessor));
    }
}
