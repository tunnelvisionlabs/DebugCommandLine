// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Tvl.DebugCommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("238A874D-D659-4517-85D1-06B0E3CF4B7F")]
    internal class DebugCommandLinePackage : Package
    {
        private static readonly string[] KnownStartupProperties = { "CommandArguments", "StartArguments" };
        private static readonly string SettingsCollectionName = "DebugCommandLine";
        private static readonly string RecentCommandLinesCollectionName = SettingsCollectionName + @"\RecentCommandLines";
        private static readonly int maxRecentCommandLineCount = 15;
        private WritableSettingsStore SettingsStore;

        private ReadOnlyCollection<string> RecentCommandLines = new ReadOnlyCollection<string>(new string[0]);

        protected override void Initialize()
        {
            base.Initialize();

            OleMenuCommandService menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (menuCommandService != null)
            {
                // This is the drop down combo box itself
                CommandID comboBoxCommandID = new DebugCommandLineCommandID(DebugCommandLineCommand.DebugCommandLineCombo);
                OleMenuCommand comboBoxCommand = new OleMenuCommand(HandleInvokeCombo, HandleChangeCombo, HandleBeforeQueryStatusCombo, comboBoxCommandID);
                menuCommandService.AddCommand(comboBoxCommand);

                // This is the special command to get the list of drop down items
                CommandID comboBoxGetListCommandID = new DebugCommandLineCommandID(DebugCommandLineCommand.DebugCommandLineComboGetList);
                OleMenuCommand comboBoxGetListCommand = new OleMenuCommand(HandleInvokeComboGetList, comboBoxGetListCommandID);
                menuCommandService.AddCommand(comboBoxGetListCommand);
            }

            var shellSettingsManager = new ShellSettingsManager(this);
            SettingsStore = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            LoadSettings();
        }

        private void LoadSettings()
        {
            var recentCommands = new List<string>(RecentCommandLines);
            for (int i = 0; i < maxRecentCommandLineCount; i++)
            {
                if (!SettingsStore.PropertyExists(RecentCommandLinesCollectionName, i.ToString()))
                    break;

                var commandLine = SettingsStore.GetString(RecentCommandLinesCollectionName, i.ToString());
                recentCommands.Add(commandLine);
            }

            RecentCommandLines = new ReadOnlyCollection<string>(recentCommands);
        }

        private void SaveSettings()
        {
            if (SettingsStore.CollectionExists(RecentCommandLinesCollectionName))
                SettingsStore.DeleteCollection(RecentCommandLinesCollectionName);

            SettingsStore.CreateCollection(RecentCommandLinesCollectionName);
            for (int i = 0; i < RecentCommandLines.Count; i++)
            {
                SettingsStore.SetString(RecentCommandLinesCollectionName, i.ToString(), RecentCommandLines[i]);
            }
        }

        private void HandleInvokeCombo(object sender, EventArgs e)
        {
            OleMenuCmdEventArgs oleEventArgs = e as OleMenuCmdEventArgs;
            if (oleEventArgs == null)
                throw new ArgumentException("EventArgs required.");

            string newChoice = oleEventArgs.InValue as string;
            if (newChoice != null)
            {
                SetStartupCommandArguments(newChoice);
                SetMostRecentString(newChoice);
            }

            if (oleEventArgs.OutValue != IntPtr.Zero)
            {
                string commandArguments = TryGetStartupCommandArguments();
                SetMostRecentString(commandArguments);
                Marshal.GetNativeVariantForObject(commandArguments, oleEventArgs.OutValue);
                return;
            }
        }

        private void HandleChangeCombo(object sender, EventArgs e)
        {
        }

        private void HandleBeforeQueryStatusCombo(object sender, EventArgs e)
        {
            OleMenuCommand command = sender as OleMenuCommand;
            if (command == null)
                return;

            DebugCommandLineCommandID commandID = command.CommandID as DebugCommandLineCommandID;
            if (commandID == null || commandID.ID != DebugCommandLineCommand.DebugCommandLineCombo)
                return;

            command.Supported = true;

            try
            {
                command.Enabled = !string.IsNullOrEmpty(TryGetStartupCommandArgumentsPropertyName());
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                    throw;

                command.Enabled = false;
            }
        }

        private void HandleInvokeComboGetList(object sender, EventArgs e)
        {
            OleMenuCmdEventArgs oleEventArgs = e as OleMenuCmdEventArgs;
            if (oleEventArgs == null)
                throw new ArgumentException("EventArgs required.");

            if (oleEventArgs.InValue != null)
                throw new ArgumentException();

            if (oleEventArgs.OutValue == IntPtr.Zero)
                throw new ArgumentException();

            Marshal.GetNativeVariantForObject(RecentCommandLines.ToArray(), oleEventArgs.OutValue);
        }

        private static EnvDTE.Properties TryGetDtePropertiesFromHierarchy(IVsHierarchy hierarchy)
        {
            try
            {
                EnvDTE.Project project = TryGetExtensibilityObject(hierarchy) as EnvDTE.Project;
                if (project == null)
                    return null;

                EnvDTE.ConfigurationManager configurationManager = project.ConfigurationManager;
                if (configurationManager == null)
                    return null;

                EnvDTE.Configuration activeConfiguration = configurationManager.ActiveConfiguration;
                if (activeConfiguration == null)
                    return null;

                return activeConfiguration.Properties;
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                    throw;

                return null;
            }
        }

        private static object TryGetExtensibilityObject(IVsHierarchy hierarchy, uint itemId = (uint)VSConstants.VSITEMID.Root)
        {
            try
            {
                object obj;
                int hr = hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
                if (ErrorHandler.Failed(hr))
                    return null;

                return obj;
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                    throw;

                return null;
            }
        }

        private EnvDTE.Properties TryGetStartupProjectProperties()
        {
            try
            {
                IVsSolutionBuildManager solutionBuildManager = GetGlobalService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
                if (solutionBuildManager == null)
                    return null;

                IVsHierarchy startupProject;
                if (ErrorHandler.Failed(solutionBuildManager.get_StartupProject(out startupProject)) || startupProject == null)
                    return null;

                EnvDTE.Properties properties = TryGetDtePropertiesFromHierarchy(startupProject);
                return properties;
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                    throw;

                return null;
            }
        }

        private string TryGetStartupCommandArgumentsPropertyName()
        {
            try
            {
                EnvDTE.Properties properties = TryGetStartupProjectProperties();
                if (properties == null)
                    return null;

                return KnownStartupProperties.FirstOrDefault(i => properties.OfType<EnvDTE.Property>().Any(property => string.Equals(i, property.Name, StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                    throw;

                return null;
            }
        }

        private string TryGetStartupCommandArguments()
        {
            EnvDTE.Properties properties = TryGetStartupProjectProperties();
            if (properties == null)
                return null;

            try
            {
                // Iterating over the properties has proven much more reliable than calling Item()
                foreach (EnvDTE.Property property in properties)
                {
                    foreach (var propertyName in KnownStartupProperties)
                    {
                        if (string.Equals(propertyName, property.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            return property.Value as string;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                    throw;

                return null;
            }
        }

        private void SetStartupCommandArguments(string value)
        {
            EnvDTE.Properties properties = TryGetStartupProjectProperties();
            if (properties == null)
                throw new NotSupportedException("No startup project is set, or it does not support setting properties.");

            // Iterating over the properties has proven much more reliable than calling Item()
            foreach (EnvDTE.Property property in properties)
            {
                foreach (var propertyName in KnownStartupProperties)
                {
                    if (string.Equals(propertyName, property.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value = value ?? string.Empty;
                        return;
                    }
                }
            }

            throw new NotSupportedException("Could not identify the startup arguments property for the project.");
        }

        private void SetMostRecentString(string command)
        {
            List<string> recentCommands = new List<string>(RecentCommandLines);
            recentCommands.Remove(command);
            recentCommands.Insert(0, command);
            while (recentCommands.Count > maxRecentCommandLineCount)
                recentCommands.RemoveAt(recentCommands.Count - 1);

            RecentCommandLines = new ReadOnlyCollection<string>(recentCommands);
            SaveSettings();
        }
    }
}
