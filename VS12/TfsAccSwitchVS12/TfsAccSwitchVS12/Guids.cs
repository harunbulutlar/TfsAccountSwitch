// Guids.cs
// MUST match guids.h
using System;

namespace NoComp.TfsAccSwitchVS12
{
    static class GuidList
    {
        public const string guidTfsAccSwitchVS12PkgString = "0fadd53b-5e95-4b75-97c1-baa743a91407";
        public const string guidTfsAccSwitchVS12CmdSetString = "8c7f7720-0d86-4763-9f7c-a24d5bb868a1";

        public static readonly Guid guidTfsAccSwitchVS12CmdSet = new Guid(guidTfsAccSwitchVS12CmdSetString);
    };
}