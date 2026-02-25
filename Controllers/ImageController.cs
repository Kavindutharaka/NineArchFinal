using Microsoft.AspNetCore.Mvc;

namespace NineArchTours.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ImageController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Upload images for a given category (locations, hotels, vehicles)
        [HttpPost("upload/{category}/{itemName}")]
        public async Task<ActionResult> Upload(string category, string itemName, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var allowedCategories = new[] { "locations", "hotels", "vehicles" };
            if (!allowedCategories.Contains(category.ToLower()))
                return BadRequest("Invalid category.");

            // Sanitize itemName for folder use
            var safeName = string.Join("_", itemName.Split(Path.GetInvalidFileNameChars()));
            var uploadDir = Path.Combine(_env.WebRootPath, "images", category.ToLower(), safeName);
            Directory.CreateDirectory(uploadDir);

            var uploadedPaths = new List<string>();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    uploadedPaths.Add($"/images/{category.ToLower()}/{safeName}/{fileName}");
                }
            }

            return Ok(new { paths = uploadedPaths });
        }

        // Get all images for a given category/item
        [HttpGet("list/{category}/{itemName}")]
        public ActionResult ListImages(string category, string itemName)
        {
            var safeName = string.Join("_", itemName.Split(Path.GetInvalidFileNameChars()));
            var dir = Path.Combine(_env.WebRootPath, "images", category.ToLower(), safeName);

            if (!Directory.Exists(dir))
                return Ok(new { paths = new List<string>() });

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var paths = Directory.GetFiles(dir)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                .Select(f => $"/images/{category.ToLower()}/{safeName}/{Path.GetFileName(f)}")
                .ToList();

            return Ok(new { paths });
        }

        // Delete a specific image
        [HttpDelete("delete")]
        public ActionResult DeleteImage([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path))
                return BadRequest("Path required.");

            var fullPath = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                return Ok(new { deleted = true });
            }
            return NotFound();
        }
    }
}
