using CUE4Parse.UE4.Writers;
using Ionic.Zlib;

namespace CUE4Parse_Conversion.UnrealFormat;

public class ExportableUnrealAssetBase
{
    public int Length => GetLength();
    
    protected readonly FArchiveWriter Ar = new();
    protected FUnrealHeader Header;
    
    public void Save(FArchiveWriter archive)
    {
        Header.Serialize(archive);

        var data = Ar.GetBuffer();
        var finalData = Header.CompressionFormat switch
        {
            EFileCompressionFormat.None => data,
            EFileCompressionFormat.GZIP => GZipStream.CompressBuffer(data)
        };
        
        archive.Write(finalData);
    }

    private int GetLength()
    {
        using var miniArchive = new FArchiveWriter();
        Header.Serialize(miniArchive);

        var data = Ar.GetBuffer();
        var finalData = Header.CompressionFormat switch
        {
            EFileCompressionFormat.None => data,
            EFileCompressionFormat.GZIP => GZipStream.CompressBuffer(data)
        };
        
        miniArchive.Write(finalData);

        return (int) miniArchive.Length;
    }
}