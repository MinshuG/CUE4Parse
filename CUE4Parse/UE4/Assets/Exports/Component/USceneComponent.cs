using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Component
{
    public class USceneComponent : UObject
    {


        public FVector GetRelativeLocation()
        {
            return GetOrDefault("RelativeLocation", new FVector(0, 0, 0));
        }
        
        public FRotator GetRelativeRotation()
        {
            return GetOrDefault("RelativeRotation", new FRotator(0, 0, 0));
        }
        
        public FVector GetRelativeScale3D()
        {
            return GetOrDefault("RelativeScale3D", new FVector(1, 1, 1));
        }
    }
}
