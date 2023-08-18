using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CMeshVertex
    {
        public FVector Position;
        public FVector Normal;
        public FVector Tangent;
        public FMeshUVFloat UV;

        public CMeshVertex(FVector position, FPackedNormal normal, FPackedNormal tangent, FMeshUVFloat uv)
        {
            Position = position;
            Normal = normal.GetFVector();
            Tangent = tangent.GetFVector();
            UV = uv;
        }

        public CMeshVertex(FVector position, FVector normal, FVector tangent, FMeshUVFloat uv)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            UV = uv;
        }
    }
}