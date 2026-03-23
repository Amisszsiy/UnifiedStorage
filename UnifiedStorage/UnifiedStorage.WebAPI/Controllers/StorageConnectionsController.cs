using MediatR;
using Microsoft.AspNetCore.Mvc;
using UnifiedStorage.Application.StorageConnections.Commands.DisconnectStorage;
using UnifiedStorage.Application.StorageConnections.Queries.GetConnections;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Exceptions;

namespace UnifiedStorage.WebAPI.Controllers;

[ApiController]
[Route("api/connections")]
public class StorageConnectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StorageConnectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns all storage connections for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StorageConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetConnectionsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Disconnects and removes a storage provider connection.
    /// </summary>
    [HttpDelete("{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disconnect(
        [FromRoute] StorageProvider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DisconnectStorageCommand(provider), cancellationToken);
            return NoContent();
        }
        catch (StorageConnectionNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
