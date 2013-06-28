﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Process = System.Diagnostics.Process;

namespace FundaRealEstateBV.TSVN
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidTSVNPkgString)]
    public sealed class TSVNPackage : Package
    {
        private string _solutionDir;

        #region Package Members
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            DTE dte = (DTE)GetService(typeof(DTE));
            if (!string.IsNullOrEmpty(dte.Solution.FullName))
            {
                var pathParts = dte.Solution.FullName.Split(new[] {"\\"}, StringSplitOptions.None);
                _solutionDir = string.Format("{0}\\{1}\\{2}\\", pathParts[0], pathParts[1], pathParts[2]);
            }         

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                CommandID showChangesCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)PkgCmdIdList.ShowChangesCommand);
                MenuCommand showChangesMenuItem = new MenuCommand(ShowChangesCommand, showChangesCommandId);
                mcs.AddCommand(showChangesMenuItem);

                CommandID updateCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)PkgCmdIdList.UpdateCommand);
                MenuCommand menuItem = new MenuCommand(UpdateCommand, updateCommandId);
                mcs.AddCommand( menuItem );

                CommandID commitCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)PkgCmdIdList.CommitCommand);
                MenuCommand commitMenuItem = new MenuCommand(CommitCommand, commitCommandId);
                mcs.AddCommand(commitMenuItem);

                CommandID showLogCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)PkgCmdIdList.ShowLogCommand);
                MenuCommand showLogMenuItem = new MenuCommand(ShowLogCommand, showLogCommandId);
                mcs.AddCommand(showLogMenuItem);

                CommandID createPatchCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)PkgCmdIdList.CreatePatchCommand);
                MenuCommand createPatchMenuItem = new MenuCommand(CreatePatchCommand, createPatchCommandId);
                mcs.AddCommand(createPatchMenuItem);

                CommandID applyPatchCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)PkgCmdIdList.ApplyPatchCommand);
                MenuCommand applyPatchMenuItem = new MenuCommand(ApplyPatchCommand, applyPatchCommandId);
                mcs.AddCommand(applyPatchMenuItem);

                CommandID revertCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)PkgCmdIdList.RevertCommand);
                MenuCommand revertMenuItem = new MenuCommand(RevertCommand, revertCommandId);
                mcs.AddCommand(revertMenuItem);
            }
        }
        #endregion

        #region Button Commands
        private void ShowChangesCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_solutionDir)) return;
            Process.Start("TortoiseProc.exe", string.Format("/command:repostatus /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void UpdateCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_solutionDir)) return;
            Process.Start("TortoiseProc.exe", string.Format("/command:update /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void CommitCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_solutionDir)) return;
            Process.Start("TortoiseProc.exe", string.Format("/command:commit /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void ShowLogCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_solutionDir)) return;
            Process.Start("TortoiseProc.exe", string.Format("/command:log /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void CreatePatchCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_solutionDir)) return;
            Process.Start("TortoiseProc.exe", string.Format("/command:createPatch /path:\"{0}\" /noview /closeonend:0", _solutionDir));
        }

        private void ApplyPatchCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_solutionDir)) return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Patch Files (.patch)|*.patch|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) return;

            Process.Start("TortoiseMerge.exe", string.Format("/diff:\"{0}\" /patchpath:\"{1}\"", openFileDialog.FileName, _solutionDir));
        }

        private void RevertCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_solutionDir)) return;
            Process.Start("TortoiseProc.exe", string.Format("/command:revert /path:\"{0}\" /closeonend:0", _solutionDir));
        }
        #endregion
    }
}