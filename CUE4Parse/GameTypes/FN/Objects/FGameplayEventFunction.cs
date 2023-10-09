using System;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.GameTypes.FN.Objects;


// public readonly struct FMemberReference {
     // public readonly FPackageIndex MemberParent;
     // public readonly string MemberScope;
     // public readonly FName MemberName;
     // public MemberGuid
// } 

public class FMemberReference //: FStructFallback
{
    public readonly FPackageIndex MemberParent;
    public readonly string MemberScope;
    public readonly FName MemberName;
    public readonly FGuid MemberGuid;
    public readonly bool bSelfContext;
    public readonly bool bWasDeprecated;

    public FMemberReference(FAssetArchive Ar)//: base(Ar, "MemberReference") { }
    {
        MemberParent = new FPackageIndex(Ar);
        MemberScope = Ar.ReadFString();
        MemberName = Ar.ReadFName();
        MemberGuid = Ar.Read<FGuid>();
        bSelfContext = Ar.ReadBoolean();
        bWasDeprecated = Ar.ReadBoolean();
    }
} 

public readonly struct FGameplayEventSubscription 
{
    private readonly FPackageIndex Object;
    public readonly FMemberReference EventDescriptor;
    public readonly FGameplayEventListenerHandle EventHandle;

    public FGameplayEventSubscription(FAssetArchive Ar) {
        // Object = new FPackageIndex(Ar);
        // if (Object.ResolvedObject == null)
        //     throw new Exception();
        // Console.WriteLine(Object.ResolvedObject.Name);
        EventDescriptor = new FMemberReference(Ar); // 8
        EventHandle = Ar.Read<FGameplayEventListenerHandle>();
    }
}

public readonly struct FGameplayEventListenerHandle 
{
    public readonly int Handle;
}

// DataDrivenGameplayEventRouter.GameplayEventFunction
public class FGameplayEventFunction: IUStruct
{
    public FGameplayEventSubscription[] EventSubscriptions;

    public FGameplayEventFunction(FAssetArchive Ar) {
        // Ar.DumpBytesToHex(128, 128);
        bool something = Ar.ReadBoolean(); // basically true when no events are registered and EventSubs is empty (i think)
        if (something) {
            Ar.Position += 0x54;
            // var (x,y) = Ar.OffsetScanTillError(() => new FStructFallback(Ar, structType: "GameplayEventFunction"), 100, 1);
            // System.Diagnostics.Debugger.Break();
        }
        else {
            var f = new FStructFallback(Ar, structType: "GameplayEventFunction");
        }
    }
}