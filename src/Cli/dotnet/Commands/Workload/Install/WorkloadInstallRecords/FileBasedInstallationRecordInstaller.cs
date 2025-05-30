// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.NET.Sdk.WorkloadManifestReader;

namespace Microsoft.DotNet.Cli.Commands.Workload.Install.WorkloadInstallRecords;

internal class FileBasedInstallationRecordRepository(string workloadMetadataDir) : IWorkloadInstallationRecordRepository
{
    private readonly string _workloadMetadataDir = workloadMetadataDir;
    private const string InstalledWorkloadDir = "InstalledWorkloads";

    public IEnumerable<SdkFeatureBand> GetFeatureBandsWithInstallationRecords()
    {
        if (Directory.Exists(_workloadMetadataDir))
        {
            var bands = Directory.EnumerateDirectories(_workloadMetadataDir);
            return bands
                .Where(band => Directory.Exists(Path.Combine(band, InstalledWorkloadDir)) && Directory.GetFiles(Path.Combine(band, InstalledWorkloadDir)).Any())
                .Select(path => new SdkFeatureBand(Path.GetFileName(path)));
        }
        else
        {
            return [];
        }
    }

    public IEnumerable<WorkloadId> GetInstalledWorkloads(SdkFeatureBand featureBand)
    {
        var path = Path.Combine(_workloadMetadataDir, featureBand.ToString(), InstalledWorkloadDir);
        if (Directory.Exists(path))
        {
            return Directory.EnumerateFiles(path)
                .Select(file => new WorkloadId(Path.GetFileName(file)));
        }
        else
        {
            return [];
        }
    }

    public void WriteWorkloadInstallationRecord(WorkloadId workloadId, SdkFeatureBand featureBand)
    {
        var path = Path.Combine(_workloadMetadataDir, featureBand.ToString(), InstalledWorkloadDir, workloadId.ToString());
        if (!File.Exists(path))
        {
            var pathDir = Path.GetDirectoryName(path);
            if (pathDir != null && !Directory.Exists(pathDir))
            {
                Directory.CreateDirectory(pathDir);
            }
            File.Create(path).Close();
        }
    }

    public void DeleteWorkloadInstallationRecord(WorkloadId workloadId, SdkFeatureBand featureBand)
    {
        var path = Path.Combine(_workloadMetadataDir, featureBand.ToString(), InstalledWorkloadDir, workloadId.ToString());
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
