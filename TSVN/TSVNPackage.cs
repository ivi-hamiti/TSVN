﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Process = System.Diagnostics.Process;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Microsoft.Win32;
using System.Windows.Forms;

namespace SamirBoulema.TSVN
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.9", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidTSVNPkgString)]
    [ProvideToolWindow(typeof(SamirBoulema.TSVN.TSVNToolWindow))]
    public sealed class TSVNPackage : Package
    {
        public DTE dte;
        private string _solutionDir;
        private string _currentFilePath;
        private string tortoiseProc;


        #region Package Members
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            dte = (DTE)GetService(typeof(DTE));

            tortoiseProc = GetTortoiseSVNProc();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null == mcs) return;

            mcs.AddCommand(CreateCommand(ShowChangesCommand, PkgCmdIdList.ShowChangesCommand));
            mcs.AddCommand(CreateCommand(UpdateCommand, PkgCmdIdList.UpdateCommand));
            mcs.AddCommand(CreateCommand(UpdateToRevisionCommand, PkgCmdIdList.UpdateToRevisionCommand));
            mcs.AddCommand(CreateCommand(CommitCommand, PkgCmdIdList.CommitCommand));
            mcs.AddCommand(CreateCommand(ShowLogCommand, PkgCmdIdList.ShowLogCommand));
            mcs.AddCommand(CreateCommand(CreatePatchCommand, PkgCmdIdList.CreatePatchCommand));
            mcs.AddCommand(CreateCommand(ApplyPatchCommand, PkgCmdIdList.ApplyPatchCommand));
            mcs.AddCommand(CreateCommand(RevertCommand, PkgCmdIdList.RevertCommand));
            mcs.AddCommand(CreateCommand(DiskBrowserCommand, PkgCmdIdList.DiskBrowser));
            mcs.AddCommand(CreateCommand(RepoBrowserCommand, PkgCmdIdList.RepoBrowser));
            mcs.AddCommand(CreateCommand(BranchCommand, PkgCmdIdList.BranchCommand));
            mcs.AddCommand(CreateCommand(SwitchCommand, PkgCmdIdList.SwitchCommand));
            mcs.AddCommand(CreateCommand(MergeCommand, PkgCmdIdList.MergeCommand));
            mcs.AddCommand(CreateCommand(DifferencesCommand, PkgCmdIdList.DifferencesCommand));
            mcs.AddCommand(CreateCommand(BlameCommand, PkgCmdIdList.BlameCommand));
            mcs.AddCommand(CreateCommand(ShowLogFileCommand, PkgCmdIdList.ShowLogFileCommand));
            mcs.AddCommand(CreateCommand(CleanupCommand, PkgCmdIdList.CleanupCommand));
            mcs.AddCommand(CreateCommand(DiskBrowserFileCommand, PkgCmdIdList.DiskBrowserFileCommand));
            mcs.AddCommand(CreateCommand(RepoBrowserFileCommand, PkgCmdIdList.RepoBrowserFileCommand));
            mcs.AddCommand(CreateCommand(MergeFileCommand, PkgCmdIdList.MergeFileCommand));
            mcs.AddCommand(CreateCommand(UpdateToRevisionFileCommand, PkgCmdIdList.UpdateToRevisionFileCommand));
            mcs.AddCommand(CreateCommand(PropertiesCommand, PkgCmdIdList.PropertiesCommand));
            mcs.AddCommand(CreateCommand(UpdateFileCommand, PkgCmdIdList.UpdateFileCommand));
            mcs.AddCommand(CreateCommand(CommitFileCommand, PkgCmdIdList.CommitFileCommand));
            mcs.AddCommand(CreateCommand(DiffPreviousCommand, PkgCmdIdList.DiffPreviousCommand));
            mcs.AddCommand(CreateCommand(RevertFileCommand, PkgCmdIdList.RevertFileCommand));
            mcs.AddCommand(CreateCommand(AddFileCommand, PkgCmdIdList.AddFileCommand));

            OleMenuCommand tsvnMenu = CreateCommand(null, PkgCmdIdList.TSvnMenu);
            OleMenuCommand tsvnContextMenu = CreateCommand(null, PkgCmdIdList.TSvnContextMenu);
            switch (dte.Version)
            {
                case "11.0":
                case "12.0":
                    tsvnMenu.Text = "TSVN";
                    tsvnContextMenu.Text = "TSVN";
                    break;
                default:
                    tsvnMenu.Text = "Tsvn";
                    tsvnContextMenu.Text = "Tsvn";
                    break;
            }
            mcs.AddCommand(tsvnMenu);
            mcs.AddCommand(tsvnContextMenu);
            SamirBoulema.TSVN.TSVNToolWindowCommand.Initialize(this);
        }
        #endregion

        private static void StartProcess(string application, string args)
        {
            try
            {
                Process.Start(application, args);
            }
            catch (Exception)
            {
                MessageBox.Show("TortoiseSVN not found. Did you install TortoiseSVN?", "TortoiseSVN not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static OleMenuCommand CreateCommand(EventHandler handler, uint commandId)
        {
            CommandID menuCommandId = new CommandID(GuidList.guidTSVNCmdSet, (int)commandId);
            OleMenuCommand menuItem = new OleMenuCommand(handler, menuCommandId);
            return menuItem;
        }

        private string GetSolutionDir()
        {
            string fileName = dte.Solution.FullName;
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("Please open a solution first", "TSVN error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                var path = Path.GetDirectoryName(fileName);
                return FindSvndir(path);
            }
            return string.Empty;
        }

        private static string FindSvndir(string path)
        {
            try
            {
                var di = new DirectoryInfo(path);
                if (di.GetDirectories().Any(d => d.Name.Equals(".svn")))
                {
                    return di.FullName;
                }
                if (di.Parent != null)
                {
                    return FindSvndir(di.Parent.FullName);
                }
                throw new FileNotFoundException("Unable to find .svn directory.\nIs your solution under SVN source control?");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "TSVN error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return string.Empty;      
        }

        public string GetTortoiseSVNProc()
        {
            return (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\TortoiseSVN", "ProcPath", @"C:\Program Files\TortoiseSVN\bin\TortoiseProc.exe");
        }

        public object GetServiceHelper(Type type)
        {
            return GetService(type);
        }

        #region Button Commands
        private void ShowChangesCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:repostatus /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void UpdateCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            dte.ExecuteCommand("File.SaveAll", string.Empty);
            StartProcess(tortoiseProc, string.Format("/command:update /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void UpdateFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            dte.ActiveDocument.Save();
            StartProcess(tortoiseProc, string.Format("/command:update /path:\"{0}\" /closeonend:0", _currentFilePath));
        }

        private void UpdateToRevisionCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            dte.ExecuteCommand("File.SaveAll", string.Empty);
            StartProcess(tortoiseProc, string.Format("/command:update /path:\"{0}\" /rev /closeonend:0", _solutionDir));
        }

        private void UpdateToRevisionFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            dte.ActiveDocument.Save();
            StartProcess(tortoiseProc, string.Format("/command:update /path:\"{0}\" /rev /closeonend:0", _currentFilePath));
        }

        private void PropertiesCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:properties /path:\"{0}\" /closeonend:0", _currentFilePath));
        }

        private void CommitCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            dte.ExecuteCommand("File.SaveAll", string.Empty);
            StartProcess(tortoiseProc, string.Format("/command:commit /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void CommitFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            dte.ActiveDocument.Save();
            StartProcess(tortoiseProc, string.Format("/command:commit /path:\"{0}\" /closeonend:0", _currentFilePath));
        }

        private void ShowLogCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:log /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void ShowLogFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:log /path:\"{0}\" /closeonend:0", _currentFilePath));
        }

        private void CreatePatchCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:createpatch /path:\"{0}\" /noview /closeonend:0", _solutionDir));
        }

        private void ApplyPatchCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = Resources.PatchFileFilterString,
                FilterIndex = 1,
                Multiselect = false
            };
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) return;

            StartProcess("TortoiseMerge.exe", string.Format("/diff:\"{0}\" /patchpath:\"{1}\"", openFileDialog.FileName, _solutionDir));
        }

        private void RevertCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:revert /path:\"{0}\" /closeonend:0", _solutionDir));
        }

        private void RevertFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:revert /path:\"{0}\" /closeonend:0", _currentFilePath));
        }

        private void AddFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            dte.ActiveDocument.Save();
            StartProcess(tortoiseProc, string.Format("/command:add /path:\"{0}\" /closeonend:0", _currentFilePath));
        }

        private void DiskBrowserCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            Process.Start(_solutionDir);
        }
        private void DiskBrowserFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess("explorer.exe", _currentFilePath);
        }

        private void RepoBrowserCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:repobrowser /path:\"{0}\"", _solutionDir));
        }

        private void RepoBrowserFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:repobrowser /path:\"{0}\"", _currentFilePath));
        }

        private void BranchCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:copy /path:\"{0}\"", _solutionDir));
        }

        private void SwitchCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:switch /path:\"{0}\"", _solutionDir));
        }

        private void MergeCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:merge /path:\"{0}\"", _solutionDir));
        }

        private void MergeFileCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:merge /path:\"{0}\"", _currentFilePath));
        }

        private void CleanupCommand(object sender, EventArgs e)
        {
            _solutionDir = GetSolutionDir();  
            if (string.IsNullOrEmpty(_solutionDir)) return;
            StartProcess(tortoiseProc, string.Format("/command:cleanup /path:\"{0}\"", _solutionDir));
        }

        private void DifferencesCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:diff /path:\"{0}\"", _currentFilePath));
        }

        private void DiffPreviousCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:prevdiff /path:\"{0}\"", _currentFilePath));
        }

        private void BlameCommand(object sender, EventArgs e)
        {
            _currentFilePath = dte.SelectedItems.Item(1).ProjectItem.FileNames[0];
            int currentLineIndex = dte.ActiveDocument != null ? ((TextDocument)dte.ActiveDocument.Object(string.Empty)).Selection.CurrentLine : 0;  
            if (string.IsNullOrEmpty(_currentFilePath)) return;
            StartProcess(tortoiseProc, string.Format("/command:blame /path:\"{0}\" /line:{1}", _currentFilePath, currentLineIndex));
        }
        #endregion
    }
}
