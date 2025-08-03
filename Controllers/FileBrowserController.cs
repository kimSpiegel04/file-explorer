using Microsoft.AspNetCore.Mvc;
using TestProject.Models;
using TestProject.Helpers;

namespace TestProject.Controllers {
    [ApiController]
    [Route("api/files")]
    public class FileBrowserController : ControllerBase
    {

        private readonly ILogger<FileBrowserController> _logger;

        public FileBrowserController(ILogger<FileBrowserController> logger)
        {
            _logger = logger;
        }

        private static List<FileSystemItem> RecursiveSearch(string dirPath, string searchTerm)
        {
            var res = new List<FileSystemItem>();
            try
            {
                foreach (var dir in Directory.GetDirectories(dirPath))
                {
                    var info = new DirectoryInfo(dir);
                    if (info.Name.ToLower().Contains(searchTerm))
                    {
                        res.Add(new FileSystemItem
                        {
                            Name = info.Name,
                            Type = "folder",
                            Size = null,
                            LastModified = info.LastWriteTime
                        });
                    }
                    res.AddRange(RecursiveSearch(dir, searchTerm));
                }

                foreach (var file in Directory.GetFiles(dirPath))
                {
                    var info = new FileInfo(file);
                    if (info.Name.ToLower().Contains(searchTerm))
                    {
                        res.Add(new FileSystemItem
                        {
                            Name = info.Name,
                            Type = "file",
                            Size = info.Length,
                            LastModified = info.LastWriteTime
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipped: {dirPath} - {ex.Message}");
            }

            return res;
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            var dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            foreach (var subDir in dirs)
            {
                string newDestDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir);
            }
        }

        [HttpGet]
        public IActionResult Get(
            [FromQuery] string path,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string sortBy = "size", // or "date", "name"
            [FromQuery] string sortDirection = "asc" // or "desc"
        )
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest(new { error = "Missing path" });
            }

            var absolutePath = Path.Combine(Config.RootDirectory, path.TrimStart('/'));

            if (!Directory.Exists(absolutePath))
            {
                return BadRequest(new { error = "Directory doesn't exist" });
            }

            if (!Path.GetFullPath(absolutePath).StartsWith(Path.GetFullPath(Config.RootDirectory)))
            {
                return BadRequest("Path access outside of root is not allowed");
            }

            List<FileSystemItem> items;

            if (!string.IsNullOrEmpty(search))
            {
                items = RecursiveSearch(absolutePath, search.ToLower());
            }
            else
            {
                items = new List<FileSystemItem>();
                var folders = Directory.GetDirectories(absolutePath).Select(absolutePath =>
                {
                    var info = new DirectoryInfo(absolutePath);
                    return new FileSystemItem
                    {
                        Name = info.Name + "/",
                        Type = "folder",
                        Size = null,
                        LastModified = info.LastWriteTime
                    };
                });

                var filePaths = Directory.GetFiles(absolutePath);
                var files = filePaths.Select(absolutePath =>
                {
                    var info = new FileInfo(absolutePath);
                    return new FileSystemItem
                    {
                        Name = info.Name,
                        Type = "file",
                        Size = info.Length, // in bytes
                        LastModified = info.LastWriteTime
                    };
                });

                items.AddRange(folders);
                items.AddRange(files);

            }
            // default: sort by size
            IEnumerable<FileSystemItem> sortedItems = sortBy switch
            {
                "name" => sortDirection == "desc" ? items.OrderByDescending(i => i.Name) : items.OrderBy(i => i.Name),
                "date" => sortDirection == "desc" ? items.OrderByDescending(i => i.LastModified) : items.OrderBy(i => i.LastModified),
                "size" => sortDirection == "desc" ? items.OrderByDescending(i => i.Size ?? 0) : items.OrderBy(i => i.Size ?? 0),
                _ => items.OrderBy(i => i.Name)
            };
            var skip = (page - 1) * pageSize;
            var pagedItems = sortedItems.Skip(skip).Take(pageSize).ToList();

            var fileCount = items.Count(i => i.Type == "file");
            var folderCount = items.Count(i => i.Type == "folder");
            var totalSize = items
                .Where(i => i.Type == "file" && i.Size.HasValue)
                .Sum(i => i.Size!.Value);

            return Ok(new
            {
                path,
                items = pagedItems,
                fileCount,
                folderCount,
                totalSize,
                page,
                pageSize,
                totalItems = items.Count,
                currentPage = page,
                totalPages = (int)Math.Ceiling((double)items.Count / pageSize),
                sortBy,
                sortDirection
            });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromQuery] string path, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Invalid Path");
            }
            if (file == null || file.Length == 0)
            {
                return BadRequest("Empty file");
            }

            var absolutePath = Path.Combine(Config.RootDirectory, path.TrimStart('/'));
            if (!Path.GetFullPath(absolutePath).StartsWith(Path.GetFullPath(Config.RootDirectory)))
            {
                return BadRequest("Path access outside of root is not allowed");
            }
            var fullPath = Path.Combine(absolutePath, file.FileName);

            try
            {
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return Ok(new { fileName = file.FileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("download")]
        public IActionResult Download([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Invalid file path");
            }

            var absolutePath = Path.Combine(Config.RootDirectory, path.TrimStart('/'));
            if (!System.IO.File.Exists(absolutePath))
            {
                return BadRequest(new { error = "File doesn't exist" });
            }

            var fileName = Path.GetFileName(absolutePath);
            var mime = "application/octet-stream";
            var bytes = System.IO.File.ReadAllBytes(absolutePath);
            return File(bytes, mime, fileName);
        }

        [HttpDelete("delete")]
        public IActionResult Delete([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Missing path");
            }

            var absolutePath = Path.Combine(Config.RootDirectory, path.TrimStart('/'));

            try
            {
                if (Directory.Exists(absolutePath))
                {
                    Directory.Delete(absolutePath, recursive: true);
                }
                else if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }
                else
                {
                    return NotFound("Folder or file does not exist");
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("action")]
        public IActionResult MoveOrCopy([FromQuery] string sourcePath, [FromQuery] string destinationPath, [FromQuery] string action)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
            {
                return BadRequest("Missing source or destination path");
            }

            var absoluteSourcePath = Path.Combine(Config.RootDirectory, sourcePath.TrimStart('/'));
            var absoluteDestinationPath = Path.Combine(Config.RootDirectory, destinationPath.TrimStart('/'));

            try
            {
                if (action == "move")
                {
                    if (Directory.Exists(absoluteSourcePath))
                    {
                        Directory.Move(absoluteSourcePath, absoluteDestinationPath);
                    }
                    else if (System.IO.File.Exists(absoluteSourcePath))
                    {
                        System.IO.File.Move(absoluteSourcePath, absoluteDestinationPath);
                    }
                    else
                    {
                        return NotFound("Source path not found");
                    }
                }
                else if (action == "copy")
                {
                    if (Directory.Exists(absoluteSourcePath))
                    {
                        CopyDirectory(absoluteSourcePath, absoluteDestinationPath);

                    }
                    else if (System.IO.File.Exists(absoluteSourcePath))
                    {
                        System.IO.File.Copy(absoluteSourcePath, absoluteDestinationPath);
                    }
                    else
                    {
                        return NotFound("Source path not found");
                    }
                }
                else
                {
                    return BadRequest("how did you get here");
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("directories")]
        public IActionResult GetDirectories([FromQuery] string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return BadRequest("Invalid or missing root path");
            }

            // Normalize the path
            var candidatePath = Path.GetFullPath(
                root.StartsWith(Config.RootDirectory)
                    ? root
                    : Path.Combine(Config.RootDirectory, root.TrimStart('/'))
            );

            // Validate path stays within root
            var normalizedRoot = Path.GetFullPath(Config.RootDirectory);
            if (!candidatePath.StartsWith(normalizedRoot))
            {
                return BadRequest("Access outside of root is not allowed");
            }

            if (!Directory.Exists(candidatePath))
            {
                return BadRequest("Directory doesn't exist");
            }

            try
            {
                var dirs = Directory.GetDirectories(candidatePath, "*", SearchOption.TopDirectoryOnly).Select(path => new
                {
                    FullPath = path.Substring(normalizedRoot.Length).Replace('\\', '/'),
                    Name = Path.GetFileName(path)
                });
                return Ok(dirs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}