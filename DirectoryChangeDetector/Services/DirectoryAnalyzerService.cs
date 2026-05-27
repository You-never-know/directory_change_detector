using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DirectoryChangeDetector.Models;
using DirectoryChangeDetector.Services.Helpers;
using Microsoft.AspNetCore.Hosting;

namespace DirectoryChangeDetector.Services;

public class DirectoryAnalyzerService : IDirectoryAnalyzerService
{
    private const long MaxFileSizeBytes = 50L * 1024 * 1024; // 50MB
    private readonly string _dataFolder;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public DirectoryAnalyzerService(IWebHostEnvironment webHostEnvironment)
    : this(webHostEnvironment, new JsonSerializerOptions() { WriteIndented = true })
    {
    }
    
    public DirectoryAnalyzerService(IWebHostEnvironment webHostEnvironment, JsonSerializerOptions jsonSerializerOptions)
    {
        _dataFolder = Path.Combine(webHostEnvironment.ContentRootPath, "Data");
        _jsonSerializerOptions = jsonSerializerOptions;
    }
    
    public AnalysisResult AnalyzeDirectory(string directoryPath)
    {
        EnsureDataFolderExists();
        
        var fullPath = Path.GetFullPath(directoryPath);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory {fullPath} not found.");
        }
        
        var scanResult = ScanDirectory(fullPath);
        var previousSnapshot = LoadSnapshot(fullPath);
        var result = CompareWithPrevious(fullPath, scanResult, previousSnapshot);

        // TODO
        //SaveSnapshot(fullPath, scanResult);

        return result;
    }

    private ScanResult ScanDirectory(string rootPath)
    {
        var context = new ScanContext(rootPath);
        ScanDirectoryRecursive(context, rootPath);
        return new ScanResult(context.Files, context.Subdirectories, context.Warnings);
    }

    private void ScanDirectoryRecursive(ScanContext context, string currentPath)
    {
        var resolvedPath = FileSystemHelper.ResolveRealPath(currentPath);

        if (!context.VisitedPaths.Add(resolvedPath))
        {
            context.Warnings.Add(
                $"Skipped circular symlink: {FileSystemHelper.GetRelativePath(context.RootPath, currentPath)}");
            return;
        }

        try
        {
            foreach (var filePath in Directory.GetFiles(currentPath))
            {
                ProcessFile(context, filePath);
            }

            foreach (var dirPath in Directory.GetDirectories(currentPath))
            {
                context.Subdirectories.Add(FileSystemHelper.GetRelativePath(context.RootPath, dirPath));
                ScanDirectoryRecursive(context, dirPath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Warnings.Add(
                $"Access denied: {FileSystemHelper.GetRelativePath(context.RootPath, currentPath)} - {ex.Message}");
        }
        catch (Exception ex)
        {
            context.Warnings.Add(
                $"Error scanning directory: {FileSystemHelper.GetRelativePath(context.RootPath, currentPath)}: {ex.Message}");
        }
    }

    private void ProcessFile(ScanContext context, string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Length > MaxFileSizeBytes)
            {
                context.Warnings.Add(
                    $"Skipped file {FileSystemHelper.GetRelativePath(context.RootPath, filePath)} because it exceeds 50MB.");
                return;
            }

            context.Files.Add(new FileMetadata
                {
                    RelativePath = FileSystemHelper.GetRelativePath(context.RootPath, filePath),
                    FileHash = FileSystemHelper.ComputeFileHash(filePath),
                    Version = 1
                }

            );
        }
        catch (Exception ex)
        {
            context.Warnings.Add($"Error reading file: {FileSystemHelper.GetRelativePath(context.RootPath, filePath)}: {ex.Message}");
        }
    }

    private DirectorySnapshotData? LoadSnapshot(string directoryPath)
    {
        var snapshotPath = GetSnapshotPath(directoryPath);

        if (!File.Exists(snapshotPath))
        {
            return null;
        }

        var json = File.ReadAllText(snapshotPath);
        return JsonSerializer.Deserialize<DirectorySnapshotData>(json, _jsonSerializerOptions);
    }

    private AnalysisResult CompareWithPrevious(string fullPath, ScanResult scanResult,
        DirectorySnapshotData? previousSnapshot)
    {
        if (previousSnapshot == null)
        {
            return new AnalysisResult
            {
                DirectoryPath = fullPath,
                IsFirstRun = true,
                NewFiles = scanResult.Files.Select(f => f.RelativePath).ToList(),
                Warnings = scanResult.Warnings
            };
        }
        
        var previousFileMap = previousSnapshot.Files.ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase);
        // TODO

        return null;
    }
    
    private void EnsureDataFolderExists() => Directory.CreateDirectory(_dataFolder);
    
    private string GetSnapshotPath(string directoryPath) 
        => Path.Combine(_dataFolder, FileSystemHelper.GetSnapshotFileName(directoryPath));

    /// <summary>
    /// Holds mutable state during recursive directory scanning.
    /// </summary>
    private sealed class ScanContext
    {
        public string RootPath { get; }

        public HashSet<string> VisitedPaths { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<FileMetadata> Files { get; } = [];

        public List<string> Subdirectories { get; } = [];
        
        public List<string> Warnings { get; } = [];
        
        
        public ScanContext(string rootPath) => RootPath = rootPath;
    }
    
    /// <summary>
    /// Immutable result of a directory scan
    /// </summary>
    private sealed record ScanResult(List<FileMetadata> Files, List<string> Subdirectories, List<string> Warnings);

}