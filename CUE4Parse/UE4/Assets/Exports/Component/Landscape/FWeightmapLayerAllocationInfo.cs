﻿using System;
using System.Diagnostics;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape; 

[StructFallback]
public class FWeightmapLayerAllocationInfo: IEquatable<FWeightmapLayerAllocationInfo> {
    public FPackageIndex LayerInfo;
    public byte WeightmapTextureIndex;
    public byte WeightmapTextureChannel;

    public FWeightmapLayerAllocationInfo(FStructFallback fallback) {
        LayerInfo = fallback.GetOrDefault(nameof(LayerInfo), new FPackageIndex());
        WeightmapTextureIndex = fallback.GetOrDefault<byte>(nameof(WeightmapTextureIndex));
        WeightmapTextureChannel = fallback.GetOrDefault<byte>(nameof(WeightmapTextureChannel));
    }

    public bool Equals(FWeightmapLayerAllocationInfo? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return LayerInfo.Owner == other.LayerInfo.Owner && LayerInfo.Index == other.LayerInfo.Index &&
               WeightmapTextureIndex == other.WeightmapTextureIndex && WeightmapTextureChannel == other.WeightmapTextureChannel;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FWeightmapLayerAllocationInfo)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(LayerInfo, WeightmapTextureIndex, WeightmapTextureChannel);
    }
}
