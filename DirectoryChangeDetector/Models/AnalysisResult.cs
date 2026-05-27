using System.Collections.Generic;

namespace DirectoryChangeDetector.Models;

/*
 * Class used to return data for the service
 */
public class AnalysisResult
{
    public string DirectoryPath { get; set; } = string.Empty;
    
    public bool IsFirstRun { get; set; }

    public List<string> NewFiles { get; set; } = [];
    
    public List<ModifiedFileMetadata> ModifiedFiles { get; set; } = [];
    
    public List<string> DeletedFiles { get; set; } = [];
    
    public List<string> DeletedSubdirectories { get; set; } = [];
    
    public List<string> Warnings { get; set; } = [];
}