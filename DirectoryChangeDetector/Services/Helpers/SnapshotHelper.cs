using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DirectoryChangeDetector.Models;

namespace DirectoryChangeDetector.Services.Helpers;

public class SnapshotHelper
{
    private readonly string _dataFolder;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public SnapshotHelper(string dataFolder, JsonSerializerOptions jsonSerializerOptions)
    {
        _dataFolder = dataFolder;
        _jsonSerializerOptions = jsonSerializerOptions;
    }
    
    private string GetSnapshotPath(string directoryPath) 
        => Path.Combine(_dataFolder, FileSystemHelper.GetSnapshotFileName(directoryPath));
    
    public DirectorySnapshotData? LoadSnapshot(string directoryPath)
    {
        var snapshotPath = GetSnapshotPath(directoryPath);

        if (!File.Exists(snapshotPath))
        {
            return null;
        }

        var json = File.ReadAllText(snapshotPath);
        return JsonSerializer.Deserialize<DirectorySnapshotData>(json, _jsonSerializerOptions);
    }
    
    public void SaveSnapshot(string fullPath, List<FileMetadata> versionedFiles, List<string> subdirectories)
    {
        var directorySnapshotData = new DirectorySnapshotData
        {
            DirectoryPath = fullPath,
            Files = versionedFiles,
            Subdirectories = subdirectories,
            AnalyzedAt = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(directorySnapshotData, _jsonSerializerOptions);
        File.WriteAllText(GetSnapshotPath(fullPath), json);
    }
    
    public (AnalysisResult Result, List<FileMetadata> VersionedFiles) CompareWithPrevious(
        string fullPath, DirectoryAnalyzerService.ScanResult scanResult, DirectorySnapshotData? previousSnapshot)
    {
        if (previousSnapshot == null)
        {
            // First run - all files are new at version 1
            var result = new AnalysisResult
            {
                DirectoryPath = fullPath,
                IsFirstRun = true,
                NewFiles = scanResult.Files.Select(f => f.RelativePath).ToList(),
                Warnings = scanResult.Warnings
            };
            return (result, scanResult.Files);
        }
        
        var previousFileMap = previousSnapshot.Files.ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase);

        var newFiles = new List<string>();
        var modifiedFiles = new List<ModifiedFileMetadata>();
        var stillExistingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var versionedFiles = new List<FileMetadata>(scanResult.Files.Count);
        
        // Single pass over current files: classify and assign correct versions
        foreach (var currentFile in scanResult.Files)
        {
            if (!previousFileMap.TryGetValue(currentFile.RelativePath, out var alreadyExistingFile))
            {
                // New file - version stays at 1
                newFiles.Add(currentFile.RelativePath);
                versionedFiles.Add(currentFile);
                continue;
            }
            
            stillExistingPaths.Add(alreadyExistingFile.RelativePath);

            // Check if the file was modified
            if (currentFile.FileHash != alreadyExistingFile.FileHash)
            {
                // Modified - increment version
                currentFile.Version = alreadyExistingFile.Version + 1;
                modifiedFiles.Add(new ModifiedFileMetadata
                {
                    RelativePath = currentFile.RelativePath,
                    Version = currentFile.Version
                });
            }
            else
            {
                // Unchanged - carry previous version
                currentFile.Version = alreadyExistingFile.Version;
            }  
            
            versionedFiles.Add(currentFile);
        }

        // Single pass over previous files: anything not seen is deleted
        var deletedFiles = previousSnapshot.Files
            .Where(f => !stillExistingPaths.Contains(f.RelativePath))
            .Select(f => f.RelativePath)
            .ToList();
        
        // Single pass over previous subdirectories
        var currentSubdirectoriesSet =  new HashSet<string>(scanResult.Subdirectories, StringComparer.OrdinalIgnoreCase);
        var deletedSubdirectories = previousSnapshot.Subdirectories
            .Where(s => !currentSubdirectoriesSet.Contains(s))
            .ToList();

        var analysisResult = new AnalysisResult
        {
            DirectoryPath = fullPath,
            IsFirstRun = false,
            NewFiles = newFiles,
            ModifiedFiles = modifiedFiles,
            DeletedFiles = deletedFiles,
            DeletedSubdirectories = deletedSubdirectories,
            Warnings = scanResult.Warnings
        };
        
        return (analysisResult, versionedFiles);
    }
}