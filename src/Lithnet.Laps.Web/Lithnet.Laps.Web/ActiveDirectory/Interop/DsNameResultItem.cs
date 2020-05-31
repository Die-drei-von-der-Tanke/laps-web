﻿using System.Runtime.InteropServices;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DsNameResultItem
    {
        public DsNameError Status;

        public string Domain;

        public string Name;
    }
}