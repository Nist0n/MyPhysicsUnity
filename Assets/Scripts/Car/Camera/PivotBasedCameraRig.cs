using UnityEngine;

namespace Car.Camera
{
    public abstract class PivotBasedCameraRig : AbstractTargetFollower
    {
        protected Transform MCam;
        protected Transform MPivot;
        
        protected virtual void Awake()
        {
            MCam = GetComponentInChildren<UnityEngine.Camera>().transform;
            MPivot = MCam.parent;
        }
    }
}
