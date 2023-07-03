using CUE4Parse_Conversion.UnrealFormat;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Animations.UnrealFormat;

public class UnrealAnim : ExportableUnrealAssetBase
{
    public UnrealAnim(string name, ExporterOptions options)
    {
        Options = options;
        Header = new FUnrealHeader("UANIM", 1, name, Options.CompressionFormat);
    }

    public UnrealAnim(UAnimSequence anim, string name, ExporterOptions options) : this(name, options)
    {
    }
}