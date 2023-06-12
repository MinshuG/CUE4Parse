using CUE4Parse.UE4.Writers;
using Ionic.Zlib;
using ZstdSharp;

namespace CUE4Parse_Conversion.UnrealFormat;

public class ExportableUnrealAssetBase
{
    public int Length => GetLength();
    
    protected readonly FArchiveWriter Ar = new();
    protected FUnrealHeader Header;
    protected ExporterOptions Options;

    private const int ZSTD_LEVEL = 6;
    
    public void Save(FArchiveWriter archive)
    {
        Header.Serialize(archive);

        var data = Ar.GetBuffer();
        var finalData = Header.CompressionFormat switch
        {
            EFileCompressionFormat.None => data,
            EFileCompressionFormat.GZIP => GZipStream.CompressBuffer(data),
            EFileCompressionFormat.ZSTD => new Compressor(ZSTD_LEVEL).Wrap(data)
        };
        
        archive.Write(finalData);
    }

    private int GetLength() // TODO make not ugly in the future
    {
        using var miniArchive = new FArchiveWriter();
        Header.Serialize(miniArchive);

        var data = Ar.GetBuffer();
        var finalData = Header.CompressionFormat switch
        {
            EFileCompressionFormat.None => data,
            EFileCompressionFormat.GZIP => GZipStream.CompressBuffer(data),
            EFileCompressionFormat.ZSTD => new Compressor(ZSTD_LEVEL).Wrap(data)
        };
        
        miniArchive.Write(finalData);

        return (int) miniArchive.Length;
    }
}