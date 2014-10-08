using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.TextManager.Interop;
using ImageCruncher;


namespace MadsKristensen.Image_Optimizer_Extension
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
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidImage_Optimizer_ExtensionPkgString)]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    public sealed class Image_Optimizer_ExtensionPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public Image_Optimizer_ExtensionPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        private DTE2 dte;
        private IVsOutputWindowPane pane;
        double allBefore, allAfter, optimized;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();
            dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE)) as DTE2;

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidImage_Optimizer_ExtensionCmdSet, (int)PkgCmdIDList.cmdImageOptimizer);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                CommandID queryStatusCommandID = new CommandID(GuidList.guidDynamicMenuDevelopmentCmdSetPart2, (int)PkgCmdIDList.cmdidQueryStatus);
                OleMenuCommand queryStatusMenuCommand = new OleMenuCommand(Base64ItemCallback, queryStatusCommandID);
                mcs.AddCommand(queryStatusMenuCommand);
                queryStatusMenuCommand.BeforeQueryStatus += new EventHandler(queryStatusMenuCommand_BeforeQueryStatus);

                CommandID queryStatusCommandID2 = new CommandID(GuidList.guidDynamicMenuDevelopmentCmdSetPart3, (int)PkgCmdIDList.cmdImageOptimizerQueryStatus);
                OleMenuCommand queryStatusMenuCommand2 = new OleMenuCommand(MenuItemCallback, queryStatusCommandID2);
                mcs.AddCommand(queryStatusMenuCommand2);
                queryStatusMenuCommand2.BeforeQueryStatus += new EventHandler(queryStatusMenuCommand_BeforeQueryStatus2);

                CommandID queryStatusCommandID5 = new CommandID(GuidList.guidDynamicMenuDevelopmentCmdSetPart5, (int)PkgCmdIDList.embedQueryStatus);
                OleMenuCommand queryStatusMenuCommand5 = new OleMenuCommand(EmbedCallback, queryStatusCommandID5);
                mcs.AddCommand(queryStatusMenuCommand5);
                queryStatusMenuCommand5.BeforeQueryStatus += new EventHandler(queryStatusMenuCommand_BeforeQueryStatus5);
            }
        }

        private void queryStatusMenuCommand_BeforeQueryStatus2(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var items = new List<string>(GetSelectedItemPaths());

            menuCommand.Enabled = items.Count(f => Cruncher.IsSupported(f)) > 0;
        }

        private void queryStatusMenuCommand_BeforeQueryStatus5(object sender, EventArgs e)
        {
            string imageUrl = GetImageUrl();

            OleMenuCommand menuCommand = sender as OleMenuCommand;
            menuCommand.Enabled = imageUrl != null;
        }

        int start, end;

        private string GetImageUrl()
        {
            TextDocument doc = (TextDocument)dte.ActiveDocument.Object("TextDocument");
            EditPoint ep = doc.StartPoint.CreateEditPoint();
            string text = ep.GetText(doc.EndPoint);

            string line = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[doc.Selection.TopPoint.Line - 1];

            start = line.IndexOf("url(", StringComparison.Ordinal) + 4;
            if (start > 4 && start < doc.Selection.AnchorPoint.DisplayColumn)
            {
                end = line.IndexOf(")", start, StringComparison.Ordinal);
                if (end > -1)
                {
                    return line.Substring(start, end - start).Trim();
                }
            }

            return null;
        }

        internal static string GetRootFolder(DTE2 dte)
        {
            Project activeProject = null;
            if (dte.Solution.Projects.Count == 1)
                return dte.Solution.Projects.Item(1).FullName;

            Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }

            return activeProject.FullName;
        }

        private void queryStatusMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var items = new List<string>(GetSelectedItemPaths());
            bool isVisible = false;

            if (items.Count == 1 && File.Exists(items[0]))
            {
                isVisible = Cruncher.IsSupported(items[0]);
            }

            menuCommand.Enabled = isVisible;
        }

        #endregion

        private void Base64ItemCallback(object sender, EventArgs e)
        {
            var items = new List<string>(GetSelectedItemPaths());
            string text = GetDataUri(items[0]);
            Clipboard.SetText(text);
            dte.StatusBar.Text = "Base64 string has now been extracted to the clipboard";
        }

        private static string GetDataUri(string fileName)
        {
            string format = "data:{0};base64,{1}";
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, (int)fs.Length);
                return string.Format(format, "image/" + Path.GetExtension(fileName).Substring(1), Convert.ToBase64String(buffer));
            }
        }

        private void EmbedCallback(object sender, EventArgs e)
        {
            var item = dte.Solution.FindProjectItem(dte.ActiveDocument.FullName);
            string selection = GetImageUrl();
            string imageUrl = selection.Trim(new[] { '\'', '"' });

            if (selection != null)
            {
                if (imageUrl.StartsWith("/", StringComparison.Ordinal))
                    imageUrl = imageUrl.Substring(1);

                string projDir = Directory.GetParent(GetRootFolder(dte)).FullName;
                string filePath = Path.Combine(projDir, imageUrl);
                if (File.Exists(filePath))
                {
                    ((TextDocument)dte.ActiveDocument.Object("TextDocument")).ReplacePattern(selection, GetDataUri(filePath));
                }
                else
                {
                    MessageBox.Show("Couldn't find image on disk");
                }
            }
        }

        /// <summary>
        /// This fuallAfter = 0;nction is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Visible = true;
            dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationGeneral);

            pane = base.GetOutputPane(VSConstants.GUID_OutWindowDebugPane, "Debug");
            pane.Activate();

            optimized = allBefore = allAfter = 0;
            Output("\n------ Image optimization started -----\n");

            Cruncher cruncher = new Cruncher();
            cruncher.Progress += new EventHandler<CruncherEventArgs>(cruncher_Progress);
            cruncher.Completed += new EventHandler<EventArgs>(cruncher_Completed);
            cruncher.BeforeWritingFile += new EventHandler<CruncherEventArgs>(cruncher_BeforeWritingFile);
            cruncher.CrunchImages(GetSelectedItemPaths().ToArray());
        }

        void cruncher_BeforeWritingFile(object sender, CruncherEventArgs e)
        {
            try
            {
                if (dte.SourceControl.IsItemUnderSCC(e.Result.FileName) && !dte.SourceControl.IsItemCheckedOut(e.Result.FileName))
                    dte.SourceControl.CheckOutItem(e.Result.FileName);
            }
            catch { 
                // Do nothing
            }
        }

        private IEnumerable<string> GetSelectedItemPaths()
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (UIHierarchyItem selItem in items)
            {
                var item = selItem.Object as ProjectItem;
                if (item != null)
                {
                    yield return item.Properties.Item("FullPath").Value.ToString();
                }
            }
        }

        private void cruncher_Completed(object sender, EventArgs e)
        {
            Cruncher cruncher = (Cruncher)sender;

            if (cruncher.Optimized > 0)
            {
                double percent = allBefore > 0 ? Math.Round((allBefore - allAfter) / allBefore * 100, 2) : 0;
                Output("\n" + (cruncher.Count - optimized) + " skipped. " + optimized + " optimized. Total savings " + percent + "%");
            }
            else
            {
                Output("\nNo PNG or JPG images in selected folder(s)\n");
            }

            Output("\n------ Image optimization finished -----\n");
            dte.StatusBar.Progress(false);
            dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationGeneral);
        }

        private void cruncher_Progress(object sender, CruncherEventArgs e)
        {
            Cruncher cruncher = (Cruncher)sender;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Environment.NewLine + e.Result.FileName + " - using " + e.Result.Service);

            if (e.Result.HasError)
            {
                Output(e.Result.ErrorMessage);
            }
            else if (e.Result.SizeBefore > e.Result.SizeAfter)
            {
                sb.AppendLine("Before: " + e.Result.SizeBefore + " bytes");
                sb.AppendLine("After: " + e.Result.SizeAfter + " bytes");
                sb.AppendLine("Savings: " + e.Result.PercentSaved + "%");

                optimized++;
                allBefore += e.Result.SizeBefore;
                allAfter += e.Result.SizeAfter;
            }
            else
            {
                sb.AppendLine("No optimization needed");
            }

            Output(sb.ToString());
            dte.StatusBar.Progress(true, e.Result.FileName, cruncher.Optimized, cruncher.Count);
        }

        private void Output(string text)
        {
            pane.OutputString(text);
        }
    }
}
