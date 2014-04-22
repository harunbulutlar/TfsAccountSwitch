using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.TeamFoundation.Common;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace NoComp.TfsAccSwitch
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidTfsAccSwitchPkgString)]
    [ProvideAutoLoad("{e13eedef-b531-4afe-9725-28a69fa4f896}")]
    public sealed class TfsAccSwitchPackage : Package, IOleCommandTarget
    {
        private const int UnknownGroup = (int)Constants.OLECMDERR_E_UNKNOWNGROUP;

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup != GuidList.guidTfsAccSwitchCmdSet)
            {
                return UnknownGroup;
            }

            var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(TeamExplorer.GetProjectContext().DomainUri));
            var text = "You are currently logged in with\n\n" + teamProjectCollection.AuthorizedIdentity.DisplayName;
            
            switch (nCmdId)
            {
                case PkgCmdIDList.cmdidChangeAccount:
                    {
                        if (MessageBox.Show(text + Resources.ChangeMessage, Resources.Attention, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            AskForCredentials(new Uri(TeamExplorer.GetProjectContext().DomainUri));
                        }
                        break;
                    }

                case PkgCmdIDList.cmdidShowAccount:
                    {
                        MessageBox.Show(text);
                        break;
                    }
                default:
                    return UnknownGroup;
            }

            return 0;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == GuidList.guidTfsAccSwitchCmdSet)
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

        private void AskForCredentials(Uri projUri)
        {
            string user;
            string password;
            CredentialWrapper.GetCredentials(projUri.Host, out user, out password);
            if ((user == null) || (password == null))
            {
                return;
            }
            var userCredential = new CredentialWrapper.Credential
                {
                    targetName = projUri.Host,
                    type = 2,
                    userName = user,
                    attributeCount = 0,
                    persist = 3
                };
            var bytes = Encoding.Unicode.GetBytes(password);
            userCredential.credentialBlobSize = (uint)bytes.Length;
            userCredential.credentialBlob = Marshal.StringToCoTaskMemUni(password);
            if (!CredentialWrapper.CredWrite(ref userCredential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            if (MessageBox.Show(Resources.Restart, Resources.Attention, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ((IVsShell4)GetService(typeof(SVsShell))).Restart(0);
            }
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
