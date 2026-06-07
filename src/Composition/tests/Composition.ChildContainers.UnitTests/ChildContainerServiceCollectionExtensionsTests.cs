using EtherGizmos.Common.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class ChildContainerServiceCollectionExtensionsTests
{
    private IServiceCollection _serviceCollection;

    [SetUp]
    public void SetUp()
    {
        _serviceCollection = new ServiceCollection();
    }

    [Test]
    public void AddChildContainer_InBuilder_ShouldResolveSingletonService()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        }
    }

    [Test]
    public void AddChildContainer_InBuilder_ShouldResolveKeyedSingletonService()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddKeyedSingleton<TestA>(key, (e, _) => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredKeyedService<TestA>(key);
                childServices.AddKeyedSingleton<TestB>(key, (e, _) => new TestB() { Data = testA.Data });
            })
            .ForwardKeyedSingleton<TestB>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredKeyedService<TestA>(key);
        var testB = provider.GetRequiredKeyedService<TestB>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        }
    }

    [Test]
    public void AddChildContainer_InBuilder_ShouldResolveScopedService()
    {
        //Arrange
        _serviceCollection
            .AddScoped<TestA>(e => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredService<TestA>();
                childServices.AddScoped<TestB>(e => new TestB() { Data = testA.Data });
            })
            .ForwardScoped<TestB>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        var testA = provider.GetRequiredService<TestA>();
        var testB = provider.GetRequiredService<TestB>();

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        }
    }

    [Test]
    public void AddChildContainer_InBuilder_ShouldResolveKeyedScopedService()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddKeyedScoped<TestA>(key, (e, _) => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredKeyedService<TestA>(key);
                childServices.AddKeyedScoped<TestB>(key, (e, _) => new TestB() { Data = testA.Data });
            })
            .ForwardKeyedScoped<TestB>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        var testA = provider.GetRequiredKeyedService<TestA>(key);
        var testB = provider.GetRequiredKeyedService<TestB>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        }
    }

    [Test]
    public void AddChildContainer_InBuilder_ShouldResolveTransientService()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        }
    }

    [Test]
    public void AddChildContainer_InBuilder_ShouldResolveKeyedTransientService()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddKeyedTransient<TestA>(key, (e, _) => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredKeyedService<TestA>(key);
                childServices.AddKeyedTransient<TestB>(key, (e, _) => new TestB() { Data = testA.Data });
            })
            .ForwardKeyedTransient<TestB>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredKeyedService<TestA>(key);
        var testB = provider.GetRequiredKeyedService<TestB>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testB, Is.Not.Null);
            Assert.That(testB.Data, Is.EqualTo(testA.Data));
        }
    }

    [Test]
    public void AddChildContainer_InImport_ShouldResolveSingletonServices()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        }
    }

    [Test]
    public void AddChildContainer_InImport_ShouldResolveKeyedSingletonServices()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddKeyedSingleton<Child>(key, (e, _) => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedSingleton<Parent>(key, (e, _) => new Parent(e.GetRequiredKeyedService<Child>(key)));
            })
            .ImportKeyedSingleton<Child>(key)
            .ForwardKeyedSingleton<Parent>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var parent = provider.GetRequiredKeyedService<Parent>(key);
        var child = provider.GetRequiredKeyedService<Child>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        }
    }

    [Test]
    public void AddChildContainer_InImport_ShouldResolveScopedServices()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        }
    }

    [Test]
    public void AddChildContainer_InImport_ShouldResolveKeyedScopedServices()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddKeyedScoped<Child>(key, (e, _) => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedScoped<Parent>(key, (e, _) => new Parent(e.GetRequiredKeyedService<Child>(key)));
            })
            .ImportKeyedScoped<Child>(key)
            .ForwardKeyedScoped<Parent>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider()
            .CreateScope().ServiceProvider;

        var parent = provider.GetRequiredKeyedService<Parent>(key);
        var child = provider.GetRequiredKeyedService<Child>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        }
    }

    [Test]
    public void AddChildContainer_InImport_ShouldResolveTransientServices()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        }
    }

    [Test]
    public void AddChildContainer_InImport_ShouldResolveKeyedTransientServices()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddKeyedTransient<Child>(key, (e, _) => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedTransient<Parent>(key, (e, _) => new Parent(e.GetRequiredKeyedService<Child>(key)));
            })
            .ImportKeyedTransient<Child>(key)
            .ForwardKeyedTransient<Parent>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var parent = provider.GetRequiredKeyedService<Parent>(key);
        var child = provider.GetRequiredKeyedService<Child>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
        }
    }

    [Test]
    public void AddChildContainer_NoForward_ShouldNotResolveServices()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.DoesNotThrow(() => provider.GetRequiredService<TestA>());
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<TestB>());
        }
    }

    [Test]
    public void AddChildContainer_ForwardSingleton_ShouldBeSingleton()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testA_1, Is.Not.Null);
            Assert.That(testA_1, Is.EqualTo(testA_2));
        }
    }

    [Test]
    public void AddChildContainer_ForwardKeyedSingleton_ShouldBeSingleton()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedSingleton<TestA>(key, (e, _) => new TestA() { Data = "Test" });
            })
            .ForwardKeyedSingleton<TestA>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA_1 = provider.GetRequiredKeyedService<TestA>(key);
        var testA_2 = provider.GetRequiredKeyedService<TestA>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testA_1, Is.Not.Null);
            Assert.That(testA_1, Is.EqualTo(testA_2));
        }
    }

    [Test]
    public void AddChildContainer_ForwardScoped_ShouldBeScoped()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testA_1_1, Is.Not.Null);
            Assert.That(testA_1_1, Is.EqualTo(testA_1_2));

            Assert.That(testA_2_1, Is.Not.Null);
            Assert.That(testA_2_1, Is.EqualTo(testA_2_2));

            Assert.That(testA_1_1, Is.Not.EqualTo(testA_2_1));
        }
    }

    [Test]
    public void AddChildContainer_ForwardKeyedScoped_ShouldBeScoped()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedScoped<TestA>(key, (e, _) => new TestA() { Data = "Test" });
            })
            .ForwardKeyedScoped<TestA>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var scope_1 = provider.CreateScope().ServiceProvider;

        var testA_1_1 = scope_1.GetRequiredKeyedService<TestA>(key);
        var testA_1_2 = scope_1.GetRequiredKeyedService<TestA>(key);

        var scope_2 = provider.CreateScope().ServiceProvider;

        var testA_2_1 = scope_2.GetRequiredKeyedService<TestA>(key);
        var testA_2_2 = scope_2.GetRequiredKeyedService<TestA>(key);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testA_1_1, Is.Not.Null);
            Assert.That(testA_1_1, Is.EqualTo(testA_1_2));

            Assert.That(testA_2_1, Is.Not.Null);
            Assert.That(testA_2_1, Is.EqualTo(testA_2_2));

            Assert.That(testA_1_1, Is.Not.EqualTo(testA_2_1));
        }
    }

    [Test]
    public void AddChildContainer_ForwardTransient_ShouldBeTransient()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testA_1, Is.Not.Null);
            Assert.That(testA_1, Is.Not.EqualTo(testA_2));
        }
    }

    [Test]
    public void AddChildContainer_ForwardKeyedTransient_ShouldBeTransient()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedTransient<TestA>(key, (e, _) => new TestA() { Data = "Test" });
            })
            .ForwardKeyedTransient<TestA>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA_1 = provider.GetRequiredKeyedService<TestA>(key);
        var testA_2 = provider.GetRequiredKeyedService<TestA>(key);
        
        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testA_1, Is.Not.Null);
            Assert.That(testA_1, Is.Not.EqualTo(testA_2));
        }
    }

    [Test]
    public void AddChildContainer_RecursiveServices_ShouldThrowCircularDependencyException()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<CircularDependencyException>(() =>
            {
                provider.GetRequiredService<Parent>();
            });

            Assert.Throws<CircularDependencyException>(() =>
            {
                provider.GetRequiredService<Child>();
            });
        }
    }

    [Test]
    public void AddChildContainer_NestedContainers_ShouldResolveServices()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testA, Is.Not.Null);
            Assert.That(testB, Is.Not.Null);
            Assert.That(testC, Is.Not.Null);
            Assert.That(testC.Data, Is.EqualTo("Parent - Child2"));
        }
    }

    [Test]
    public void AddChildContainer_ImportMultipleServices_ShouldResolveServices()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(wrapperA, Is.Not.Null);
            Assert.That(wrapperA.TestA, Is.Not.Null);
            Assert.That(wrapperA.TestA.Data, Is.EqualTo("TestA"));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wrapperB, Is.Not.Null);
            Assert.That(wrapperB.TestB, Is.Not.Null);
            Assert.That(wrapperB.TestB.Data, Is.EqualTo("TestB"));
        }
    }

    [Test]
    public void AddChildContainer_ServiceReplacement_ShouldResolveReplacedService()
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
    public void AddChildContainer_ConditionalRegistration_ShouldResolveServices(bool condition, bool isInvalid)
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
    public void AddChildContainer_CreateScopeWithinChild_ShouldResolveParentService()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(childService, Is.Not.Null);
            Assert.That(parentService, Is.Not.Null);
        }
    }

    [Test]
    public void AddChildContainer_ForwardKeyedScoped_WithUnkeyedChildAndKeyedParent_ShouldResolveService()
    {
        //Arrange
        var parentKey = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<TestA>(_ => new TestA { Data = "Child" });
            })
            .ForwardKeyedScoped<TestA>(
                childServiceKey: null,
                parentServiceKey: parentKey);

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        var testA = provider.GetRequiredKeyedService<TestA>(parentKey);

        //Assert
        Assert.That(testA.Data, Is.EqualTo("Child"));
    }

    [Test]
    public void AddChildContainer_ForwardKeyedScoped_WithKeyedChildAndUnkeyedParent_ShouldResolveService()
    {
        //Arrange
        var childKey = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedScoped<TestA>(childKey, (_, _) => new TestA { Data = "Child" });
            })
            .ForwardKeyedScoped<TestA>(
                childServiceKey: childKey,
                parentServiceKey: null);

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        var testA = provider.GetRequiredService<TestA>();

        //Assert
        Assert.That(testA.Data, Is.EqualTo("Child"));
    }

    [Test]
    public void AddChildContainer_ForwardKeyedScoped_WithDifferentKeys_ShouldResolveUsingParentKey()
    {
        //Arrange
        var childKey = new TestKey("child");
        var parentKey = new TestKey("parent");

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedScoped<TestA>(childKey, (_, _) => new TestA { Data = "Child" });
            })
            .ForwardKeyedScoped<TestA>(
                childServiceKey: childKey,
                parentServiceKey: parentKey);

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredKeyedService<TestA>(parentKey).Data, Is.EqualTo("Child"));
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<TestA>(childKey));
        }
    }

    [Test]
    public void AddChildContainer_ImportKeyedScoped_WithUnkeyedParentAndKeyedChild_ShouldResolveService()
    {
        //Arrange
        var childKey = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddScoped<Child>(_ => new Child { Name = "Parent" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<Parent>(sp =>
                    new Parent(sp.GetRequiredKeyedService<Child>(childKey)));
            })
            .ImportKeyedScoped<Child>(
                parentServiceKey: null,
                childServiceKey: childKey)
            .ForwardScoped<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        var parent = provider.GetRequiredService<Parent>();

        //Assert
        Assert.That(parent.Child.Name, Is.EqualTo("Parent"));
    }

    [Test]
    public void AddChildContainer_ImportKeyedScoped_WithKeyedParentAndUnkeyedChild_ShouldResolveService()
    {
        //Arrange
        var parentKey = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddKeyedScoped<Child>(parentKey, (_, _) => new Child { Name = "Parent" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<Parent>();
            })
            .ImportKeyedScoped<Child>(
                parentServiceKey: parentKey,
                childServiceKey: null)
            .ForwardScoped<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        var parent = provider.GetRequiredService<Parent>();

        //Assert
        Assert.That(parent.Child.Name, Is.EqualTo("Parent"));
    }

    [Test]
    public void AddChildContainer_MultipleChildrenForwardSameServiceTypeWithDifferentKeys_ShouldResolveCorrectService()
    {
        //Arrange
        var keyA = new TestKey("A");
        var keyB = new TestKey("B");

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<TestA>(_ => new TestA { Data = "A" });
            })
            .ForwardKeyedScoped<TestA>(null, keyA);

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<TestA>(_ => new TestA { Data = "B" });
            })
            .ForwardKeyedScoped<TestA>(null, keyB);

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        var a = provider.GetRequiredKeyedService<TestA>(keyA);
        var b = provider.GetRequiredKeyedService<TestA>(keyB);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.Data, Is.EqualTo("A"));
            Assert.That(b.Data, Is.EqualTo("B"));
        }
    }

    [Test]
    public void AddChildContainer_ForwardKeyedScoped_WithWrongParentKey_ShouldThrow()
    {
        //Arrange
        var childKey = new TestKey("child");
        var parentKey = new TestKey("parent");
        var wrongKey = new TestKey("wrong");

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedScoped<TestA>(childKey, (_, _) => new TestA { Data = "Child" });
            })
            .ForwardKeyedScoped<TestA>(childKey, parentKey);

        //Act
        var provider = _serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;

        //Assert
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredKeyedService<TestA>(wrongKey));
    }

    [Test]
    public void AddChildContainer_RecursiveKeyedServices_ShouldThrowCircularDependencyException()
    {
        //Arrange
        var key = new TestKey(Guid.NewGuid().ToString());

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedSingleton<Parent>(key, (sp, _) =>
                    new Parent(sp.GetRequiredKeyedService<Child>(key)));
            })
            .ImportKeyedSingleton<Child>(key)
            .ForwardKeyedSingleton<Parent>(key);

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddKeyedSingleton<Child>(key, (sp, _) =>
                    new RecursiveChild(sp.GetRequiredKeyedService<Parent>(key)));
            })
            .ImportKeyedSingleton<Parent>(key)
            .ForwardKeyedSingleton<Child>(key);

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        //Assert
        Assert.Throws<CircularDependencyException>(() =>
            provider.GetRequiredKeyedService<Parent>(key));
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

    private record TestKey(string Key);
}
