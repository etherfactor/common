using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Services;

internal class DigestRouterTests
{
    private DigestRouter<TestDomainEvent> _router;

    [SetUp]
    public void SetUp()
    {
        _router = new();
    }

    [Test]
    public async Task FilterScopeAsync_WhenAudienceContainsUser_ShouldReturnUser()
    {
        //Arrange
        var digest = new Digest<TestDomainEvent>();

        var audiences = new[]
        {
            new AudienceKey("$self", "user-1"),
        };

        var userIds = new[]
        {
            "user-1",
        };

        //Act
        var result = await _router.FilterScopeAsync(digest, audiences, userIds).ToListAsync();

        //Assert
        Assert.That(result, Is.EqualTo(["user-1"]));
    }

    [Test]
    public async Task FilterScopeAsync_WhenAudienceDoesNotContainUser_ShouldReturnEmpty()
    {
        //Arrange
        var digest = new Digest<TestDomainEvent>();

        var audiences = new[]
        {
            new AudienceKey("$self", "user-2"),
        };

        var userIds = new[]
        {
            "user-1",
        };

        //Act
        var result = await _router.FilterScopeAsync(digest, audiences, userIds).ToListAsync();

        //Assert
        Assert.That(result, Is.EqualTo(Enumerable.Empty<string>()));
    }

    [Test]
    public async Task FilterScopeAsync_WhenAudienceContainsDuplicates_ShouldDeduplicate()
    {
        //Arrange
        var digest = new Digest<TestDomainEvent>();

        var audiences = new[]
        {
            new AudienceKey("$self", "user-1"),
            new AudienceKey("$self", "user-1"),
        };

        var userIds = new[]
        {
            "user-1",
            "user-1",
        };

        //Act
        var result = await _router.FilterScopeAsync(digest, audiences, userIds).ToListAsync();

        //Assert
        Assert.That(result, Is.EqualTo(["user-1"]));
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
    }
}
