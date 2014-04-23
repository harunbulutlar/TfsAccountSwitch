using System.ComponentModel;
using System.Windows.Forms;
using CredentialUtility.Properties;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
namespace CredentialUtility
{
    public class CredentialWrapper
    {
        [DllImport("ole32.dll")]
        public static extern void CoTaskMemFree(IntPtr ptr);
        [DllImport("credui")]
        private static extern CredUIReturnCodes CredUIPromptForCredentials(ref CREDUI_INFO creditUr, string targetName, IntPtr reserved1, int iError, StringBuilder userName, int maxUserName, StringBuilder password, int maxPassword, [MarshalAs(UnmanagedType.Bool)] ref bool pfSave, CREDUI_FLAGS flags);
        [DllImport("credui.dll", CharSet=CharSet.Auto)]
        private static extern int CredUIPromptForWindowsCredentials(ref CREDUI_INFO notUsedHere, int authError, ref uint authPackage, IntPtr inAuthBuffer, uint inAuthBufferSize, out IntPtr refOutAuthBuffer, out uint refOutAuthBufferSize, ref bool fSave, int flags);
        [DllImport("credui.dll", CharSet=CharSet.Auto)]
        private static extern bool CredUnPackAuthenticationBuffer(int dwFlags, IntPtr pAuthBuffer, uint cbAuthBuffer, StringBuilder pszUserName, ref int pcchMaxUserName, StringBuilder pszDomainName, ref int pcchMaxDomainame, StringBuilder pszPassword, ref int pcchMaxPassword);
        [DllImport("Advapi32.dll", EntryPoint="CredWriteW", CharSet=CharSet.Unicode, SetLastError=true)]
        public static extern bool CredWrite([In] ref Credential userCredential, [In] uint flags);
        public  static string GetLoggedInMessage(string displayName)
        {
           return "You are currently logged in with\n\n" + displayName;
        }

        public static  bool AskForCredentials(string displayName, Uri projUri)
        {
            var text = GetLoggedInMessage(displayName);
            if (MessageBox.Show(text + Resources.ChangeMessage, Resources.Attention, MessageBoxButtons.YesNo) !=
                DialogResult.Yes)
            {
                return false;
            }
            string user;
            string password;
            GetCredentials(projUri.Host, out user, out password);
            if ((user == null) || (password == null))
            {
                return false;
            }
            var credList = GetCredentialList(projUri.Host);
            if(credList == null || credList.Count == 0)
            {
                credList = new List<CredentialBasic>() { new CredentialBasic() { CredType = CRED_TYPE.CRED_TYPE_DOMAIN_PASSWORD, Uri = projUri.Host } };
            }
            foreach (var credBasic in credList)
            {
                var userCredential = new Credential
                {
                    targetName = credBasic.Uri,
                    type = (uint)credBasic.CredType,
                    userName = user,
                    attributeCount = 0,
                    persist = 3
                };
                var bytes = Encoding.Unicode.GetBytes(password);
                userCredential.credentialBlobSize = (uint)bytes.Length;
                userCredential.credentialBlob = Marshal.StringToCoTaskMemUni(password);
                if (!CredWrite(ref userCredential, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            if (MessageBox.Show(Resources.Restart, Resources.Attention, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                return true;
            }
            return false;
        }
        public static void GetCredentials(string serverName, out string user, out string password)
        {
            user = null;
            password = null;
            if (IsWinVistaOrHigher)
            {
                GetCredentialsVistaAndUp(serverName, out user, out password);
            }
            else if (IsWinXpOrHigher)
            {
                GetCredentialsXp(serverName, out user, out password);
            }
        }

        private static void GetCredentialsVistaAndUp(string serverName, out string user, out string password)
        {
            var creduiInfo = new CREDUI_INFO();
            uint num2;
            creduiInfo = new CREDUI_INFO {
                pszCaptionText = "Please enter the credentails for " + serverName,
                pszMessageText = "Enter Credentials",
                cbSize = Marshal.SizeOf(creduiInfo)
            };
            uint authPackage = 0;
            IntPtr refOutAuthBuffer;
            var fSave = false;
            var num3 = CredUIPromptForWindowsCredentials(ref creduiInfo, 0, ref authPackage, IntPtr.Zero, 0, out refOutAuthBuffer, out num2, ref fSave, 1);
            var pszUserName = new StringBuilder(100);
            var pszPassword = new StringBuilder(100);
            var pszDomainName = new StringBuilder(100);
            int pcchMaxUserName = 100;
            int pcchMaxDomainame = 100;
            int pcchMaxPassword = 100;
            user = null;
            password = null;
            if ((num3 == 0) && CredUnPackAuthenticationBuffer(0, refOutAuthBuffer, num2, pszUserName, ref pcchMaxUserName, pszDomainName, ref pcchMaxDomainame, pszPassword, ref pcchMaxPassword))
            {
                CoTaskMemFree(refOutAuthBuffer);
                user = pszUserName.ToString();
                password = pszPassword.ToString();
            }
        }

        private static void GetCredentialsXp(string serverName, out string user, out string password)
        {
            var creduiInfo = new CREDUI_INFO();
            var builder = new StringBuilder();
            var userName = new StringBuilder();
            creduiInfo = new CREDUI_INFO {
                cbSize = Marshal.SizeOf(creduiInfo)
            };
            var pfSave = false;
            const CREDUI_FLAGS flags = CREDUI_FLAGS.GENERIC_CREDENTIALS | CREDUI_FLAGS.ALWAYS_SHOW_UI;
            CredUIPromptForCredentials(ref creduiInfo, serverName, IntPtr.Zero, 0, userName, 100, builder, 100, ref pfSave, flags);
            user = userName.ToString();
            password = builder.ToString();
        }

        private static bool IsWinVistaOrHigher
        {
            get
            {
                OperatingSystem oSVersion = Environment.OSVersion;
                return ((oSVersion.Platform == PlatformID.Win32NT) && (oSVersion.Version.Major >= 6));
            }
        }

        private static bool IsWinXpOrHigher
        {
            get
            {
                OperatingSystem oSVersion = Environment.OSVersion;
                return ((oSVersion.Platform == PlatformID.Win32NT) && ((oSVersion.Version.Major > 5) || ((oSVersion.Version.Major == 5) && (oSVersion.Version.Minor >= 1))));
            }
        }
        private static List<CredentialBasic> GetCredentialList(string filter=null)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "cmdkey.EXE";
            start.Arguments = @"/list";
            start.RedirectStandardOutput = true;
            start.UseShellExecute = false;
            List<CredentialBasic> credList = new List<CredentialBasic>();

            try
            {
                string result;

                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        result = reader.ReadToEnd();
                    }
                }
                var matches = Regex.Matches(result, @"Target:\s*(LegacyGeneric|Domain)\s*:\s*(.*)", RegexOptions.Multiline);

                foreach (Match match in matches)
                {
                    var cred = new CredentialBasic();
                    if (match.Groups != null && match.Groups.Count >= 2)
                    {
                        string credType = match.Groups[1].ToString().Trim();
                        switch (credType)
                        {
                            case "LegacyGeneric":
                                cred.CredType = CRED_TYPE.CRED_TYPE_GENERIC;
                                break;
                            case "Domain":
                                cred.CredType = CRED_TYPE.CRED_TYPE_DOMAIN_PASSWORD;
                                break;
                            default:
                                continue;
                        }
                        Match uriMatch = Regex.Match(match.Groups[2].ToString(), @"target=(.*)");
                        if (uriMatch.Groups != null && uriMatch.Groups.Count >= 2)
                        {
                            cred.Uri = uriMatch.Groups[1].ToString().Trim();
                            credList.Add(cred);
                        }

                    }

                }
            }
            catch
            {
                //Somehow this failed
                return null;
            }
          

            if (filter != null)
            {
                var filteredList = from cred in credList 
                                   where cred.Uri.Contains(filter) select cred;
                return filteredList.ToList();
            }
            return credList;
        }
        public enum CRED_PERSIST : uint
        {
            CRED_PERSIST_ENTERPRISE = 3,
            CRED_PERSIST_LOCAL_MACHINE = 2,
            CRED_PERSIST_SESSION = 1
        }

        public enum CRED_TYPE : uint
        {
            CRED_TYPE_DOMAIN_CERTIFICATE = 3,
            CRED_TYPE_DOMAIN_PASSWORD = 2,
            CRED_TYPE_DOMAIN_VISIBLE_PASSWORD = 4,
            CRED_TYPE_GENERIC = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public struct Credential
        {
            public uint flags;
            public uint type;
            public string targetName;
            public string comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME lastWritten;
            public uint credentialBlobSize;
            public IntPtr credentialBlob;
            public uint persist;
            public uint attributeCount;
            public IntPtr credAttribute;
            public string targetAlias;
            public string userName;
        }

        [Flags]
        private enum CREDUI_FLAGS
        {
            ALWAYS_SHOW_UI = 0x80,
            COMPLETE_USERNAME = 0x800,
            DO_NOT_PERSIST = 2,
            EXCLUDE_CERTIFICATES = 8,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            INCORRECT_PASSWORD = 1,
            KEEP_USERNAME = 0x100000,
            PASSWORD_ONLY_OK = 0x200,
            PERSIST = 0x1000,
            REQUEST_ADMINISTRATOR = 4,
            REQUIRE_CERTIFICATE = 0x10,
            REQUIRE_SMARTCARD = 0x100,
            SERVER_CREDENTIAL = 0x4000,
            SHOW_SAVE_CHECK_BOX = 0x40,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            VALIDATE_USERNAME = 0x400
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        public enum CredUIReturnCodes
        {
            ERROR_CANCELLED = 0x4c7,
            ERROR_INSUFFICIENT_BUFFER = 0x7a,
            ERROR_INVALID_ACCOUNT_NAME = 0x523,
            ERROR_INVALID_FLAGS = 0x3ec,
            ERROR_INVALID_PARAMETER = 0x57,
            ERROR_NO_SUCH_LOGON_SESSION = 0x520,
            ERROR_NOT_FOUND = 0x490,
            NO_ERROR = 0
        }
        protected class CredentialBasic
        {
            public CRED_TYPE CredType { get; set; }
            public string Uri { get; set; }
        }
    }
}

