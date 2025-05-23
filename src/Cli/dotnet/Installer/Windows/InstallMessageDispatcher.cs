﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO.Pipes;
using System.Runtime.Versioning;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using static Microsoft.Win32.Msi.Error;

namespace Microsoft.DotNet.Cli.Installer.Windows;

/// <summary>
/// Provides basic interprocess communication primitives for sending and receiving install messages.
/// over a <see cref="PipeStream"/>.
/// </summary>
#if NETCOREAPP
[SupportedOSPlatform("windows")]
#endif
internal class InstallMessageDispatcher(PipeStream pipeStream) : PipeStreamMessageDispatcherBase(pipeStream), IInstallMessageDispatcher
{

    /// <summary>
    /// Sends a message and blocks until a reply is received.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>The response message.</returns>
    public InstallResponseMessage Send(InstallRequestMessage message)
    {
        WriteMessage(message.ToByteArray());
        return InstallResponseMessage.Create(ReadMessage());
    }

    public void Reply(InstallResponseMessage message)
    {
        WriteMessage(message.ToByteArray());
    }

    public void Reply(Exception e)
    {
        Reply(new InstallResponseMessage { HResult = e.HResult, Message = e.Message });
    }

    /// <summary>
    /// Sends a reply with the specified error.
    /// </summary>
    /// <param name="error">The error code to include in the response message.</param>
    public void Reply(uint error)
    {
        Reply(new InstallResponseMessage { Error = error });
    }


    public void ReplySuccess(string message)
    {
        Reply(new InstallResponseMessage { Message = message, HResult = S_OK, Error = SUCCESS });
    }

    public InstallRequestMessage ReceiveRequest()
    {
        return InstallRequestMessage.Create(ReadMessage());
    }

    /// <summary>
    /// Sends an <see cref="InstallRequestMessage"/> to update the MSI package cache and blocks
    /// until a response is received from the server.
    /// </summary>
    /// <param name="requestType">The action to perform</param>
    /// <param name="manifestPath">The JSON manifest associated with the MSI.</param>
    /// <param name="packageId">The ID of the workload pack package containing an MSI.</param>
    /// <param name="packageVersion">The package version.</param>
    /// <returns></returns>
    public InstallResponseMessage SendCacheRequest(InstallRequestType requestType, string manifestPath,
        string packageId, string packageVersion)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = requestType,
            ManifestPath = manifestPath,
            PackageId = packageId,
            PackageVersion = packageVersion,
        });
    }

    /// <summary>
    /// Sends an <see cref="InstallRequestMessage"/> to modify the dependent of a provider key and
    /// blocks until a response is received from the server.
    /// </summary>
    /// <param name="requestType">The action to perform on the provider key.</param>
    /// <param name="providerKeyName">The name of the provider key.</param>
    /// <param name="dependent">The dependent value.</param>
    /// <returns></returns>
    public InstallResponseMessage SendDependentRequest(InstallRequestType requestType, string providerKeyName, string dependent)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = requestType,
            ProviderKeyName = providerKeyName,
            Dependent = dependent,
        });
    }

    /// <summary>
    /// Sends an <see cref="InstallRequestMessage"/> to install, repair, or uninstall an MSI and
    /// blocks until a response is received from the server.
    /// </summary>
    /// <param name="requestType">The action to perform on the MSI.</param>
    /// <param name="logFile">The log file to created when performing the action.</param>
    /// <param name="packagePath">The path to the MSI package.</param>
    /// <param name="productCode">The product code of the installer package.</param>
    /// <returns></returns>
    public InstallResponseMessage SendMsiRequest(InstallRequestType requestType, string logFile, string packagePath = null, string productCode = null)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = requestType,
            LogFile = logFile,
            PackagePath = packagePath,
            ProductCode = productCode,
        });
    }

    /// <summary>
    /// Sends an <see cref="InstallRequestMessage"/> to shut down the server.
    /// </summary>
    /// <returns></returns>
    public InstallResponseMessage SendShutdownRequest()
    {
        return Send(new InstallRequestMessage { RequestType = InstallRequestType.Shutdown });
    }

    /// <summary>
    /// Sends an <see cref="InstallRequestMessage"/> to create or delete a workload installation record.
    /// </summary>
    /// <param name="requestType">The action to perform on the workload record.</param>
    /// <param name="workloadId">The workload identifier.</param>
    /// <param name="sdkFeatureBand">The SDK feature band associated with the record.</param>
    /// <returns></returns>
    public InstallResponseMessage SendWorkloadRecordRequest(InstallRequestType requestType, WorkloadId workloadId, SdkFeatureBand sdkFeatureBand)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = requestType,
            WorkloadId = workloadId.ToString(),
            SdkFeatureBand = sdkFeatureBand.ToString(),
        });
    }

    /// <summary>
    /// Send an <see cref="InstallRequestMessage"/> to delete the install state file.
    /// </summary>
    /// <param name="sdkFeatureBand">The SDK feature band of the install state file to delete.</param>
    /// <returns></returns>
    public InstallResponseMessage SendRemoveManifestsFromInstallStateFileRequest(SdkFeatureBand sdkFeatureBand)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = InstallRequestType.RemoveManifestsFromInstallStateFile,
            SdkFeatureBand = sdkFeatureBand.ToString(),
        });
    }

    /// <summary>
    /// Sends an <see cref="InstallRequestMessage"/> to write the install state file.
    /// </summary>
    /// <param name="sdkFeatureBand">The SDK feature band of the install state file to write</param>
    /// <param name="value">A multi-line string containing the formatted JSON data to write.</param>
    /// <returns></returns>
    public InstallResponseMessage SendSaveInstallStateManifestVersions(SdkFeatureBand sdkFeatureBand, Dictionary<string, string> manifestContents)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = InstallRequestType.SaveInstallStateManifestVersions,
            SdkFeatureBand = sdkFeatureBand.ToString(),
            InstallStateManifestVersions = manifestContents
        });
    }

    /// <summary>
    /// Send an <see cref="InstallRequestMessage"/> to adjust the mode used for installing and updating workloads
    /// </summary>
    /// <param name="sdkFeatureBand">The SDK feature band of the install state file to write</param>
    /// <param name="newMode">Whether to use workload sets or not</param>
    /// <returns></returns>
    public InstallResponseMessage SendUpdateWorkloadModeRequest(SdkFeatureBand sdkFeatureBand, bool? newMode)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = InstallRequestType.AdjustWorkloadMode,
            SdkFeatureBand = sdkFeatureBand.ToString(),
            UseWorkloadSets = newMode,
        });
    }

    /// <summary>
    /// Send an <see cref="InstallRequestMessage"/> to adjust the workload set version used for installing and updating workloads
    /// </summary>
    /// <param name="sdkFeatureBand">The SDK feature band of the install state file to write</param>
    /// <param name="newVersion">The workload set version</param>
    /// <returns></returns>
    public InstallResponseMessage SendUpdateWorkloadSetRequest(SdkFeatureBand sdkFeatureBand, string newVersion)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = InstallRequestType.AdjustWorkloadSetVersion,
            SdkFeatureBand = sdkFeatureBand.ToString(),
            WorkloadSetVersion = newVersion,
        });
    }

    public InstallResponseMessage SendRecordWorkloadSetInGlobalJsonRequest(SdkFeatureBand sdkFeatureBand, string globalJsonPath, string workloadSetVersion)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = InstallRequestType.RecordWorkloadSetInGlobalJson,
            SdkFeatureBand = sdkFeatureBand.ToString(),
            GlobalJsonPath = globalJsonPath,
            WorkloadSetVersion = workloadSetVersion,
        });
    }

    public InstallResponseMessage SendGetGlobalJsonWorkloadSetVersionsRequest(SdkFeatureBand sdkFeatureBand)
    {
        return Send(new InstallRequestMessage
        {
            RequestType = InstallRequestType.GetGlobalJsonWorkloadSetVersions,
            SdkFeatureBand = sdkFeatureBand.ToString(),
        });
    }
}
