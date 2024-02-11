using ImageConverterApi.Models;
using ImageConverterApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Storage;
using System.Text.RegularExpressions;
using System.Security.Claims;


namespace ImageConverterApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly IImageService _imageService;
        private readonly DatabaseContext _dbContext;
        private readonly EncryptionService _encryptionService;

        public ImageController(ILogger<ImageController> logger, IImageService imageService, DatabaseContext dbContext, EncryptionService encryptionService)
        {
            _logger = logger;
            _imageService = imageService;
            _dbContext = dbContext;
            _encryptionService = encryptionService;
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] ImageUploadModel model, IFormFile imageFile)
        {
            // Validate input
            if (model.TargetWidth <= 0 || model.TargetHeight <= 0)
                return BadRequest("Invalid target dimensions");
            
            if (string.IsNullOrEmpty(model.TargetFormat) || !Regex.IsMatch(model.TargetFormat, "png|jpeg", RegexOptions.IgnoreCase))
                return BadRequest("Invalid target format");

            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No image file uploaded");

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Import the image
            using var fileStream = imageFile.OpenReadStream();
            
            var imageResult = await _imageService.ImportImage(model, fileStream, imageFile.FileName, username);
            // Log the upload
            if(!imageResult.AlreadyExists) _logger.LogInformation($"Uploaded image {imageResult.ImageId} with format {model.TargetFormat} and dimensions {model.TargetWidth}x{model.TargetHeight}");
            else _logger.LogInformation($"Duplicate image detected with hash {imageResult.ImageId}");

            // Return the image ID
            return Ok(new { imageId = imageResult.ImageId.ToString(), AlreadyExists = imageResult.AlreadyExists });
        }


        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            // Lookup the image by ID
            var image = await _dbContext.Images.FirstOrDefaultAsync(i => i.ImageId == id);

            // If not found (or invalid) return 404
            if (image == null || image.Data == null)
                return NotFound();

            var decryptedData = _encryptionService.Decrypt(image.Data);

            // Return the image with the correct content type
            return File(decryptedData, $"image/{image.ImageFormat}");
        }

        [HttpGet("info/{id}")]
        public async Task<IActionResult> Info(Guid id)
        {
            // Lookup the image by ID
            var image = await _dbContext.Images.FirstOrDefaultAsync(i => i.ImageId == id);

            // If not found (or invalid ID) return 404
            if (image == null)
            {
                return NotFound();
            }

            // Construct a response object with the image information
            var response = new
            {
                OriginalFilename = image.FileName,
                Format = image.ImageFormat,
                CreatedAt = image.CreatedAt,
                Width = image.Width,
                Height = image.Height,
                StoredSizeInBytes = image.Data?.Length ?? 0,
                User = image.Username
            };

            // Return the image information as JSON
            return Ok(response);
        }


    }
}
