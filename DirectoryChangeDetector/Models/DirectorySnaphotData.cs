using System;
using System.Collections.Generic;

namespace DirectoryChangeDetector.Models;

/*
 * Class used to store data about the current state of a directory
 */
public class DirectorySnapshotData
{
    public string DirectoryPath { get; set; }

    public List<FileMetadata> Files { get; set; } = [];

    public List<string> Subdirectories { get; set; } = [];
    
    public DateTime AnalyzedAt { get; set; }
}