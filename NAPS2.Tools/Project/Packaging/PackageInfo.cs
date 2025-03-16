using NAPS2.Tools.Project.Targets;

namespace NAPS2.Tools.Project.Packaging;

public class PackageInfo
{
    private readonly List<PackageFile> _files = [];
    private readonly HashSet<string> _destPaths = [];

    public PackageInfo(Platform platform, string versionName, string versionNumber, string? packageName)
    {
        Platform = platform;
        VersionName = versionName;
        VersionNumber = versionNumber;
        PackageName = packageName == null ? platform.PackageName() : $"{platform.PackageName()}-{packageName}";;
    }

    public Platform Platform { get; }
    
    public string VersionName { get; }

    public string VersionNumber { get; }

    public string PackageName { get; }

    public string GetPath(string ext)
    {
        return ProjectHelper.GetPackagePath(ext, Platform, VersionName, PackageName);
    }

    public IEnumerable<PackageFile> Files => _files;

    /// <summary>
    /// The set of languages that have resource files. This may be different from the set of NAPS2-supported languages
    /// as WinForms etc. provide resources for a disjoint set of languages.
    /// </summary>
    public HashSet<string> Languages { get; } = [];

    public void AddFile(FileInfo file, string destFolder, string? destFileName = null)
    {
        if (file.DirectoryName == null)
        {
            throw new ArgumentException();
        }
        AddFile(new PackageFile(file.DirectoryName, destFolder, file.Name, destFileName));
    }

    public void AddFile(PackageFile file)
    {
        if (_destPaths.Add(file.DestPath))
        {
            _files.Add(file);
        }
    }
}