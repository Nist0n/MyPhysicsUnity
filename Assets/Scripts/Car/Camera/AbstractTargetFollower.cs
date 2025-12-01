using UnityEngine;
using UnityEngine.Serialization;

namespace Car.Camera
{
    public abstract class AbstractTargetFollower : MonoBehaviour
    {
        public enum UpdateType
        {
            FixedUpdate,
            LateUpdate,
        }

        [SerializeField] protected Transform target;
        [SerializeField] private bool autoTargetPlayer = true;
        [SerializeField] private UpdateType updateType;

        protected Rigidbody TargetRigidbody;

        protected virtual void Start()
        {
            if (autoTargetPlayer)
            {
                FindAndTargetPlayer();
            }
            if (!target) return;
            TargetRigidbody = target.GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (autoTargetPlayer && (!target || !target.gameObject.activeSelf))
            {
                FindAndTargetPlayer();
            }
            if (updateType == UpdateType.FixedUpdate)
            {
                FollowTarget(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (autoTargetPlayer && (!target || !target.gameObject.activeSelf))
            {
                FindAndTargetPlayer();
            }
            if (updateType == UpdateType.LateUpdate)
            {
                FollowTarget(Time.deltaTime);
            }
        }

        protected abstract void FollowTarget(float deltaTime);

        public void FindAndTargetPlayer()
        {
            var targetObj = GameObject.FindGameObjectWithTag("Player");
            if (targetObj)
            {
                SetTarget(targetObj.transform);
            }
        }
        
        public virtual void SetTarget(Transform newTransform)
        {
            target = newTransform;
        }
    }
}
