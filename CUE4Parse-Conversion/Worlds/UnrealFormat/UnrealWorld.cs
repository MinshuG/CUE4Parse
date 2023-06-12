using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.UnrealFormat;
using CUE4Parse_Conversion.UnrealFormat;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Worlds.UnrealFormat;

public class UnrealWorld : ExportableUnrealAssetBase
{
    private readonly Dictionary<int, UnrealModel> MeshMap = new();
    private readonly List<FActor> Actors = new();

    public UnrealWorld(string name, ExporterOptions options)
    {
        Header = new FUnrealHeader("UWORLD", 1, name, options.CompressionFormat);
    }
    
    public UnrealWorld(UWorld world, string name, ExporterOptions options) : this(name, options)
    {
        ProcessWorld(world, FVector.ZeroVector, FRotator.ZeroRotator);
        
        using (var meshChunk = new FDataChunk("MESHES", MeshMap.Count))
        {
            foreach (var (hash, model) in MeshMap)
            {
                meshChunk.Write(hash);
                meshChunk.Write(model.Length);
                model.Save(meshChunk);
            }
            
            meshChunk.Serialize(Ar);
        }
        
        
        using (var actorChunk = new FDataChunk("ACTORS", Actors.Count))
        {
            foreach (var actor in Actors)
            {
                actor.Serialize(actorChunk);
            }
            
            actorChunk.Serialize(Ar);
        }
    }

    private void ProcessWorld(UWorld world, FVector positionOffset, FRotator rotationOffset)
    {
        var level = world.PersistentLevel.Load<ULevel>();
        if (level is null) return;

        foreach (var actorUnloaded in level.Actors)
        {
            var actor = actorUnloaded.Load();
            if (actor is null) continue;
            if (actor.ExportType is "LODActor") continue;
            if (actor.Name.StartsWith("LF_")) continue;
            
            ProcessMesh(actor, positionOffset, rotationOffset);
            ProcessAdditionalWords(actor, positionOffset, rotationOffset);
        }
    }
    
    private void ProcessMesh(UObject actor, FVector positionOffset, FRotator rotationOffset)
    {
        if (!actor.TryGetValue(out UStaticMeshComponent staticMeshComponent, "StaticMeshComponent", "StaticMesh", "Mesh", "LightMesh")) return;
        if (!staticMeshComponent.GetStaticMesh().TryLoad(out UStaticMesh staticMesh)) return;

        var hash = staticMesh.GetPathName().GetHashCode();
        if (!MeshMap.ContainsKey(hash) && staticMesh.TryConvert(out var convertedMesh))
        {
            MeshMap[staticMesh.GetPathName().GetHashCode()] = new UnrealModel(convertedMesh.LODs.First(), staticMesh.Name, default);
        }

        var position = staticMeshComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector) + positionOffset;
        position.Y = -position.Y;
        
        var rotation = staticMeshComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator) + rotationOffset;
        var rotationQuat = rotation.Quaternion();
        rotationQuat.Y = -rotationQuat.Y;
        rotationQuat.Z = -rotationQuat.Z;
        
        var scale = staticMeshComponent.GetOrDefault("RelativeScale3D", FVector.OneVector);

        Actors.Add(new FActor(hash, actor.Name, position, rotationQuat.Rotator(), scale));
    }
    
    private void ProcessAdditionalWords(UObject actor, FVector positionOffset, FRotator rotationOffset)
    {
        if (!actor.TryGetValue(out FSoftObjectPath[] additionalWorlds, "AdditionalWorlds")) return;
        if (!actor.TryGetValue(out FPackageIndex staticMeshComponentUnloaded, "StaticMeshComponent", "Mesh")) return;
        if (!staticMeshComponentUnloaded.TryLoad(out var staticMeshComponent)) return;
        
        var position = staticMeshComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector) + positionOffset;
        position.Y = -position.Y;
        
        var rotation = staticMeshComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator) + rotationOffset;
        var rotationQuat = rotation.Quaternion();
        rotationQuat.Y = -rotationQuat.Y;
        rotationQuat.Z = -rotationQuat.Z;
        
        foreach (var additionalWorldPath in additionalWorlds)
        {
            var additionalWorld = additionalWorldPath.Load<UWorld>();
            ProcessWorld(additionalWorld, position, rotationQuat.Rotator());
        }
    }
}

