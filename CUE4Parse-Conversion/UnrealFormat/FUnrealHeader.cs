using System;
using System.Text;
using CUE4Parse.UE4.Writers;
using Serilog;

namespace CUE4Parse_Conversion.UnrealFormat;

public class FUnrealHeader
{
    public EFileCompressionFormat CompressionFormat;
    private string Identifier;
    private int FileVersion;
    private string ObjectName;
    private const string MAGIC = "UNREALFORMAT";

    public FUnrealHeader(string identifier, int fileVersion, string objectName, EFileCompressionFormat compressionFormat = EFileCompressionFormat.None)
    {
        Identifier = identifier;
        FileVersion = fileVersion;
        ObjectName = objectName;
        CompressionFormat = compressionFormat;
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        var padded = new byte[MAGIC.Length];
        var bytes = Encoding.UTF8.GetBytes(MAGIC); 
        Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
        Ar.Write(padded);
        
        Ar.WriteFString(Identifier);
        Ar.Write(FileVersion);
        Ar.WriteFString(ObjectName);

        var isCompressed = CompressionFormat != EFileCompressionFormat.None;
        Ar.Write(isCompressed);
        if (isCompressed)
        {
            Ar.WriteFString(CompressionFormat.ToString());
        }
    }
}