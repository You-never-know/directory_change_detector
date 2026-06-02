# Directory Change Detector

An ASP.NET Core MVC application that detects changes in a local directory between manual analysis runs.

## How It Works

The application monitors a user-specified directory for file changes using a snapshot-based approach:

1. **First analysis:** The app recursively scans the directory, computes a SHA-256 hash of each file's content, and persists the result as a JSON snapshot file. All files start at version 1.
2. **Subsequent analyses:** The app scans again, loads the previous snapshot, and performs a single-pass comparison to classify every file:
    - **New files** — present now but absent from the previous snapshot (version = 1).
    - **Modified files** — content hash differs from the snapshot → version is incremented.
    - **Deleted files** — in the previous snapshot but no longer on disk.
    - **Deleted subdirectories** — previously tracked directories that no longer exist.
3. **Snapshot update:** After comparison, the new state (with correct version numbers) is saved, becoming the baseline for the next run.

### Key Design Decisions

| Concern | Approach |
|---------|----------|
| Change detection | SHA-256 hash of file content (reliable, handles binary files) |
| Version tracking | Integer counter per file, incremented on each detected change |
| Persistence | JSON files in `Data/` folder — no database required |
| Symlink safety | Track resolved real paths in a `HashSet` to detect and skip circular symlinks |
| Snapshot naming | MD5 hash of the directory path → supports analyzing multiple directories independently |
| Architecture | Interface-based DI (`IDirectoryAnalyzerService`), separated concerns (service, helper, models) |

## Running the Application

```bash
cd .\DirectoryChangeDetector
dotnet run
```

Open the URL shown in the terminal (typically `http://localhost:5000`) in your browser.

### Usage

1. Enter a directory path manually **or** click **Browse** to navigate your filesystem visually.
2. Click **Analyze** to trigger the scan.
3. Review the results: new files, modified files (with version transition), deleted files/subdirectories, and any warnings.

## Architecture

```
DirectoryChangeDetector/
├── Controllers/
│   └── HomeController.cs              – Index, Analyze, and BrowseDirectories actions
├── Models/
│   ├── FileMetadata.cs                – File metadata for persistence (path, hash, version)
│   │── ModifiedFileMetadata           - File metadata returned to the view
│   ├── DirectorySnapshotData.cs       – Full directory state saved as JSON
│   └── AnalysisResult.cs              – Comparison result returned to the view
├── Services/
│   ├── IDirectoryAnalyzerService.cs   – Service interface (DI contract)
│   ├── DirectoryAnalyzerService.cs    – Core logic: scan, compare, persist
│   ├── Helpers/FileSystemHelper.cs    – Static utilities: hashing, path resolution
│   └── Helpers/SnapshotHelper         - Helper for loading and saving the snapshot into the file system      
├── Views/
│   ├── Home/Index.cshtml              – Main UI with directory browser
│   └── Shared/_Layout.cshtml          – Bootstrap layout
├── Data/                              – Snapshot JSON files (gitignored)
└── Program.cs                         – App entry point & DI registration
```

## Limitations & Future Improvements

### Current Limitations

- **File size cap:** Files larger than 50MB are skipped (reported as a warning). This is per the spec assumption and avoids excessive memory/time usage during hashing.
- **No real-time watching:** Changes are only detected when the user clicks "Analyze". There is no automatic filesystem monitoring.
- **No concurrency control:** If two browser tabs analyze the same directory simultaneously, the snapshot file could be corrupted. A file lock or mutex would solve this.
- **Rename detection:** A renamed file appears as one deletion + one new file. There is no heuristic to correlate them (e.g., matching hashes of deleted and new files).
- **No pagination:** The UI displays all results in one page. For directories with thousands of files, this could be slow to render.
- **Single-machine use:** The directory browser exposes the local filesystem. This is appropriate for local use but would be a security concern if deployed publicly.

### Potential Future Improvements

- **Rename detection** — match deleted file hashes against new file hashes to identify renames.
- **File lock / mutex** — prevent concurrent snapshot writes to the same file.
- **Async I/O** — for deployment scenarios with multiple concurrent users or network storage.
- **Pagination / virtual scrolling** — for large directories with many changes.
- **Configurable file size limit** — allow the user to adjust the 50MB threshold.
- **Diff preview** — show a content diff for modified text files.
- **Scheduled automatic analysis** — optional background task instead of manual trigger.
- **Export results** — CSV or JSON export of the change report.