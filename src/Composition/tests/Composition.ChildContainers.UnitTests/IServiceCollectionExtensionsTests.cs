using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class IServiceCollectionExtensionsTests
{
    private IServiceCollection _serviceCollection;

    [SetUp]
    public void SetUp()
    {
        _serviceCollection = new ServiceCollection();
    }

    [Test]
    public void AddChildContainer_InBuilder_ResolvesSingletonService()
    {
        //Arrange
        _serviceCollection
            .AddSingleton<TestA>(e => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredService<TestA>();
                childServices.AddSingleton<TestB>(e => new TestB() { Data = testA.Data });
            })
            .ForwardSingleton<TestB>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredService<TestA>();
        var testB = provider.GetRequiredService<TestB>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        });
    }

    [Test]
    public void AddChildContainer_InBuilder_ResolvesTransientService()
    {
        //Arrange
        _serviceCollection
            .AddTransient<TestA>(e => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredService<TestA>();
                childServices.AddTransient<TestB>(e => new TestB() { Data = testA.Data });
            })
            .ForwardTransient<TestB>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredService<TestA>();
        var testB = provider.GetRequiredService<TestB>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        });
    }

    [Test]
    public void AddChildContainer_InImport_ResolvesSingletonServices()
    {
        //Arrange
        _serviceCollection
            .AddSingleton<Child>(e => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<Parent>();
            })
            .ImportSingleton<Child>()
            .ForwardSingleton<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var parent = provider.GetRequiredService<Parent>();
        var child = provider.GetRequiredService<Child>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        });
    }

    [Test]
    public void AddChildContainer_InImport_ResolvesScopedServices()
    {
        //Arrange
        _serviceCollection
            .AddScoped<Child>(e => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<Parent>();
            })
            .ImportScoped<Child>()
            .ForwardScoped<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider()
            .CreateScope().ServiceProvider;

        var parent = provider.GetRequiredService<Parent>();
        var child = provider.GetRequiredService<Child>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        });
    }

    [Test]
    public void AddChildContainer_InImport_ResolvesTransientServices()
    {
        //Arrange
        _serviceCollection
            .AddTransient<Child>(e => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<Parent>();
            })
            .ImportTransient<Child>()
            .ForwardTransient<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var parent = provider.GetRequiredService<Parent>();
        var child = provider.GetRequiredService<Child>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        });
    }

    [Test]
    public void AddChildContainer_NoForward_DoesNotResolveServices()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<TestA>(e => new TestA() { Data = "Test" });
                childServices.AddTransient<TestB>(e => new TestB() { Data = "Test" });
            })
            .ForwardTransient<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => provider.GetRequiredService<TestA>());
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<TestB>());
        });
    }

    [Test]
    public void AddChildContainer_ForwardSingleton_IsSingleton()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<TestA>(e => new TestA() { Data = "Test" });
            })
            .ForwardSingleton<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA_1 = provider.GetRequiredService<TestA>();
        var testA_2 = provider.GetRequiredService<TestA>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(testA_1, Is.Not.Null);
            Assert.That(testA_1, Is.EqualTo(testA_2));
        });
    }

    [Test]
    public void AddChildContainer_ForwardScoped_IsScoped()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<TestA>(e => new TestA() { Data = "Test" });
            })
            .ForwardScoped<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var scope_1 = provider.CreateScope().ServiceProvider;

        var testA_1_1 = scope_1.GetRequiredService<TestA>();
        var testA_1_2 = scope_1.GetRequiredService<TestA>();

        var scope_2 = provider.CreateScope().ServiceProvider;

        var testA_2_1 = scope_2.GetRequiredService<TestA>();
        var testA_2_2 = scope_2.GetRequiredService<TestA>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(testA_1_1, Is.Not.Null);
            Assert.That(testA_1_1, Is.EqualTo(testA_1_2));

            Assert.That(testA_2_1, Is.Not.Null);
            Assert.That(testA_2_1, Is.EqualTo(testA_2_2));

            Assert.That(testA_1_1, Is.Not.EqualTo(testA_2_1));
        });
    }

    [Test]
    public void AddChildContainer_ForwardTransient_IsTransient()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<TestA>(e => new TestA() { Data = "Test" });
            })
            .ForwardTransient<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA_1 = provider.GetRequiredService<TestA>();
        var testA_2 = provider.GetRequiredService<TestA>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(testA_1, Is.Not.Null);
            Assert.That(testA_1, Is.Not.EqualTo(testA_2));
        });
    }

    [Test]
    public void AddChildContainer_RecursiveServices_ThrowsCircularDependencyException()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<Parent>();
            })
            .ImportSingleton<Child>()
            .ForwardSingleton<Parent>();

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
             {
                 childServices.AddSingleton<Child, RecursiveChild>();
             })
            .ImportSingleton<Parent>()
            .ForwardSingleton<Child>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.Throws<CircularDependencyException>(() =>
            {
                provider.GetRequiredService<Parent>();
            });

            Assert.Throws<CircularDependencyException>(() =>
            {
                provider.GetRequiredService<Child>();
            });
        });
    }

    [Test]
    public void AddChildContainer_NestedContainers_ResolvesServices()
    {
        //Arrange
        _serviceCollection
            .AddSingleton<TestA>(e => new TestA() { Data = "Parent" })
            .AddChildContainer((childServices1, parentServices1) =>
            {
                childServices1.AddSingleton<TestB>(e => new TestB() { Data = "Child1" })
                    .AddChildContainer((childServices2, parentServices2) =>
                    {
                        var testA = parentServices2.GetRequiredService<TestA>();
                        childServices2.AddSingleton<TestC>(e => new TestC() { Data = testA.Data + " - Child2" });
                    })
                    .ForwardSingleton<TestC>();
            })
            .ImportSingleton<TestA>()
            .ForwardSingleton<TestB>()
            .ForwardSingleton<TestC>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredService<TestA>();
        var testB = provider.GetRequiredService<TestB>();
        var testC = provider.GetRequiredService<TestC>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(testA, Is.Not.Null);
            Assert.That(testB, Is.Not.Null);
            Assert.That(testC, Is.Not.Null);
            Assert.That(testC.Data, Is.EqualTo("Parent - Child2"));
        });
    }

    [Test]
    public void AddChildContainer_ImportMultipleServices_ResolvesServices()
    {
        //Arrange
        _serviceCollection
            .AddTransient<TestA>(e => new TestA() { Data = "TestA" })
            .AddTransient<TestB>(e => new TestB() { Data = "TestB" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<WrapperA>();
                childServices.AddTransient<WrapperB>();
            })
            .ImportTransient<TestA>()
            .ImportTransient<TestB>()
            .ForwardTransient<WrapperA>()
            .ForwardTransient<WrapperB>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var wrapperA = provider.GetRequiredService<WrapperA>();
        var wrapperB = provider.GetRequiredService<WrapperB>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(wrapperA, Is.Not.Null);
            Assert.That(wrapperA.TestA, Is.Not.Null);
            Assert.That(wrapperA.TestA.Data, Is.EqualTo("TestA"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(wrapperB, Is.Not.Null);
            Assert.That(wrapperB.TestB, Is.Not.Null);
            Assert.That(wrapperB.TestB.Data, Is.EqualTo("TestB"));
        });
    }

    [Test]
    public void AddChildContainer_ServiceReplacement_ResolvesReplacedService()
    {
        //Arrange
        _serviceCollection
            .AddSingleton<TestA>(e => new TestA() { Data = "Parent" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<TestA>(e => new TestA() { Data = "Child" });
            })
            .ForwardSingleton<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredService<TestA>();

        //Assert
        Assert.That(testA, Is.Not.Null);
        Assert.That(testA.Data, Is.EqualTo("Child"));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void AddChildContainer_ConditionalRegistration_ResolvesServices(bool condition, bool isInvalid)
    {
        //Arrange
        _serviceCollection
            .AddSingleton<TestA>(e => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                if (condition)
                {
                    childServices.AddSingleton<TestB>();
                }
            })
            .ForwardSingleton<TestB>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredService<TestA>();

        //Assert
        Assert.That(testA, Is.Not.Null);

        if (condition)
        {
            Assert.DoesNotThrow(() =>
            {
                var testB = provider.GetService<TestB>();
            });
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var testB = provider.GetService<TestB>();
            });
        }
    }

    [Test]
    public void AddChildContainer_CreateScopeWithinChild_ResolvesParentService()
    {
        //Arrange
        _serviceCollection
            .AddScoped<Parent>()
            .AddScoped<Child>()
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<NestedService>();
            })
            .ImportScoped<Parent>()
            .ForwardSingleton<NestedService>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var scope = provider.CreateScope().ServiceProvider;
        var childService = scope.GetRequiredService<NestedService>();

        var childScope = childService.CreateScopedProvider();
        var parentService = childScope.GetRequiredService<Parent>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(childService, Is.Not.Null);
            Assert.That(parentService, Is.Not.Null);
        });
    }

    private class TestA
    {
        public string Data { get; set; } = null!;
    }

    private class WrapperA
    {
        public TestA TestA { get; set; }

        public WrapperA(TestA testA)
        {
            TestA = testA;
        }
    }

    private class TestB
    {
        public string Data { get; set; } = null!;
    }

    private class WrapperB
    {
        public TestB TestB { get; set; }

        public WrapperB(TestB testB)
        {
            TestB = testB;
        }
    }

    private class TestC
    {
        public string Data { get; set; } = null!;
    }

    private class Parent
    {
        public Child Child { get; }

        public Parent(Child child)
        {
            Child = child;
        }
    }

    private class Child
    {
        public string Name { get; set; } = null!;
    }

    private class RecursiveChild : Child
    {
        public Parent Parent { get; }

        public RecursiveChild(Parent parent)
        {
            Parent = parent;
        }
    }

    private class NestedService
    {
        private readonly IServiceProvider _serviceProvider;

        public NestedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceProvider CreateScopedProvider()
        {
            return _serviceProvider.CreateScope().ServiceProvider;
        }
    }
}
