using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.Utils; 

public static class CUE4ParseNatives 
{
    [DllImport("CUE4Parse-Natives", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool IsFeatureAvailable([MarshalAs(UnmanagedType.LPStr)] string featureName); 
}