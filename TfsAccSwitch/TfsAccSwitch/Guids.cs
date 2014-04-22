// Guids.cs
// MUST match guids.h
using System;

namespace NoComp.TfsAccSwitch
{
    static class GuidList
    {
        public const string guidTfsAccSwitchPkgString = "e98f8fa6-98b4-479d-89c7-8b99e6fa5b23";
        public const string guidTfsAccSwitchCmdSetString = "d269264a-f6f7-49c7-b684-e6a1b6440895";
        public static readonly Guid guidTfsAccSwitchCmdSet = new Guid(guidTfsAccSwitchCmdSetString);


        //public const string GuidTfsAccSwitchPkgString = "e98f8fa6-98b4-479d-89c7-8b99e6fa5b23";
        //public const string GuidTfsAccSwitchCmdSetString = "d269264a-f6f7-49c7-b684-e6a1b6440895";
        //public const string GuidTeamExplorerString = "23D49123-60AC-4D7E-939A-E01A4E176BEE";
        

        //public static readonly Guid GuidTfsAccSwitchCmdSet = new Guid(GuidTfsAccSwitchCmdSetString);
    };
}