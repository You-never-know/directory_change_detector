namespace DirectoryChangeDetector.Models;

/*
 * Class used to return file metadata needed by the service
 */
public class ModifiedFileMetadata
{
    public string RelativePath { get; set; }
    
    public int Version { get; set; }
}