using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valetax.Application.Journal.GetRange;
using Valetax.Application.Journal.GetSingle;
using Valetax.Api.Contracts;

namespace Valetax.Api.Controllers;

[ApiController]
[Authorize]
[Tags("user.journal")]
public class JournalController(
    IGetJournalRangeService getJournalRangeService,
    IGetJournalSingleService getJournalSingleService) : ControllerBase
{
    /// <summary>
    /// Provides the pagination API for journal records.
    /// </summary>
    /// <remarks>
    /// Skip means the number of items skipped by the server. Take means the maximum number of items returned by the server.
    /// All fields of the filter are optional.
    /// </remarks>
    [HttpPost("/api.user.journal.getRange")]
    [ProducesResponseType(typeof(PagedResponse<JournalInfoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<JournalInfoResponse>>> GetRange(
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromBody] JournalFilterRequest? filter,
        CancellationToken cancellationToken)
    {
        var result = await getJournalRangeService.ExecuteAsync(new GetJournalRangeRequest
        {
            Skip = skip,
            Take = take,
            From = filter?.From,
            To = filter?.To,
            Search = filter?.Search
        }, cancellationToken);

        return Ok(new PagedResponse<JournalInfoResponse>
        {
            Skip = result.Skip,
            Count = result.Count,
            Items = result.Items
                .Select(item => new JournalInfoResponse
                {
                    Id = item.Id,
                    EventId = item.EventId,
                    CreatedAt = item.CreatedAt
                })
                .ToList()
        });
    }

    /// <summary>
    /// Returns the information about a particular event by identifier.
    /// </summary>
    [HttpPost("/api.user.journal.getSingle")]
    [ProducesResponseType(typeof(JournalResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JournalResponse>> GetSingle([FromQuery] long id, CancellationToken cancellationToken)
    {
        var result = await getJournalSingleService.ExecuteAsync(new GetJournalSingleRequest
        {
            Id = id
        }, cancellationToken);

        return Ok(new JournalResponse
        {
            Id = result!.Id,
            EventId = result.EventId,
            CreatedAt = result.CreatedAt,
            Text = result.Text
        });
    }
}
