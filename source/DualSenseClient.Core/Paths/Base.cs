namespace DualSenseClient.Core.Paths;

public abstract class Base
{
    protected static string _baseDirectory
    {
        get
        {
            // Most accurate method of returning directory where executable is
            string? exePath = Path.GetDirectoryName(Environment.ProcessPath);
            if (!string.IsNullOrEmpty(exePath))
            {
                return exePath;
            }

            // Can return temp folder if used as single file
            string appDomainDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(appDomainDir) && !IsTempDirectory(appDomainDir))
            {
                return appDomainDir;
            }

            // Can return temp folder if used as single file
            string appContextDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(appContextDir) && !IsTempDirectory(appContextDir))
            {
                return appContextDir;
            }

            // Return current working directory as last resort
            return Directory.GetCurrentDirectory();

            // Helper to detect if a folder is a temp directory
            static bool IsTempDirectory(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }
                string tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
                return normalizedPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    protected static string GetFullPath(string relativePath) => Path.Combine(_baseDirectory, relativePath);
    protected static string GetFullPath(params string[] relativePaths) => Path.Combine(new[] { _baseDirectory }.Concat(relativePaths).ToArray());
}