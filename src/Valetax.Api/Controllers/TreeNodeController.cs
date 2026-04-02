using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valetax.Application.TreeNodes.CreateNode;
using Valetax.Application.TreeNodes.DeleteNode;
using Valetax.Application.TreeNodes.RenameNode;

namespace Valetax.Api.Controllers;

[ApiController]
[Authorize]
[Tags("user.tree.node")]
public class TreeNodeController(
    ICreateNodeService createNodeService,
    IDeleteNodeService deleteNodeService,
    IRenameNodeService renameNodeService) : ControllerBase
{
    /// <summary>
    /// Creates a new node in the specified tree.
    /// </summary>
    /// <remarks>
    /// The parent node, when provided, must belong to the same tree. Node names must be unique across siblings.
    /// </remarks>
    [HttpPost("/api.user.tree.node.create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromQuery] string treeName,
        [FromQuery] long? parentNodeId,
        [FromQuery] string nodeName,
        CancellationToken cancellationToken)
    {
        await createNodeService.ExecuteAsync(new CreateNodeRequest
        {
            TreeName = treeName,
            ParentNodeId = parentNodeId,
            NodeName = nodeName
        }, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Deletes an existing node.
    /// </summary>
    [HttpPost("/api.user.tree.node.delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromQuery] long nodeId, CancellationToken cancellationToken)
    {
        await deleteNodeService.ExecuteAsync(new DeleteNodeRequest
        {
            NodeId = nodeId
        }, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Renames an existing node.
    /// </summary>
    /// <remarks>
    /// The new name must be unique across sibling nodes.
    /// </remarks>
    [HttpPost("/api.user.tree.node.rename")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Rename(
        [FromQuery] long nodeId,
        [FromQuery] string newNodeName,
        CancellationToken cancellationToken)
    {
        await renameNodeService.ExecuteAsync(new RenameNodeRequest
        {
            NodeId = nodeId,
            NewNodeName = newNodeName
        }, cancellationToken);

        return Ok();
    }
}
