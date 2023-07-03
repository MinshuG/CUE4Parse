using System;
using System.IO;
using CUE4Parse_Conversion.Worlds.UnrealFormat;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Worlds;

public class WorldExporter : ExporterBase
{
    private string FileName;
    private byte[] FileData;

    public WorldExporter(UWorld world, ExporterOptions options) : base(world, options)
    {
        using var Ar = new FArchiveWriter();
        string ext;
        switch (Options.WorldFormat)
        {
            case EWorldFormat.Unreal:
                ext = "uworld";
                new UnrealWorld(world, world.Name, options).Save(Ar);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
        }
        
        FileName = $"{PackagePath}.{ext}";
        FileData = Ar.GetBuffer();
    }
    
    public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
    {
        label = string.Empty;
        savedFilePath = string.Empty;
        if (!baseDirectory.Exists) return false;

        savedFilePath = FixAndCreatePath(baseDirectory, FileName);
        File.WriteAllBytes(savedFilePath, FileData);
        label = Path.GetFileName(savedFilePath);
        return File.Exists(savedFilePath);
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