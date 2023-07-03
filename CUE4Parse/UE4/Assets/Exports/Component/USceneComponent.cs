using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component
{
    public class USceneComponent : UObject
    {
        private FVector RelativeLocation;
        private FRotator RelativeRotation;
        private FVector RelativeScale3D;
        
        private FPackageIndex /*USceneComponent*/ AttachParent;
        private FTransform ComponentToWorld;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            RelativeLocation = GetOrDefault<FVector>(nameof(RelativeLocation), new FVector());
            RelativeRotation = GetOrDefault<FRotator>(nameof(RelativeRotation), new FRotator(EForceInit.ForceInit));
            RelativeScale3D = GetOrDefault<FVector>(nameof(RelativeScale3D), FVector.OneVector);

            AttachParent = GetOrDefault<FPackageIndex>(nameof(AttachParent), new FPackageIndex());

            SetComponentToWorld(new FTransform(RelativeRotation, RelativeLocation, RelativeScale3D));
        }

        private void SetComponentToWorld(FTransform relativeTransform)
        {
            if (GetAttachParent() != null) // CalcNewComponentToWorld_GeneralCases
            {
                ComponentToWorld = relativeTransform * GetAttachParent()!.GetSocketTransform("", ERelativeTransformSpace.RTS_World);
            }
            else
            {
                ComponentToWorld = relativeTransform;
            }
        }

        public FTransform GetSocketTransform(string socketName, ERelativeTransformSpace transformSpace)
        {
            var relativeTransform = new FTransform(RelativeRotation, RelativeLocation, RelativeScale3D);
            if (transformSpace == ERelativeTransformSpace.RTS_World)
            {
                return relativeTransform;
            }
            else
            {
                throw new NotImplementedException();
                return relativeTransform * GetComponentTransform();
            }
        }
        
        public USceneComponent? GetAttachParent()
        {
            return AttachParent.Load<USceneComponent>();
        }
        
        public FTransform GetComponentTransform()
        {
            return ComponentToWorld;
        }
        
        public FVector GetRelativeLocation()
        {
            return RelativeLocation;
        }
        
        public FRotator GetRelativeRotation()
        {
            return RelativeRotation;
        }
        
        public FVector GetRelativeScale3D()
        {
            return RelativeScale3D;
        }
    }
}

public enum ERelativeTransformSpace : int
{
    /** World space transform. */
    RTS_World,
    /** Actor space transform. */
    RTS_Actor,
    /** Component space transform. */
    RTS_Component,
    /** Parent bone space transform */
    RTS_ParentBoneSpace,
};
