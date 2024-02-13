using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TTI.Buffalo;
using TTI.Buffalo.Models;
using FileInfo = TTI.Buffalo.Models.FileInfo;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Buffalo.Sample.Controllers;

[Route("api/[controller]")]
[ApiController]
//[AllowAnonymous]
public class FileController : ControllerBase
{
    private readonly FileManager _fileManager;

    public FileController(FileManager fileManager)
    {
        _fileManager = fileManager;
    }

    // GET api/<FileController>/5
    /// <summary>
    ///     Retrieve a file
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        try
        {
            var file = await _fileManager.GetFile(id, User);
            var fileName = file.Metadata[BuffaloMetadata.Filename];
            return File(file.Data, file.MimeType, fileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ApplicationException)
        {
            return Problem("Application Error");
        }
    }

    // POST api/<FileController>
    /// <summary>
    ///     Upload a new file
    /// </summary>
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadFile([Required] IFormFile file)
    {
        try
        {
            var newFile = await _fileManager.UploadFile(file, User);
            return Created(newFile.ResourceUri ?? "/", newFile);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ApplicationException)
        {
            return Problem("Application Error");
        }
    }


    // DELETE api/<FileController>/5
    /// <summary>
    ///     Deletes a specific File.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        try
        {
            var deleted = await _fileManager.DeleteFile(id, User);
            return deleted ? NoContent() : BadRequest();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ApplicationException)
        {
            return Problem("Application Error");
        }
    }

    // DELETE api/<FileController>/5
    /// <summary>
    ///     Returns a list of saved file IDs
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RetrieveFileListAsync([FromQuery] bool? IncludeMetadata = false)
    {
        try
        {
            return Ok(await _fileManager.RetrieveFileListAsync(IncludeMetadata, User));
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ApplicationException)
        {
            return Problem("Application Error");
        }
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFileMetadataAsync(Guid id, [FromBody] UpdateFileMetadataBody body)
    {
        try
        {
            await _fileManager.UpdateFileMetadata(id, body, User);
            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ApplicationException)
        {
            return Problem("Application Error");
        }
    }
    
}