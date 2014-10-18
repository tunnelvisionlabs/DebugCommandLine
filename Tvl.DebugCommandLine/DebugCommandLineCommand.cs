// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.

namespace Tvl.DebugCommandLine
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// This enumeration defines the commands that are provided by this extension. The GUID of this enumeration is the
    /// command group which these commands are assigned to.
    /// </summary>
    [Guid("B9B17AA7-66FB-4BEB-AE17-876222AE8390")]
    internal enum DebugCommandLineCommand
    {
        DebugCommandLineCombo = 0,
        DebugCommandLineComboGetList = 1,
    }
}
