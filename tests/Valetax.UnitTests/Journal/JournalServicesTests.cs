using Valetax.Application.Journal.GetRange;
using Valetax.Application.Journal.GetSingle;
using Valetax.Domain.Entities;
using Valetax.Domain.Exceptions;
using Valetax.UnitTests.Common;

namespace Valetax.UnitTests.Journal;

public sealed class JournalServicesTests
{
    [Fact]
    public async Task GetRangeExecuteAsync_FiltersBySearchAndReturnsCount()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.ExceptionJournals.AddRange(
            new ExceptionJournal
            {
                Id = 1,
                EventId = 1001,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
                ExceptionType = "SecureException",
                Message = "one",
                Text = "alpha problem"
            },
            new ExceptionJournal
            {
                Id = 2,
                EventId = 1002,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                ExceptionType = "Exception",
                Message = "two",
                Text = "beta problem"
            });
        await dbContext.SaveChangesAsync();

        var service = new GetJournalRangeService(dbContext);

        var result = await service.ExecuteAsync(new GetJournalRangeRequest
        {
            Skip = 0,
            Take = 10,
            Search = "beta"
        });
        var item = Assert.Single(result.Items);

        Assert.Equal(1, result.Count);
        Assert.Equal(1002, item.EventId);
    }

    [Fact]
    public async Task GetSingleExecuteAsync_WhenJournalExists_ReturnsEntry()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.ExceptionJournals.Add(new ExceptionJournal
        {
            Id = 1,
            EventId = 2001,
            CreatedAt = DateTimeOffset.UtcNow,
            ExceptionType = "SecureException",
            Message = "message",
            Text = "payload"
        });
        await dbContext.SaveChangesAsync();

        var service = new GetJournalSingleService(dbContext);

        var result = await service.ExecuteAsync(new GetJournalSingleRequest
        {
            Id = 2001
        });

        Assert.NotNull(result);
        Assert.Equal(2001, result!.EventId);
        Assert.Equal("payload", result.Text);
    }

    [Fact]
    public async Task GetSingleExecuteAsync_WhenJournalDoesNotExist_ThrowsSecureException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new GetJournalSingleService(dbContext);

        var exception = await Assert.ThrowsAsync<SecureException>(() => service.ExecuteAsync(new GetJournalSingleRequest
        {
            Id = 9999
        }));

        Assert.Equal("Journal event was not found", exception.Message);
    }
}
