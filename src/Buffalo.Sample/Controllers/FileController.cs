﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TTI.Buffalo;
using TTI.Buffalo.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Buffalo.Sample.Controllers
{
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
        /// Retrieve a file
        /// </summary>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetFile(Guid id)
		{

			try
			{
				var file = await _fileManager.GetFile(id, User.Identity?.Name);

				return File(file.Data, file.MimeType, file.FileName);
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
        /// Upload a new file
        /// </summary>
        [HttpPost, DisableRequestSizeLimit]
		public async Task<IActionResult> UploadFile([Required] IFormFile file, [FromForm, Required] AccessLevels accessLevel = AccessLevels.PRIVATE)
		{
			try
			{
				FileDto? newFile = await _fileManager.UploadFile(file, User.Identity?.Name, accessLevel);
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
        /// Deletes a specific File.
        /// </summary>
        [HttpDelete("{id}")]
		public async Task<IActionResult> DeleteAsync(Guid id)
		{

			try
			{
				bool deleted = await _fileManager.DeleteFile(id, User.Identity?.Name);
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
        /// Returns a list of saved file IDs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ObjectList>> RetrieveFileListAsync()
        {

            try
            {
                return await _fileManager.RetrieveFileListAsync();

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
}
