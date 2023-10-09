using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartitionRuntimeCellData: UObject
{
    public string DebugName { get; private set; }
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DebugName = Ar.ReadFString();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName(nameof(DebugName));
        writer.WriteValue(DebugName);
    }
}
