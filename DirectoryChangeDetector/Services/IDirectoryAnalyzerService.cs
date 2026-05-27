using DirectoryChangeDetector.Models;

namespace DirectoryChangeDetector.Services;

public interface IDirectoryAnalyzerService
{
    AnalysisResult AnalyzeDirectory(string directoryPath);
}