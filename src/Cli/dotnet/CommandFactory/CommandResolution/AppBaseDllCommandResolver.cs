﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Utils.Extensions;

namespace Microsoft.DotNet.Cli.CommandFactory.CommandResolution;

public class AppBaseDllCommandResolver : ICommandResolver
{
    public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
    {
        if (commandResolverArguments.CommandName == null)
        {
            return null;
        }
        if (commandResolverArguments.CommandName.EndsWith(FileNameSuffixes.DotNet.DynamicLib))
        {
            var localPath = Path.Combine(AppContext.BaseDirectory,
                commandResolverArguments.CommandName);
            if (File.Exists(localPath))
            {
                var escapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(
                    new[] { localPath }
                    .Concat(commandResolverArguments.CommandArguments.OrEmptyIfNull()));
                return new CommandSpec(
                    new Muxer().MuxerPath,
                    escapedArgs);
            }
        }
        return null;
    }
}
