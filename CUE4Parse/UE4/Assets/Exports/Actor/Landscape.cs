using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class ALandscapeProxy : Actor
{
    public int ComponentSizeQuads;
    public int SubsectionSizeQuads;
    public int NumSubsections;
    public FPackageIndex[] LandscapeComponents;
    public int LandscapeSectionOffset;
    public FPackageIndex LandscapeMaterial;
    public FPackageIndex SplineComponent;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ComponentSizeQuads = GetOrDefault<int>(nameof(ComponentSizeQuads));
        SubsectionSizeQuads = GetOrDefault<int>(nameof(SubsectionSizeQuads));
        NumSubsections = GetOrDefault<int>(nameof(NumSubsections));
        LandscapeComponents = GetOrDefault<FPackageIndex[]>(nameof(LandscapeComponents));
        LandscapeSectionOffset = GetOrDefault<int>(nameof(LandscapeSectionOffset));
        LandscapeMaterial = GetOrDefault<FPackageIndex>(nameof(LandscapeMaterial));
        SplineComponent = GetOrDefault<FPackageIndex>(nameof(SplineComponent));
    }
}

public class ALandscape: ALandscapeProxy { }
public class ALandscapeStreamingProxy: ALandscapeProxy { }
