using System.Collections.Generic;
using CUE4Parse_Conversion.UnrealFormat;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UnrealFormat;

public class FMorphTarget : ISerializable
{
    private FString MorphName;
    private List<FMorphData> MorphData = new();
    private int VertexCount;
    
    public FMorphTarget(string morphName, FMorphTargetLODModel morphLod)
    {
        MorphName = new FString(morphName);
        VertexCount = morphLod.Vertices.Length;
        foreach (var delta in morphLod.Vertices)
        {
            MorphData.Add(new FMorphData(delta.PositionDelta, delta.TangentZDelta, delta.SourceIdx));
        }
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        MorphName.Serialize(Ar);
        Ar.Write(VertexCount);
        MorphData.ForEach(x => x.Serialize(Ar));
    }
}

public class FMorphData : ISerializable
{
    public readonly FVector PositionDelta;
    public readonly FVector TangentZDelta;
    public readonly uint VertexIndex;

    public FMorphData(FVector positionDelta, FVector tangentZDelta, uint vertexIndex)
    {
        PositionDelta = positionDelta;
        PositionDelta.Y = -PositionDelta.Y;
        
        TangentZDelta = tangentZDelta;
        TangentZDelta.Y = -TangentZDelta.Y;
        
        VertexIndex = vertexIndex;
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        PositionDelta.Serialize(Ar);
        TangentZDelta.Serialize(Ar);
        Ar.Write(VertexIndex);
    }
}