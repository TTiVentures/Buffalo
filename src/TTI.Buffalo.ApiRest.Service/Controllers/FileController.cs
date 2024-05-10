using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using TTI.Buffalo;
using TTI.Buffalo.Models;

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
            file.Metadata.TryGetValue(BuffaloMetadata.Filename, out var fileName);

            if (fileName is null)
            {
                try
                {

                    if (file.MimeType.Contains("/"))
                    {
                        var fileExtension = file.MimeType.Split('/')[1];
                        fileName = id + "." + fileExtension;
                    }
                    else
                    {
                        fileName = id.ToString();
                    }
                }
                catch
                {
                    fileName = id.ToString();
                }
            }

            file.Data.Position = 0; // This is needed to reset the stream position to the beginning
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
            var newFileId = await _fileManager.UploadFile(file, User);

            return Ok(new
            {
                FileId = newFileId
            });
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
            var mediaUrl = await _fileManager.UpdateFileMetadata(id, body, User);
            return Ok(new
            {
                publicUrl = mediaUrl
            });
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