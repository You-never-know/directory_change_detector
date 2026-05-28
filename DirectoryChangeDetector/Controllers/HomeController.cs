using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using DirectoryChangeDetector.Services;

namespace DirectoryChangeDetector.Controllers;

public class HomeController : Controller
{
    private readonly IDirectoryAnalyzerService _directoryAnalyzerService;

    public HomeController(IDirectoryAnalyzerService directoryAnalyzerService)
    {
        _directoryAnalyzerService = directoryAnalyzerService;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Analyze(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            ViewBag.Error = "Please enter a directory path.";
            return View("Index");
        }

        try
        {
            var result = _directoryAnalyzerService.AnalyzeDirectory(directoryPath.Trim());
            ViewBag.DirectoryPath = directoryPath.Trim();
            return View("Index", result);
        }
        catch (DirectoryNotFoundException ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.DirectoryPath = directoryPath.Trim();
            return View("Index");
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"An error occured: {ex.Message}";
            ViewBag.DirectoryPath = directoryPath.Trim();
            return View("Index");
        }
    }

    [HttpGet]
    public IActionResult BrowseDirectories(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                // Return available drives as root entries
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new { name = d.Name.TrimEnd('\\'), path = d.Name })
                    .ToList();
                return Json(new { parent = (string?)null, items = drives });
            }

            var fullPath = Path.GetFullPath(path);

            if (!Directory.Exists(fullPath))
                return Json(new { error = "Directory not found." });

            var directories = Directory.GetDirectories(fullPath)
                .Select(d => new { name = Path.GetFileName(d), path = d })
                .OrderBy(d => d.name)
                .ToList();

            var parent = Directory.GetParent(fullPath)?.FullName;

            return Json(new { parent, items = directories });
        }
        catch (UnauthorizedAccessException)
        {
            return Json(new { error = "Access denied." });
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }
}