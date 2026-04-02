using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valetax.Application.Trees.GetTree;
using Valetax.Api.Contracts;

namespace Valetax.Api.Controllers;

[ApiController]
[Authorize]
[Tags("user.tree")]
public class TreeController(IGetTreeService getTreeService) : ControllerBase
{
    /// <summary>
    /// Returns the entire tree. If the tree does not exist, it is created automatically.
    /// </summary>
    [HttpPost("/api.user.tree.get")]
    [ProducesResponseType(typeof(NodeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NodeResponse>> Get([FromQuery] string treeName, CancellationToken cancellationToken)
    {
        var result = await getTreeService.ExecuteAsync(new GetTreeRequest
        {
            TreeName = treeName
        }, cancellationToken);

        return Ok(MapNode(result));
    }

    private static NodeResponse MapNode(GetTreeNodeResult node)
    {
        return new NodeResponse
        {
            Id = node.Id,
            Name = node.Name,
            Children = node.Children.Select(MapNode).ToList()
        };
    }
}
