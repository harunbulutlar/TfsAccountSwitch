using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CredentialUtility;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.TeamFoundation.Common;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace NoComp.TfsAccSwitchVS10
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidTfsAccSwitchVS10PkgString)]
    [ProvideAutoLoad("{e13eedef-b531-4afe-9725-28a69fa4f896}")]
    public sealed class TfsAccSwitchVS10Package : Package, IOleCommandTarget
    {
        private const int UnknownGroup = (int)Constants.OLECMDERR_E_UNKNOWNGROUP;
        protected override void Initialize()
        {
            if (CredentialUtility.VSVersion.VS2010)
            {
                base.Initialize();
            }
        }
        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup != GuidList.guidTfsAccSwitchVS10CmdSet)
            {
                return UnknownGroup;
            }
            var uri = new Uri(TeamExplorer.GetProjectContext().DomainUri);
            var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(uri);
            var displayName = teamProjectCollection.AuthorizedIdentity.DisplayName;
            
            switch (nCmdId)
            {
                case PkgCmdIDList.cmdidChangeAccount:
                    {
                        if (CredentialWrapper.AskForCredentials(displayName, uri))
                        {
                            ((IVsShell4)GetService(typeof(SVsShell))).Restart(0);
                        }
                        break;
                    }

                case PkgCmdIDList.cmdidShowAccount:
                    {
                        MessageBox.Show(CredentialWrapper.GetLoggedInMessage(displayName));
                        break;
                    }
                default:
                    return UnknownGroup;
            }

            return 0;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == GuidList.guidTfsAccSwitchVS10CmdSet)
            {
                switch (prgCmds[0].cmdID)
                {
                    case PkgCmdIDList.cmdidChangeAccount:
                    case PkgCmdIDList.cmdidShowAccount:
                        prgCmds[0].cmdf = (int)OLECMDF.OLECMDF_SUPPORTED | (int)OLECMDF.OLECMDF_ENABLED;
                        return 0;
                }
            }
            return UnknownGroup;
        }
        public IVsTeamExplorer TeamExplorer
        {
            get
            {
                return GetService(typeof(IVsTeamExplorer)) as IVsTeamExplorer;
            }
        }

        public EnvDTE.DTE Dte
        {
            get
            {
                return GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            }
        }
    }
}
