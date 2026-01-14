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

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(@interface =>
            @interface.HttpContext)
            .Returns(() => new DefaultHttpContext()
            {
                RequestServices = _serviceProvider,
            });

        services.AddSingleton(accessorMock.Object);

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
    public void Create_WithRequestTrue_ShouldUseRequestScope()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(useRequestScope: true);

        //Assert
        Assert.That(uow.Services, Is.EqualTo(_serviceProvider));
    }

    [Test]
    public void Create_WithRequestFalse_ShouldUseNewScope()
    {
        //Arrange & Act
        using var uow = _uowFactory.Create(useRequestScope: false);

        //Assert
        Assert.That(uow.Services, Is.Not.EqualTo(_serviceProvider));
    }
}
