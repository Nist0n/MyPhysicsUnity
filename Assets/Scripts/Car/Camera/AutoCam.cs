using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR

#endif

namespace Car.Camera
{
    [ExecuteInEditMode]
    public class AutoCam : PivotBasedCameraRig
    {
        [SerializeField] private float moveSpeed = 15;
        [SerializeField] private float turnSpeed = 15;
        [SerializeField] private float rollSpeed = 3f;
        [SerializeField] private bool followVelocity = false;
        [SerializeField] private bool followTilt = true;
        [SerializeField] private float spinTurnLimit = 180;
        [SerializeField] private float targetVelocityLowerLimit = 4f;
        [SerializeField] private float smoothTurnTime = 0.2f;

        private float _lastFlatAngle;
        private float _currentTurnAmount;
        private float _turnSpeedVelocityChange;
        private Vector3 _rollUp = Vector3.up;
        
        protected override void FollowTarget(float deltaTime)
        {
            if (!(deltaTime > 0) || !target)
            {
                return;
            }
            
            var targetForward = target.forward;
            var targetUp = target.up;

            if (followVelocity && Application.isPlaying)
            {
                if (TargetRigidbody.linearVelocity.magnitude > targetVelocityLowerLimit)
                {
                    targetForward = TargetRigidbody.linearVelocity.normalized;
                    targetUp = Vector3.up;
                }
                else
                {
                    targetUp = Vector3.up;
                }
                _currentTurnAmount = Mathf.SmoothDamp(_currentTurnAmount, 1, ref _turnSpeedVelocityChange, smoothTurnTime);
            }
            else
            {
                var currentFlatAngle = Mathf.Atan2(targetForward.x, targetForward.z)*Mathf.Rad2Deg;
                if (spinTurnLimit > 0)
                {
                    var targetSpinSpeed = Mathf.Abs(Mathf.DeltaAngle(_lastFlatAngle, currentFlatAngle))/deltaTime;
                    var desiredTurnAmount = Mathf.InverseLerp(spinTurnLimit, spinTurnLimit*0.75f, targetSpinSpeed);
                    var turnReactSpeed = (_currentTurnAmount > desiredTurnAmount ? .1f : 1f);
                    if (Application.isPlaying)
                    {
                        _currentTurnAmount = Mathf.SmoothDamp(_currentTurnAmount, desiredTurnAmount,
                                                             ref _turnSpeedVelocityChange, turnReactSpeed);
                    }
                    else
                    {
                        _currentTurnAmount = desiredTurnAmount;
                    }
                }
                else
                {
                    _currentTurnAmount = 1;
                }
                _lastFlatAngle = currentFlatAngle;
            }

            transform.position = Vector3.Lerp(transform.position, target.position, deltaTime*moveSpeed);
            
            if (!followTilt)
            {
                targetForward.y = 0;
                if (targetForward.sqrMagnitude < float.Epsilon)
                {
                    targetForward = transform.forward;
                }
            }
            var rollRotation = Quaternion.LookRotation(targetForward, _rollUp);

            if (rollSpeed > 0) _rollUp = Vector3.Slerp(_rollUp, targetUp, rollSpeed * deltaTime);
            else _rollUp = Vector3.up;
            transform.rotation = Quaternion.Lerp(transform.rotation, rollRotation, turnSpeed*_currentTurnAmount*deltaTime);
        }
    }
}
