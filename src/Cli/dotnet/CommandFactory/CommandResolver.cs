﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.DotNet.Cli.CommandFactory.CommandResolution;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.CommandFactory;

internal class CommandResolver
{
    public static CommandSpec TryResolveCommandSpec(
        string commandName,
        IEnumerable<string> args,
        NuGetFramework framework = null,
        string configuration = Constants.DefaultConfiguration,
        string outputPath = null,
        string applicationName = null)
    {
        return TryResolveCommandSpec(
            new DefaultCommandResolverPolicy(),
            commandName,
            args,
            framework,
            configuration,
            outputPath,
            applicationName);
    }

    public static CommandSpec TryResolveCommandSpec(
        ICommandResolverPolicy commandResolverPolicy,
        string commandName,
        IEnumerable<string> args,
        NuGetFramework framework = null,
        string configuration = Constants.DefaultConfiguration,
        string outputPath = null,
        string applicationName = null,
        string currentWorkingDirectory = null)
    {
        var commandResolverArgs = new CommandResolverArguments
        {
            CommandName = commandName,
            CommandArguments = args,
            Framework = framework,
            ProjectDirectory = currentWorkingDirectory ?? Directory.GetCurrentDirectory(),
            Configuration = configuration,
            OutputPath = outputPath,
            ApplicationName = applicationName
        };

        var defaultCommandResolver = commandResolverPolicy.CreateCommandResolver(currentWorkingDirectory);

        return defaultCommandResolver.Resolve(commandResolverArgs);
    }
}

