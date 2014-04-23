// Guids.cs
// MUST match guids.h
using System;

namespace NoComp.TfsAccSwitchVS13
{
    static class GuidList
    {
        public const string guidTfsAccSwitchVS13PkgString = "4330c48a-6bd2-4852-bb9d-59ee4408eb8b";
        public const string guidTfsAccSwitchVS13CmdSetString = "93898ac8-a802-4961-b02b-4eadf10af299";

        public static readonly Guid guidTfsAccSwitchVS13CmdSet = new Guid(guidTfsAccSwitchVS13CmdSetString);
    };
}