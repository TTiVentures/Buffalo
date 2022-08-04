using Buffalo;
using Buffalo.Dto;
using Buffalo.Implementations;
using Buffalo.Models;
using Buffalo.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Buffalo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class FileController : ControllerBase
    {
        private IStorage _cloudStorage;
        private IConfiguration _configuration;
        private FileContext _fileContext;

        public FileController(IStorage cloudStorage, IConfiguration iconfiguration, FileContext fileContext)
		{
			_cloudStorage = cloudStorage;
			_configuration = iconfiguration;
			_fileContext = fileContext;

		}

        // GET api/<FileController>/5
        [HttpGet("{id}")]
		public async Task<IActionResult> GetFile(Guid id)
		{


			var storedFile = _fileContext.Files.Find(id);

			if (storedFile == null)
			{
				return NotFound("File not exists.");
				//throw new HttpResponseException(404, "File not exists.");
			}

			var file = await _cloudStorage.RetrieveFileAsync(id);

			return File(file, storedFile.ContentType ?? "application/octet-stream", storedFile.FileName);


		}

        // POST api/<FileController>
        [HttpPost, DisableRequestSizeLimit]
		public async Task<IActionResult> UploadFile([Required] IFormFile file, [FromForm, Required] AccessModes accessMode = AccessModes.PRIVATE)
		{

			try
			{ 
				Console.WriteLine($"New picture request -> {file.FileName}");

				if (file.Length > 0)
				{
					Guid fileId = Guid.NewGuid();

					string imageUrl = await _cloudStorage.UploadFileAsync(file, fileId.ToString(), accessMode);

					string mime = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName);

					Buffalo.Models.File fileRecord = new()
					{
						CreatedOn = DateTime.UtcNow,
						FileId = fileId,
						FileName = file.FileName,
						ContentType = file.ContentType,
						AccessType = accessMode,
						UploadedBy = User.Identity.Name
					};

					_fileContext.Files.Add(fileRecord);

					await _fileContext.SaveChangesAsync();

					return Ok(new FileDto
                    {
						ContentType = mime,
						AccessType = accessMode,
						CreatedOn = DateTime.UtcNow,
						FileId = fileId,
						FileName = file.FileName,
						UploadedBy = User.Identity.Name

					});
				}
				else
				{
					return BadRequest();
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex}");
			}
			}

        // DELETE api/<FileController>/5
        [HttpDelete("{id}")]
		public async Task<IActionResult> DeleteAsync(Guid id)
		{
			
			var getdocument = _fileContext.Files.Find(id);

			if (getdocument == null)
			{
				return NotFound("File not exists.");
				//throw new HttpResponseException(404, "File not exists.");
			}
			else
			{

				_fileContext.Files.Remove(getdocument);

				try
				{
					await _cloudStorage.DeleteFileAsync(id.ToString());
				}

				catch (Exception e)
				{
					await _fileContext.SaveChangesAsync();
					return BadRequest();
				}

				await _fileContext.SaveChangesAsync();

				return NoContent();

			}
			
		}
    }
}
