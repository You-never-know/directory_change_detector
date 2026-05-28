namespace DirectoryChangeDetector.Models;

/*
 * Class used to store important metadata about a file
 */
public class FileMetadata
{
    public string RelativePath { get; set; } = string.Empty;
    
    public string FileHash  { get; set; } = string.Empty;

    public int Version { get; set; } = 1;
}