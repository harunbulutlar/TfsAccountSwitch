using System;
using System.Runtime.InteropServices;
using CredentialUtility;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace NoComp.TfsAccSwitchVS12
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
    [Guid(GuidList.guidTfsAccSwitchVS12PkgString)]
    [ProvideAutoLoad("{e13eedef-b531-4afe-9725-28a69fa4f896}")]
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\12.0")]
    public sealed class TfsAccSwitchVS12Package : Package, IOleCommandTarget
    {
        private const int UnknownGroup = (int)Constants.OLECMDERR_E_UNKNOWNGROUP;
        protected override void Initialize()
        {
            if (CredentialUtility.VSVersion.VS2012)
            {
                base.Initialize();
            }
        }
        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup != GuidList.guidTfsAccSwitchVS12CmdSet)
            {
                return UnknownGroup;
            }
            var uri = new Uri(TeamExplorer.CurrentContext.DomainUri());
            var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(uri);
            var displayName =   teamProjectCollection.AuthorizedIdentity.DisplayName;

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
                default:
                    return UnknownGroup;
            }

            return 0;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (!CredentialUtility.VSVersion.VS2012)
            {
                prgCmds[0].cmdf = (int)OLECMDF.OLECMDF_INVISIBLE;
                return UnknownGroup;
            }
            if (pguidCmdGroup == GuidList.guidTfsAccSwitchVS12CmdSet)
            {
                switch (prgCmds[0].cmdID)
                {
                    case PkgCmdIDList.cmdidChangeAccount:
                        prgCmds[0].cmdf = (int)OLECMDF.OLECMDF_SUPPORTED | (int)OLECMDF.OLECMDF_ENABLED;
                        return 0;
                }
            }
            return UnknownGroup;
        }

        public ITeamFoundationContextManager TeamExplorer
        {
            get
            {
                return GetService(typeof(ITeamFoundationContextManager)) as ITeamFoundationContextManager;
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
