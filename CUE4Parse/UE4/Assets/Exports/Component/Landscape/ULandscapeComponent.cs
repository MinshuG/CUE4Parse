using System;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeComponent: UPrimitiveComponent
{
    public int SectionBaseX;
    public int SectionBaseY;
    public int ComponentSizeQuads;
    public int SubsectionSizeQuads;
    public int NumSubsections;
    public FVector4 HeightmapScaleBias;
    public int WeightmapScaleBias;
    public float WeightmapSubsectionOffset;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SectionBaseX = GetOrDefault(nameof(SectionBaseX), 0);
        SectionBaseY = GetOrDefault(nameof(SectionBaseY), 0);
        ComponentSizeQuads = GetOrDefault(nameof(ComponentSizeQuads), 0);
        SubsectionSizeQuads = GetOrDefault(nameof(SubsectionSizeQuads), 0);
        NumSubsections = GetOrDefault(nameof(NumSubsections), 1);
        HeightmapScaleBias = GetOrDefault(nameof(HeightmapScaleBias), new FVector4(0, 0, 0, 0));
        WeightmapScaleBias = GetOrDefault(nameof(WeightmapScaleBias), 0);
        WeightmapSubsectionOffset = GetOrDefault(nameof(WeightmapSubsectionOffset), 0f);
        
        
        // throw new NotImplementedException();
    }

    public void GetComponentExtent(ref int MinX, ref int MinY, ref int MaxX, ref int MaxY)
    {
        MinX = Math.Min(SectionBaseX, MinX);
        MinY = Math.Min(SectionBaseY, MinY);
        MaxX = Math.Max(SectionBaseX + ComponentSizeQuads, MaxX);
        MaxY = Math.Max(SectionBaseY + ComponentSizeQuads, MaxY);
    }

    public FIntRect GetComponentExtent()
    {
        int MinX = int.MaxValue, MinY = int.MaxValue;
        int MaxX = int.MinValue, MaxY = int.MinValue;
        GetComponentExtent(ref MinX, ref MinY, ref MaxX, ref MaxY);
        return new FIntRect(new FIntPoint(MinX, MinY), new FIntPoint(MaxX, MaxY));
    }
    
    public UTexture2D? GetHeightmap(bool bWorkOnEditingLayer) => GetOrDefault<UTexture2D>("HeightmapTexture", null);
}