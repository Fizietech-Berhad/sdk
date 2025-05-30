﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.Deployment.DotNet.Releases;
using Microsoft.DotNet.Cli.Commands.Workload.Install;
using Microsoft.DotNet.Cli.Commands.Workload.List;
using Microsoft.DotNet.Cli.Commands.Workload.Uninstall;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Utils.Extensions;
using Microsoft.NET.Sdk.WorkloadManifestReader;

namespace Microsoft.DotNet.Cli.Commands.Workload.Clean;

internal class WorkloadCleanCommand : WorkloadCommandBase
{
    private readonly bool _cleanAll;

    private readonly string _dotnetPath;
    private readonly string _userProfileDir;

    private readonly ReleaseVersion _sdkVersion;
    private readonly IInstaller _workloadInstaller;
    private readonly IWorkloadResolver _workloadResolver;
    protected readonly IWorkloadResolverFactory _workloadResolverFactory;

    public WorkloadCleanCommand(
        ParseResult parseResult,
        IReporter? reporter = null,
        IWorkloadResolverFactory? workloadResolverFactory = null) : base(parseResult, reporter: reporter)
    {
        _cleanAll = parseResult.GetValue(WorkloadCleanCommandParser.CleanAllOption);

        _workloadResolverFactory = workloadResolverFactory ?? new WorkloadResolverFactory();

        if (!string.IsNullOrEmpty(parseResult.GetValue(WorkloadUninstallCommandParser.VersionOption)))
        {
            throw new GracefulException(CliCommandStrings.SdkVersionOptionNotSupported);
        }

        var creationResult = _workloadResolverFactory.Create();

        _dotnetPath = creationResult.DotnetPath;
        _userProfileDir = creationResult.UserProfileDir;
        _workloadResolver = creationResult.WorkloadResolver;
        _sdkVersion = creationResult.SdkVersion;

        var sdkFeatureBand = new SdkFeatureBand(_sdkVersion);
        _workloadInstaller = WorkloadInstallerFactory.GetWorkloadInstaller(Reporter, sdkFeatureBand, creationResult.WorkloadResolver, Verbosity, creationResult.UserProfileDir, VerifySignatures, PackageDownloader, creationResult.DotnetPath);
    }

    public override int Execute()
    {
        ExecuteGarbageCollection();
        return 0;
    }

    private void ExecuteGarbageCollection()
    {
        _workloadInstaller.GarbageCollect(workloadVersion => _workloadResolverFactory.CreateForWorkloadSet(_dotnetPath, _sdkVersion.ToString(), _userProfileDir, workloadVersion),
            cleanAllPacks: _cleanAll);

        DisplayUninstallableVSWorkloads();
    }

    /// <summary>
    /// Print VS Workloads with the same machine arch which can't be uninstalled through the SDK CLI to increase user awareness that they must uninstall via VS.
    /// </summary>
    private void DisplayUninstallableVSWorkloads()
    {
#if !DOT_NET_BUILD_FROM_SOURCE
        // We don't want to print MSI related content in a file-based installation.
        if (!(_workloadInstaller.GetType() == typeof(NetSdkMsiInstallerClient)))
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            // All VS Workloads should have a corresponding MSI based SDK. This means we can pull all of the VS SDK feature bands using MSI/VS related registry keys.
            var installedSDKVersionsWithPotentialVSRecords = MsiInstallerBase.GetInstalledSdkVersions();
            HashSet<string> vsWorkloadUninstallWarnings = [];

            string defaultDotnetWinPath = MsiInstallerBase.GetDotNetHome();
            foreach (string sdkVersion in installedSDKVersionsWithPotentialVSRecords)
            {
                try
                {
#pragma warning disable CS8604 // We error in the constructor if the dotnet path is null.

                    // We don't know if the dotnet installation for the other bands is in a different directory than the current dotnet; check the default directory if it isn't.
                    var bandedDotnetPath = Path.Exists(Path.Combine(_dotnetPath, "sdk", sdkVersion)) ? _dotnetPath : defaultDotnetWinPath;

                    if (!Path.Exists(bandedDotnetPath))
                    {
                        Reporter.WriteLine(string.Format(CliCommandStrings.CannotAnalyzeVSWorkloadBand, sdkVersion, _dotnetPath, defaultDotnetWinPath).Yellow());
                        continue;
                    }

                    var workloadManifestProvider = new SdkDirectoryWorkloadManifestProvider(bandedDotnetPath, sdkVersion, _userProfileDir, SdkDirectoryWorkloadManifestProvider.GetGlobalJsonPath(Environment.CurrentDirectory));
                    var bandedResolver = WorkloadResolver.Create(workloadManifestProvider, bandedDotnetPath, sdkVersion.ToString(), _userProfileDir);
#pragma warning restore CS8604

                    InstalledWorkloadsCollection vsWorkloads = new();
                    VisualStudioWorkloads.GetInstalledWorkloads(bandedResolver, vsWorkloads, _cleanAll ? null : new SdkFeatureBand(sdkVersion));
                    foreach (var vsWorkload in vsWorkloads.AsEnumerable())
                    {
                        vsWorkloadUninstallWarnings.Add(string.Format(CliCommandStrings.VSWorkloadNotRemoved, $"{vsWorkload.Key}", $"{vsWorkload.Value}"));
                    }
                }
                catch (WorkloadManifestException ex)
                {
                    // Limitation: We don't know the dotnetPath of the other feature bands when making the manifestProvider and resolvers.
                    // This can cause the manifest resolver to fail as it may look for manifests in an invalid path.
                    // It can theoretically be customized, but that is not currently supported for workloads with VS.
                    Reporter.WriteLine(string.Format(CliCommandStrings.CannotAnalyzeVSWorkloadBand, sdkVersion, _dotnetPath, defaultDotnetWinPath).Yellow());
                    Utils.Reporter.Verbose.WriteLine(ex.Message);
                }
            }

            foreach (string warning in vsWorkloadUninstallWarnings)
            {
                Reporter.WriteLine(warning.Yellow());
            }
        }
#endif
    }
}
