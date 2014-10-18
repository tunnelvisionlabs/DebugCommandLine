// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.

namespace Tvl.DebugCommandLine
{
    using System.ComponentModel.Design;

    /// <summary>
    /// This class represents a command ID from the <see cref="DebugCommandLineCommand"/> enumeration.
    /// </summary>
    internal class DebugCommandLineCommandID : CommandID
    {
        public DebugCommandLineCommandID(DebugCommandLineCommand command)
            : base(typeof(DebugCommandLineCommand).GUID, (int)command)
        {
        }

        public new DebugCommandLineCommand ID
        {
            get
            {
                return (DebugCommandLineCommand)base.ID;
            }
        }
    }
}
