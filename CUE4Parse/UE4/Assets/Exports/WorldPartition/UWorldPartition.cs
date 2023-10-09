using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartition : UObject
{
    public FPackageIndex? StreamingPolicy { get; private set; }
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.WorldPartitionSerializeStreamingPolicyOnCook)
        {
            bool bCooked = Ar.ReadBoolean();
            if (bCooked)
            {
                StreamingPolicy = new FPackageIndex(Ar);
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(StreamingPolicy));
        serializer.Serialize(writer, StreamingPolicy);
    }
}