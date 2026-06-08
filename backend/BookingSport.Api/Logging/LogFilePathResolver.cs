namespace BookingSport.Api.Logging;

public static class LogFilePathResolver
{
    public static string ResolveFilePath(string contentRootPath, LogFileOptions options)
    {
        var folderPath = string.IsNullOrWhiteSpace(options.FolderPath)
            ? "logs"
            : options.FolderPath;
        var fileNamePattern = string.IsNullOrWhiteSpace(options.FileNamePattern)
            ? "booking-sport-api-.log"
            : options.FileNamePattern;
        var resolvedFolderPath = Path.IsPathRooted(folderPath)
            ? folderPath
            : Path.Combine(contentRootPath, folderPath);

        Directory.CreateDirectory(resolvedFolderPath);

        return Path.Combine(resolvedFolderPath, fileNamePattern);
    }
}
