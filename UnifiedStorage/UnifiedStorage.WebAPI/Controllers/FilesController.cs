using MediatR;
using Microsoft.AspNetCore.Mvc;
using UnifiedStorage.Application.Files.Commands.DeleteFile;
using UnifiedStorage.Application.Files.Commands.UploadFile;
using UnifiedStorage.Application.Files.Queries.DownloadFile;
using UnifiedStorage.Application.Files.Queries.GetFiles;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Exceptions;

namespace UnifiedStorage.WebAPI.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lists files from all connected providers, or a specific provider if specified.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CloudFileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiles(
        [FromQuery] StorageProvider? provider,
        [FromQuery] string? folderId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFilesQuery(provider, folderId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Downloads a file from the specified provider.
    /// </summary>
    [HttpGet("{provider}/{fileId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(
        [FromRoute] StorageProvider provider,
        [FromRoute] string fileId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new DownloadFileQuery(provider, fileId), cancellationToken);
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (StorageConnectionNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReAuthRequiredException ex)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new
            {
                error = ex.Message,
                provider = ex.Provider.ToString()
            });
        }
    }

    /// <summary>
    /// Uploads a file to the specified provider.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(CloudFileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(524_288_000)] // 500 MB
    public async Task<IActionResult> UploadFile(
        [FromQuery] StorageProvider provider,
        [FromQuery] string? folderId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _mediator.Send(
                new UploadFileCommand(provider, file.FileName, stream, folderId),
                cancellationToken);

            return CreatedAtAction(nameof(GetFiles), new { provider }, result);
        }
        catch (StorageConnectionNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReAuthRequiredException ex)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new
            {
                error = ex.Message,
                provider = ex.Provider.ToString()
            });
        }
    }

    /// <summary>
    /// Deletes a file from the specified provider.
    /// </summary>
    [HttpDelete("{provider}/{fileId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(
        [FromRoute] StorageProvider provider,
        [FromRoute] string fileId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteFileCommand(provider, fileId), cancellationToken);
            return NoContent();
        }
        catch (StorageConnectionNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReAuthRequiredException ex)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new
            {
                error = ex.Message,
                provider = ex.Provider.ToString()
            });
        }
    }
}
