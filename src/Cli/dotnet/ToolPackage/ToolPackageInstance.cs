// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace Microsoft.DotNet.Cli.ToolPackage;

// This is named "ToolPackageInstance" because "ToolPackage" would conflict with the namespace
internal class ToolPackageInstance : IToolPackage
{
    public static ToolPackageInstance CreateFromAssetFile(PackageId id, DirectoryPath assetsJsonParentDirectory)
    {
        var lockFile = new LockFileFormat().Read(assetsJsonParentDirectory.WithFile(AssetsFileName).Value);
        var packageDirectory = new DirectoryPath(lockFile.PackageFolders[0].Path);
        var library = FindLibraryInLockFile(lockFile, id);
        var version = library.Version;

        return new ToolPackageInstance(id, version, packageDirectory, assetsJsonParentDirectory);
    }
    private const string PackagedShimsDirectoryConvention = "shims";

    public IEnumerable<string> Warnings => _toolConfiguration.Value.Warnings;

    public PackageId Id { get; private set; }

    public NuGetVersion Version { get; private set; }

    public DirectoryPath PackageDirectory { get; private set; }

    public RestoredCommand Command
    {
        get
        {
            return _command.Value;
        }
    }

    public IReadOnlyList<FilePath> PackagedShims
    {
        get
        {
            return _packagedShims.Value;
        }
    }

    public IEnumerable<NuGetFramework> Frameworks { get; private set; }

    private const string AssetsFileName = "project.assets.json";
    private const string ToolSettingsFileName = "DotnetToolSettings.xml";

    private readonly Lazy<RestoredCommand> _command;
    private readonly Lazy<ToolConfiguration> _toolConfiguration;
    private readonly Lazy<LockFile> _lockFile;
    private readonly Lazy<IReadOnlyList<FilePath>> _packagedShims;

    public ToolPackageInstance(PackageId id,
        NuGetVersion version,
        DirectoryPath packageDirectory,
        DirectoryPath assetsJsonParentDirectory)
    {
        _command = new Lazy<RestoredCommand>(GetCommand);
        _packagedShims = new Lazy<IReadOnlyList<FilePath>>(GetPackagedShims);

        Id = id;
        Version = version ?? throw new ArgumentNullException(nameof(version));
        PackageDirectory = packageDirectory;
        _toolConfiguration = new Lazy<ToolConfiguration>(GetToolConfiguration);
        _lockFile =
            new Lazy<LockFile>(
                () => new LockFileFormat().Read(assetsJsonParentDirectory.WithFile(AssetsFileName).Value));
        var installPath = new VersionFolderPathResolver(PackageDirectory.Value).GetInstallPath(Id.ToString(), Version);
        var toolsPackagePath = Path.Combine(installPath, "tools");
        Frameworks = Directory.GetDirectories(toolsPackagePath)
            .Select(path => NuGetFramework.ParseFolder(Path.GetFileName(path)));
    }

    private RestoredCommand GetCommand()
    {
        try
        {
            LockFileTargetLibrary library = FindLibraryInLockFile(_lockFile.Value);
            ToolConfiguration configuration = _toolConfiguration.Value;
            LockFileItem entryPointFromLockFile = FindItemInTargetLibrary(library, configuration.ToolAssemblyEntryPoint);
            if (entryPointFromLockFile == null)
            {
                throw new ToolConfigurationException(
                    string.Format(
                        CliStrings.MissingToolEntryPointFile,
                        configuration.ToolAssemblyEntryPoint,
                        configuration.CommandName));
            }

            // Currently only "dotnet" commands are supported
            return new RestoredCommand(
                new ToolCommandName(configuration.CommandName),
                "dotnet",
                LockFileRelativePathToFullFilePath(entryPointFromLockFile.Path, library));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
        {
            throw new ToolConfigurationException(
                string.Format(
                    CliStrings.FailedToRetrieveToolConfiguration,
                    ex.Message),
                ex);
        }
    }

    private FilePath LockFileRelativePathToFullFilePath(string lockFileRelativePath, LockFileTargetLibrary library)
    {
        return PackageDirectory
                    .WithSubDirectories(
                        Id.ToString(),
                        library.Version.ToNormalizedString().ToLowerInvariant())
                    .WithFile(lockFileRelativePath);
    }

    private ToolConfiguration GetToolConfiguration()
    {
        try
        {
            var library = FindLibraryInLockFile(_lockFile.Value);
            return DeserializeToolConfiguration(library);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
        {
            throw new ToolConfigurationException(
                string.Format(
                    CliStrings.FailedToRetrieveToolConfiguration,
                    ex.Message),
                ex);
        }
    }

    private IReadOnlyList<FilePath> GetPackagedShims()
    {
        LockFileTargetLibrary library;
        try
        {
            library = FindLibraryInLockFile(_lockFile.Value);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
        {
            throw new ToolPackageException(
                string.Format(
                    CliStrings.FailedToReadNuGetLockFile,
                    Id,
                    ex.Message),
                ex);
        }

        IEnumerable<LockFileItem> filesUnderShimsDirectory = library
            ?.ToolsAssemblies
            ?.Where(t => LockFileMatcher.MatchesDirectoryPath(t, PackagedShimsDirectoryConvention));

        if (filesUnderShimsDirectory == null)
        {
            return [];
        }

        IEnumerable<string> allAvailableShimRuntimeIdentifiers = filesUnderShimsDirectory
            .Select(f => f.Path.Split('\\', '/')?[4]) // ex: "tools/netcoreapp2.1/any/shims/osx-x64/demo" osx-x64 is at [4]
            .Where(f => !string.IsNullOrEmpty(f));

        if (new FrameworkDependencyFile().TryGetMostFitRuntimeIdentifier(
            DotnetFiles.VersionFileObject.BuildRid,
            [.. allAvailableShimRuntimeIdentifiers],
            out var mostFitRuntimeIdentifier))
        {
            return library?.ToolsAssemblies?.Where(l => LockFileMatcher.MatchesDirectoryPath(l, $"{PackagedShimsDirectoryConvention}/{mostFitRuntimeIdentifier}"))
                .Select(l => LockFileRelativePathToFullFilePath(l.Path, library)).ToArray() ?? [];
        }
        else
        {
            return [];
        }
    }

    private ToolConfiguration DeserializeToolConfiguration(LockFileTargetLibrary library)
    {
        var dotnetToolSettings = FindItemInTargetLibrary(library, ToolSettingsFileName);
        if (dotnetToolSettings == null)
        {
            throw new ToolConfigurationException(
                CliStrings.MissingToolSettingsFile);
        }

        var toolConfigurationPath =
            PackageDirectory
                .WithSubDirectories(
                    Id.ToString(),
                    library.Version.ToNormalizedString().ToLowerInvariant())
                .WithFile(dotnetToolSettings.Path);

        var configuration = ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);
        return configuration;
    }

    private static LockFileTargetLibrary FindLibraryInLockFile(LockFile lockFile, PackageId id)
    {
        return lockFile
            ?.Targets?.SingleOrDefault(t => t.RuntimeIdentifier != null)
            ?.Libraries?.SingleOrDefault(l =>
                string.Compare(l.Name, id.ToString(), StringComparison.OrdinalIgnoreCase) == 0);
    }

    private LockFileTargetLibrary FindLibraryInLockFile(LockFile lockFile)
    {
        return FindLibraryInLockFile(lockFile, Id);
    }

    private static LockFileItem FindItemInTargetLibrary(LockFileTargetLibrary library, string targetRelativeFilePath)
    {
        return library
            ?.ToolsAssemblies
            ?.SingleOrDefault(t => LockFileMatcher.MatchesFile(t, targetRelativeFilePath));
    }

}
