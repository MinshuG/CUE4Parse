using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.UnrealFormat;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using SixLabors.ImageSharp.Formats.Png;
using SkiaSharp;
// ReSharper disable InconsistentNaming


namespace CUE4Parse_Extensions;

public class LandscapeExporter : PSKExporter
{
    private bool _isInfoSet;
    private ULandscapeComponent[] _landscapeComponents;
    private int _componentSize;
    private FPackageIndex _landscapeMaterial;
    private List<MaterialExporter2> Materials = new List<MaterialExporter2>();

    internal Dictionary<string, SKBitmap> WeightMaps { get; set; } = new();
    internal Mesh[]? ProcessedFiles;

    public LandscapeExporter(ALandscapeProxy landscape, ULandscapeComponent[]? components, ExporterOptions options) : base(landscape, options)
    {
        _isInfoSet = false;
        _landscapeComponents = Array.Empty<ULandscapeComponent>();
        _landscapeMaterial = landscape.LandscapeMaterial;

        if (landscape is { } landscapeProxy)
        {
            LoadLandscapeInfo(landscapeProxy);
            _landscapeComponents = components ?? LoadComponents(landscapeProxy);
        }
        else
        {
            throw new Exception($"Unknown landscape type: {landscape.ExportType}");
        }
        SetMeshData(DoThings3_Mesh());
    }

    public LandscapeExporter(UWorld world, ExporterOptions options): base(world, options)
    {
        _isInfoSet = false;;
        _landscapeComponents = Array.Empty<ULandscapeComponent>();
        InitComponentsFromWorld(world);
        SetMeshData(DoThings3_Mesh()); 
    }

    private void SetMeshData(CStaticMeshLod lod)
    {
        using var Ar = new FArchiveWriter();
        var materialExports = Options.ExportMaterials ? new List<MaterialExporter2>() : null;
        string ext;
        switch (Options.MeshFormat)
        {
            case EMeshFormat.ActorX:
                ext = "pskx";
                ExportStaticMeshLods(lod, Ar, materialExports, Array.Empty<FPackageIndex>());
                break;
            case EMeshFormat.Gltf2:
                ext = "glb";
                new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                break;
            case EMeshFormat.OBJ:
                ext = "obj";
                new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                break;
            case EMeshFormat.Unreal:
                ext = "umodel";
                new UnrealModel(lod, ExportName, Options).Save(Ar);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
        }

        var final = new List<Mesh>();

        var path = GetExportSavePath();
        final.Add(new Mesh($"{path}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));

        foreach (var kv in WeightMaps) {
            string weightMapPath = $"{path}/{kv.Key}.png";
            var weightMap = kv.Value;
            var ImageData = weightMap.Encode(SKEncodedImageFormat.Png, 100).ToArray();
            final.Add(new Mesh(weightMapPath, ImageData, new List<MaterialExporter2>()));
        }
        ProcessedFiles = final.ToArray();
        WeightMaps.Clear();
        final.Clear();
    }

    private void LoadLandscapeInfo(ALandscapeProxy proxy)
    {
        if (_isInfoSet)
            return;
        // var p = proxy.GetPathName(); // just in case it was set by UWorld constructor overload
        Trace.Assert(proxy.Owner != null, "proxy.Owner != null");
        PackagePath = proxy.Owner!.Name;
        ExportName = proxy.Name;
        _componentSize = proxy.ComponentSizeQuads;
        _isInfoSet = true;
    }

    private void InitComponentsFromWorld(UWorld world) {
#if !DEBUG
         throw new InvalidOperationException();
#endif
        var comps = new List<ULandscapeComponent>();
        var actors = world.PersistentLevel.Load<ULevel>()!.Actors;
        foreach (var t in actors)
        {
            var actor = t.Load();
            if (actor is not ALandscapeProxy proxy)
                continue;
            comps.AddRange(LoadComponents(proxy));
            LoadLandscapeInfo(proxy);
        }

        foreach (var level in world.StreamingLevels)
        {
            var uWorld = level.Load()?.Get<UWorld>("WorldAsset");
            var persLevel = uWorld?.PersistentLevel.Load<ULevel>();
            for (var j = 0; j < persLevel!.Actors.Length; j++)
            {
                var actor = persLevel.Actors[j].Load();
                if (actor is not ALandscapeProxy proxy)
                    continue;
                comps.AddRange(LoadComponents(proxy));
                LoadLandscapeInfo(proxy);
            }
        }
        _landscapeComponents = comps.ToArray();
    }

    private ULandscapeComponent[] LoadComponents(ALandscapeProxy? loadedActor)
    {
        // var resComponents = new List<ULandscapeComponent>();
        // if (actor is {IsNull: true})
        //     return resComponents.ToArray();
        // var loadedActor = actor.Load<UObject>();
        // if (loadedActor == null) return resComponents.ToArray(); // && LoadedActor?.ExportType == "LandscapeStreamingProxy" || LoadedActor?.ExportType == "LevelStreamingAlwaysLoaded")
        if (loadedActor != null)
        {
            var comps = loadedActor.LandscapeComponents;
            var resComponents = new ULandscapeComponent[comps.Length];
            for (var i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                resComponents[i] = comp.Load<ULandscapeComponent>()!;
            }

            return resComponents.ToArray();
        }
        return Array.Empty<ULandscapeComponent>();
    }

    private CStaticMeshLod DoThings3_Mesh()
    {
        int MinX = int.MaxValue, MinY = int.MaxValue;
        int MaxX = int.MinValue, MaxY = int.MinValue;

        foreach (var comp in _landscapeComponents)
        {
            if (_componentSize == -1)
                _componentSize = comp.ComponentSizeQuads;
            else
            {
                Debug.Assert(_componentSize == comp.ComponentSizeQuads);
            }
            comp.GetComponentExtent(ref MinX, ref MinY, ref MaxX, ref MaxY);
        }

        // Create and fill in the vertex position data source.
        int ComponentSizeQuads = ((_componentSize + 1) >> 0/*Landscape->ExportLOD*/) - 1;
        float ScaleFactor = (float)ComponentSizeQuads / (float)_componentSize;
        int NumComponents = _landscapeComponents.Length;
        int VertexCountPerComponent = (ComponentSizeQuads + 1) * (ComponentSizeQuads + 1);
        int VertexCount = NumComponents * VertexCountPerComponent;
        int TriangleCount = NumComponents * (ComponentSizeQuads*ComponentSizeQuads) * 2;

        FVector2D UVScale = new FVector2D(1.0f, 1.0f) / new FVector2D((MaxX - MinX) + 1, (MaxY - MinY) + 1);

        // For image export
        int Width = MaxX - MinX + 1;
        int Height = MaxY - MinY + 1;

        // var name = "LandscapeMesh";
        // var meshNode = new Node(name);
        // var geometry = new Mesh(name);
        // geometry.Layers.Add(new List<LayerElement>());
        // meshNode.Children.Add(geometry);

        var landscapeLod = new CStaticMeshLod();
        landscapeLod.NumTexCoords = 2; // TextureUV and weightmapUV
        landscapeLod.AllocateVerts(VertexCount);
        landscapeLod.AllocateVertexColorBuffer();
        var ExtraVertexColorMap = new ConcurrentDictionary<string, CVertexColor>();

        // locate array for storing image data
        var weightMaps = new ConcurrentDictionary<string, SKBitmap>();
        // var heightMapData = new L16[Height*Width];

        // verts = ControlPoints (in fbx)
        // var meshVert = new XYZ[VertexCount];
        // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4549
        // https://github.com/FabianFG/CUE4Parse/blob/fbx/CUE4Parse-Conversion/Meshes/MeshIOApi.cs#L70C9-L70C46

        // _landscapeComponents = _landscapeComponents.OrderBy(x => int.Parse(x.Name.SubstringAfterLast('_'))).ToArray();
        var tasks = new Task[_landscapeComponents.Length];
        for (int i = 0, selectedComponentIndex=0; i < _landscapeComponents.Length; i++)
        {
            var comp = _landscapeComponents[i];
            var CDI = new FLandscapeComponentDataInterface(comp, 0, false);

            int baseVertIndex = selectedComponentIndex++ * VertexCountPerComponent;

            var weightMapAllocs = comp.GetWeightmapLayerAllocations(false);
  
            var task = Task.Run(() => {
                for (int vertIndex = 0; vertIndex < VertexCountPerComponent; vertIndex++) {
                    CDI.VertexIndexToXY(vertIndex, out var vertX, out var vertY);
                    
                    var vertCoord = CDI.GetLocalVertex(vertX, vertY);
                    var position = vertCoord + comp.GetRelativeLocation();

                    CDI.GetLocalTangentVectors(vertIndex, out var tangentX, out var biNormal, out var normal);

                    normal /= comp.GetComponentTransform().Scale3D;
                    normal.Normalize();
                    tangentX /= comp.GetComponentTransform().Scale3D;
                    tangentX.Normalize();
                    biNormal /= comp.GetComponentTransform().Scale3D;
                    biNormal.Normalize();

                    var textureUv = new FVector2D(vertX * ScaleFactor + comp.SectionBaseX, vertY * ScaleFactor + comp.SectionBaseY);
                    var textureUv2 = new FVector2D(vertX * ScaleFactor, vertY * ScaleFactor);

                    // XYZ FbxPosition = new XYZ(Position.X, Position.Z, Position.Y); // Position.Z is height
                    // meshVert[BaseVertIndex + VertIndex] = FbxPosition;
                    var weightmapUv = (textureUv - new FVector2D(MinX, MinY)) * UVScale;

                    // rescale float to ushort
                    // heightMapData[(int)textureUv.Y * Width + (int)textureUv.X] = new L16(CDI.GetHeight(vertX, vertY));

                    foreach (var allocationInfo in weightMapAllocs) {
                        var layerName = allocationInfo.GetLayerName();
                        // if (allocationInfo.LayerInfo.Name != "BiomeA_LayerInfo") {
                        //     continue;
                        // }
                        var weight = CDI.GetLayerWeight(vertX, vertY, allocationInfo.LayerInfo);

                        if (!weightMaps.ContainsKey(layerName)) {
                            weightMaps.TryAdd(layerName,
                                new SKBitmap(Width, Height, SKColorType.Gray8, SKAlphaType.Unpremul));
                        }

                        // ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd
                        if (!ExtraVertexColorMap.ContainsKey(layerName)) {
                            // dont use try add because FColor[VertexCount] it will be allocated
                            var shortName = layerName.SubstringBefore("_LayerInfo");
                            shortName = shortName.Substring(0, Math.Min(20 - 6, shortName.Length));
                            ExtraVertexColorMap.TryAdd(layerName, new CVertexColor(shortName, new FColor[VertexCount]));
                        }

                        var pixelX = textureUv2.X; // weightmapUv.X * weightMaps[LayerName].Width;
                        var pixelY = textureUv2.Y; // weightmapUv.Y * weightMaps[LayerName].Height;

                        weightMaps[layerName].SetPixel((int)pixelX, (int)pixelY, new SKColor(weight, weight, weight, 255)); // slow 
                        ExtraVertexColorMap[layerName].Colors[baseVertIndex + vertIndex] = new FColor(weight, weight, weight, weight);

                        // var infoObject = allocationInfo.LayerInfo.Load<ULandscapeLayerInfoObject>();
                        // var cl = infoObject.LayerUsageDebugColor.ToFColor(true);
                        // weightMapsData[allocationInfo.LayerInfo.Name].SetPixel((int)pixel_x, (int)pixel_y, new SKColor(cl.R, cl.G, cl.B, weight));
                    }

                    var vert = new CMeshVertex(position, normal, tangentX, (FMeshUVFloat)textureUv);
                    landscapeLod.Verts[baseVertIndex + vertIndex] = vert;
                    // landscapeLod.VertexColors![BaseVertIndex + VertIndex] = weightMapColor;
                    landscapeLod.ExtraUV.Value[0][baseVertIndex + vertIndex] = (FMeshUVFloat)weightmapUv;
                }
            });
            tasks[i] = task;
        }

        Task.WaitAll(tasks);

        // var image = Image.LoadPixelData<L16>(heightMapData, Width, Height);
        // image.Save(File.OpenWrite("heightmap.png"), new PngEncoder());
        WeightMaps = weightMaps.ToDictionary(x => x.Key, x => x.Value);
        landscapeLod.ExtraVertexColors = ExtraVertexColorMap.Values.ToArray();
        ExtraVertexColorMap.Clear();
        var mat = _landscapeMaterial.Load<UMaterialInterface>();
        landscapeLod.Sections = new TaskLazy<CMeshSection[]>(new[]
        {
            new CMeshSection(0, 0, TriangleCount, mat?.Name ?? "DefaultMaterial", _landscapeMaterial.ResolvedObject)
        });

        // geometry.Vertices.AddRange(meshVert);
        // var matLayer = new LayerElementMaterial(geometry)
        // {
        //     Name = "DefaultMaterial",
        //     MappingInformationType = MappingMode.ByPolygon, ReferenceInformationType = ReferenceMode.IndexToDirect
        // };
        // geometry.Layers[0].Add(matLayer);

        var meshIndices = new List<uint>(TriangleCount * 3);
        // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4657
        for (int componentIndex = 0; componentIndex < NumComponents; componentIndex++)
        {
            int baseVertIndex = componentIndex * VertexCountPerComponent;

            for (int Y = 0; Y < ComponentSizeQuads; Y++)
            {
                for (int X = 0; X < ComponentSizeQuads; X++)
                {
                    if (true) // (VisibilityData[BaseVertIndex + Y * (ComponentSizeQuads + 1) + X] < VisThreshold)
                    {
                        var w1 = baseVertIndex + (X + 0) + (Y + 0) * (ComponentSizeQuads + 1);
                        var w2 = baseVertIndex + (X + 1) + (Y + 1) * (ComponentSizeQuads + 1);
                        var w3 = baseVertIndex + (X + 1) + (Y + 0) * (ComponentSizeQuads + 1);

                        meshIndices.Add((uint)w1);
                        meshIndices.Add((uint)w2);
                        meshIndices.Add((uint)w3);

                        // matLayer.Materials.Add(0); // material index of this face
                        // geometry.Polygons.Add(new Triangle((uint)w1, (uint)w2, (uint)w3));

                        var w4 = baseVertIndex + (X + 0) + (Y + 0) * (ComponentSizeQuads + 1);
                        var w5 = baseVertIndex + (X + 0) + (Y + 1) * (ComponentSizeQuads + 1);
                        var w6 = baseVertIndex + (X + 1) + (Y + 1) * (ComponentSizeQuads + 1);

                        meshIndices.Add((uint)w4);
                        meshIndices.Add((uint)w5);
                        meshIndices.Add((uint)w6);

                        // matLayer.Materials.Add(0); // material index of this face
                        // geometry.Polygons.Add(new Triangle((uint)w4, (uint)w5, (uint)w6));
                    }
                }
            }
        }

        landscapeLod.Indices = new TaskLazy<FRawStaticIndexBuffer>(new FRawStaticIndexBuffer(meshIndices.ToArray()));
        meshIndices.Clear();
        // var staticMesh = new CStaticMesh();
        // staticMesh.LODs.Add(landscapeLod);

        // foreach (var k in WeightMaps) { 
        //     var finfo = new FileInfo($"ExportTest/{ExportName}/weightmap_{k.Key}.png");
        //     finfo.Directory!.Create();
        //     using (var s = finfo.OpenWrite()) {
        //         k.Value.Encode(SKEncodedImageFormat.Png, 100).SaveTo(s);    
        //     }
        // }

        return landscapeLod;

        // var ar = new FArchiveWriter();
        // new Gltf("LandscapeMesh", mesh, null, new ExporterOptions(){MeshFormat = EMeshFormat.Gltf2})
        //     .Save(EMeshFormat.Gltf2, ar);
        //
        // File.OpenWrite("LandScapeMesh.gltf").Write(ar.GetBuffer());
        // ar.Close();
        // var meshIoScene = new Scene();
        // meshIoScene.Nodes.Add(meshNode);

        // var fp = File.OpenWrite("LandscapeMesh.fbx");
        // new FbxWriter(fp, meshIoScene, FbxVersion.v7500).WriteAscii();
    }

    public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
    {
        Debug.Assert(ProcessedFiles != null, nameof(ProcessedFiles) + " != null");
        var b = false;
        label = string.Empty;
        savedFilePath = string.Empty;
        foreach (var pf in ProcessedFiles.Reverse()) { // hack to get the label from first one
            b |= pf.TryWriteToDir(baseDirectory, out label, out savedFilePath);
        }
        return b; // savedFilePath != string.Empty && File.Exists(savedFilePath);
    }

    public override bool TryWriteToZip(out byte[] zipFile)
    {
        throw new NotImplementedException();
    }

    public override void AppendToZip()
    {
        throw new NotImplementedException();
    }
}