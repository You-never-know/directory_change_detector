using System;
using System.Collections.Generic;
using System.IO;
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
}