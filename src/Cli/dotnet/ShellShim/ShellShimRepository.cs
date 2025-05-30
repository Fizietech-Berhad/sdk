﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Cli.ShellShim;

internal class ShellShimRepository(
    DirectoryPath shimsDirectory,
    string appHostSourceDirectory,
    IFileSystem fileSystem = null,
    IAppHostShellShimMaker appHostShellShimMaker = null,
    IFilePermissionSetter filePermissionSetter = null) : IShellShimRepository
{
    private readonly DirectoryPath _shimsDirectory = shimsDirectory;
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystemWrapper();
    private readonly IAppHostShellShimMaker _appHostShellShimMaker = appHostShellShimMaker ?? new AppHostShellShimMaker(appHostSourceDirectory: appHostSourceDirectory);
    private readonly IFilePermissionSetter _filePermissionSetter = filePermissionSetter ?? new FilePermissionSetter();

    public void CreateShim(FilePath targetExecutablePath, ToolCommandName commandName, IReadOnlyList<FilePath> packagedShims = null)
    {
        if (string.IsNullOrEmpty(targetExecutablePath.Value))
        {
            throw new ShellShimException(CliStrings.CannotCreateShimForEmptyExecutablePath);
        }

        if (ShimExists(commandName))
        {
            throw new ShellShimException(
                string.Format(
                    CliStrings.ShellShimConflict,
                    commandName));
        }

        TransactionalAction.Run(
            action: () =>
            {
                try
                {
                    if (!_fileSystem.Directory.Exists(_shimsDirectory.Value))
                    {
                        _fileSystem.Directory.CreateDirectory(_shimsDirectory.Value);
                    }

                    if (TryGetPackagedShim(packagedShims, commandName, out FilePath? packagedShim))
                    {
                        _fileSystem.File.Copy(packagedShim.Value.Value, GetShimPath(commandName).Value);
                        _filePermissionSetter.SetUserExecutionPermission(GetShimPath(commandName).Value);
                    }
                    else
                    {
                        _appHostShellShimMaker.CreateApphostShellShim(
                            targetExecutablePath,
                            GetShimPath(commandName));
                    }
                }
                catch (FilePermissionSettingException ex)
                {
                    throw new ShellShimException(
                            string.Format(CliStrings.FailedSettingShimPermissions, ex.Message));
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    throw new ShellShimException(
                        string.Format(
                            CliStrings.FailedToCreateShellShim,
                            commandName,
                            ex.Message
                        ),
                        ex);
                }
            },
            rollback: () =>
            {
                foreach (var file in GetShimFiles(commandName).Where(f => _fileSystem.File.Exists(f.Value)))
                {
                    File.Delete(file.Value);
                }
            });
    }

    public void RemoveShim(ToolCommandName commandName)
    {
        var files = new Dictionary<string, string>();
        TransactionalAction.Run(
            action: () =>
            {
                try
                {
                    foreach (var file in GetShimFiles(commandName).Where(f => _fileSystem.File.Exists(f.Value)))
                    {
                        var tempPath = Path.Combine(_fileSystem.Directory.CreateTemporarySubdirectory(), Path.GetRandomFileName());
                        FileAccessRetrier.RetryOnMoveAccessFailure(() => _fileSystem.File.Move(file.Value, tempPath));
                        files[file.Value] = tempPath;
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    throw new ShellShimException(
                        string.Format(
                            CliStrings.FailedToRemoveShellShim,
                            commandName.ToString(),
                            ex.Message
                        ),
                        ex);
                }
            },
            commit: () =>
            {
                foreach (var value in files.Values)
                {
                    _fileSystem.File.Delete(value);
                }
            },
            rollback: () =>
            {
                foreach (var kvp in files)
                {
                    FileAccessRetrier.RetryOnMoveAccessFailure(() => _fileSystem.File.Move(kvp.Value, kvp.Key));
                }
            });
    }

    private class StartupOptions
    {
        public string appRoot { get; set; }
    }

    private class RootObject
    {
        public StartupOptions startupOptions { get; set; }
    }

    private bool ShimExists(ToolCommandName commandName)
    {
        return GetShimFiles(commandName).Any(p => _fileSystem.File.Exists(p.Value));
    }

    private IEnumerable<FilePath> GetShimFiles(ToolCommandName commandName)
    {
        yield return GetShimPath(commandName);
    }

    private FilePath GetShimPath(ToolCommandName commandName)
    {
        if (OperatingSystem.IsWindows())
        {
            return _shimsDirectory.WithFile(commandName.Value + ".exe");
        }
        else
        {
            return _shimsDirectory.WithFile(commandName.Value);
        }
    }

    private bool TryGetPackagedShim(
        IReadOnlyList<FilePath> packagedShims,
        ToolCommandName commandName,
        out FilePath? packagedShim)
    {
        packagedShim = null;

        if (packagedShims != null && packagedShims.Count > 0)
        {
            FilePath[] candidatepackagedShim = [.. packagedShims.Where(s => string.Equals(Path.GetFileName(s.Value), Path.GetFileName(GetShimPath(commandName).Value)))];

            if (candidatepackagedShim.Length > 1)
            {
                throw new ShellShimException(
                    string.Format(
                        CliStrings.MoreThanOnePackagedShimAvailable,
                        string.Join(';', candidatepackagedShim)));
            }

            if (candidatepackagedShim.Length == 1)
            {
                packagedShim = candidatepackagedShim.Single();
                return true;
            }
        }

        return false;
    }
}
