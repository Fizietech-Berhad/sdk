﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Cli.Commands.Fsi;

public class FsiCommand
{
    public static int Run(string[] args)
    {
        DebugHelper.HandleDebugSwitch(ref args);
        return new FsiForwardingApp(args).Execute();
    }
}
