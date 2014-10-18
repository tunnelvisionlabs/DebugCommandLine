// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.

namespace Tvl.DebugCommandLine
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("238A874D-D659-4517-85D1-06B0E3CF4B7F")]
    internal class DebugCommandLinePackage : Package
    {
    }
}
