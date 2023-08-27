using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.RenderCore
{
    [JsonConverter(typeof(FPackedNormalConverter))]
    [StructLayout(LayoutKind.Explicit)]
    public struct FPackedNormal
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public readonly sbyte X;
        [System.Runtime.InteropServices.FieldOffset(1)]
        public readonly sbyte Y;
        [System.Runtime.InteropServices.FieldOffset(2)]
        public readonly sbyte Z;
        [System.Runtime.InteropServices.FieldOffset(3)]
        public readonly sbyte W;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public uint Data;
        
        // public readonly uint Data;
        // public float X => (Data & 0xFF) / (float) 127.5 - 1;
        // public float Y => ((Data >> 8) & 0xFF) / (float) 127.5 - 1;
        // public float Z => ((Data >> 16) & 0xFF) / (float) 127.5 - 1;
        // public float W => ((Data >> 24) & 0xFF) / (float) 127.5 - 1;

        public FPackedNormal(FArchive Ar)
        {
            Data = Ar.Read<uint>();
            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.IncreaseNormalPrecision)
                Data ^= 0x80808080;
        }

        public FPackedNormal(uint data)
        {
            Data = data;
        }

        public FPackedNormal(FVector vector)
        {
            // const float Scale = sbyte.MaxValue;
            X = RescaleToInt8(vector.X); // (sbyte)Math.Clamp(Math.Round(vector.X * Scale), sbyte.MinValue, sbyte.MaxValue);
            Y = RescaleToInt8(vector.Y); // (sbyte)Math.Clamp(Math.Round(vector.Y * Scale), sbyte.MinValue, sbyte.MaxValue);
            Z = RescaleToInt8(vector.Z); // (sbyte)Math.Clamp(Math.Round(vector.Z * Scale), sbyte.MinValue, sbyte.MaxValue);
            W = sbyte.MaxValue;
        }

        public FPackedNormal(FVector4 vector) // is this broken?
        {
            // const float Scale = sbyte.MaxValue;
            X = RescaleToInt8(vector.X); //(sbyte)Math.Clamp(Math.Round(vector.X * Scale), sbyte.MinValue, sbyte.MaxValue); // 
            Y = RescaleToInt8(vector.Y); // (sbyte)Math.Clamp(Math.Round(vector.Y * Scale), sbyte.MinValue, sbyte.MaxValue); // RescaleToInt8(vector.Y);
            Z = RescaleToInt8(vector.Z); // (sbyte)Math.Clamp(Math.Round(vector.Z * Scale), sbyte.MinValue, sbyte.MaxValue); // RescaleToInt8(vector.Z);
            W = RescaleToInt8(vector.W); // (sbyte)Math.Clamp(Math.Round(vector.W * Scale), sbyte.MinValue, sbyte.MaxValue); // RescaleToInt8(vector.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RescaleToFloat(sbyte value) => value * (1.0f / sbyte.MaxValue);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte RescaleToInt8(float value) => (sbyte) Math.Clamp(Math.Round(value * sbyte.MaxValue), sbyte.MinValue, sbyte.MaxValue);

        public FVector GetFVector()
        {
            return new(RescaleToFloat(X), RescaleToFloat(Y), RescaleToFloat(Z));
        }
        
        public FVector4 GetFVector4()
        {
            return new(GetFVector(), RescaleToFloat(W));
        }
        
        public Vector3 GetVector3()
        {
            return GetFVector();
        }
        
        public Vector4 GetVector4()
        {
            var vec4 = GetFVector4();
            return new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);
        }

        public static explicit operator FVector(FPackedNormal packedNormal) => packedNormal.GetFVector();
        public static implicit operator FVector4(FPackedNormal packedNormal) => packedNormal.GetFVector4();
        public static explicit operator Vector3(FPackedNormal packedNormal) => packedNormal.GetVector3();
        public static implicit operator Vector4(FPackedNormal packedNormal) => packedNormal.GetVector4();

        public static bool operator ==(FPackedNormal a, FPackedNormal b) => a.Data == b.Data && a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
        public static bool operator !=(FPackedNormal a, FPackedNormal b) => a.Data != b.Data || a.X != b.X || a.Y != b.Y || a.Z != b.Z || a.W != b.W;
    }

    public struct FDeprecatedSerializedPackedNormal
    {
        public uint Data;

        public static FVector4 VectorMultiplyAdd(FVector4 vec1, FVector4 vec2, FVector4 vec3) =>
            new(vec1.X * vec2.X + vec3.X, vec1.Y * vec2.Y + vec3.Y, vec1.Z * vec2.Z + vec3.Z, vec1.W * vec2.W + vec3.W);

        public static explicit operator FVector4(FDeprecatedSerializedPackedNormal packed)
        {
            var vectorToUnpack = new FVector4(packed.Data & 0xFF, (packed.Data >> 8) & 0xFF, (packed.Data >> 16) & 0xFF, (packed.Data >> 24) & 0xFF);
            return VectorMultiplyAdd(vectorToUnpack, new FVector4(1.0f / 127.5f), new FVector4(-1.0f));
        }

        public static explicit operator FVector(FDeprecatedSerializedPackedNormal packed) => (FVector) (FVector4) packed;
    }
}
