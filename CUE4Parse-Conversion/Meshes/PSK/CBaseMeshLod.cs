using System;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public readonly struct CVertexColor {
        public readonly string Name;
        public readonly FColor[] Colors;

        public CVertexColor(string name, FColor[] colors)
        {
            Name = name;
            Colors = colors;
        }
    }

    public class CBaseMeshLod
    {
        public int NumVerts = 0;
        public int NumTexCoords = 0;
        public bool HasNormals = false;
        public bool HasTangents = false;
        public bool IsTwoSided = false;
        public TaskLazy<CMeshSection[]> Sections;
        public TaskLazy<FMeshUVFloat[][]> ExtraUV;
        public FColor[]? VertexColors;
        public CVertexColor[]? ExtraVertexColors;
        public TaskLazy<FRawStaticIndexBuffer>? Indices;
        public bool SkipLod => Sections.Value.Length < 1 || Indices?.Value == null;

        public void AllocateUVBuffers()
        {
            ExtraUV = new TaskLazy<FMeshUVFloat[][]>(() =>
            {
                FMeshUVFloat[][] ret;
                if (NumTexCoords > 0)
                    ret = new FMeshUVFloat[NumTexCoords - 1][];
                else
                {
                    ret = Array.Empty<FMeshUVFloat[]>();
                }

                for (var i = 0; i < ret.Length; i++)
                {
                    ret[i] = new FMeshUVFloat[NumVerts];
                    for (var j = 0; j < ret[i].Length; j++)
                    {
                        ret[i][j] = new FMeshUVFloat(0, 0);
                    }
                }
                return ret;
            });
        }

        public void AllocateVertexColorBuffer()
        {
            VertexColors = new FColor[NumVerts];
            for (var i = 0; i < VertexColors.Length; i++)
            {
                VertexColors[i] = new FColor();
            }

            ExtraVertexColors = Array.Empty<CVertexColor>();
        }
    }
}
