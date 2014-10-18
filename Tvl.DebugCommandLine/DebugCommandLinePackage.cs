// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.

namespace Tvl.DebugCommandLine
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("238A874D-D659-4517-85D1-06B0E3CF4B7F")]
    internal class DebugCommandLinePackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();

            OleMenuCommandService menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (menuCommandService != null)
            {
                // This is the drop down combo box itself
                CommandID comboBoxCommandID = new DebugCommandLineCommandID(DebugCommandLineCommand.DebugCommandLineCombo);
                OleMenuCommand comboBoxCommand = new OleMenuCommand(HandleInvokeCombo, comboBoxCommandID);
                menuCommandService.AddCommand(comboBoxCommand);

                // This is the special command to get the list of drop down items
                CommandID comboBoxGetListCommandID = new DebugCommandLineCommandID(DebugCommandLineCommand.DebugCommandLineComboGetList);
                OleMenuCommand comboBoxGetListCommand = new OleMenuCommand(HandleInvokeComboGetList, comboBoxGetListCommandID);
                menuCommandService.AddCommand(comboBoxGetListCommand);
            }
        }

        private void HandleInvokeCombo(object sender, EventArgs e)
        {
        }

        private void HandleInvokeComboGetList(object sender, EventArgs e)
        {
        }
    }
}
