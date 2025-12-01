using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Standard_Assets.Cameras.Scripts
{
    public class ProtectCameraFromWallClip : MonoBehaviour
    {
        public float ClipMoveTime = 0.05f;              
        public float ReturnTime = 0.4f;                 
        public float SphereCastRadius = 0.1f;           
        public float ClosestDistance = 0.5f;            
        public bool protecting { get; private set; }    // Используется для определения, есть-ли объект между целью и камерой
        public string DontClipTag = "Player";          

        private Transform _mCam;                  
        private Transform _mPivot;               
        private float _mOriginalDist;             
        private float _mMoveVelocity;             
        private float _mCurrentDist;              
        private Ray _mRay = new Ray();            
        private RaycastHit[] _mHits;              // Результаты проверки между камерой и целью
        private RayHitComparer _mRayHitComparer;  // Переменная для сравнения расстояний попаданий луча


        private void Start()
        {
            _mCam = GetComponentInChildren<Camera>().transform;
            _mPivot = _mCam.parent;
            _mOriginalDist = _mCam.localPosition.magnitude;
            _mCurrentDist = _mOriginalDist;
            _mRayHitComparer = new RayHitComparer();
        }


        private void LateUpdate()
        {
            float targetDist = _mOriginalDist;

            _mRay.origin = _mPivot.position + _mPivot.forward*SphereCastRadius;
            _mRay.direction = -_mPivot.forward;

            var cols = Physics.OverlapSphere(_mRay.origin, SphereCastRadius);

            bool initialIntersect = false;
            bool hitSomething = false;

            // Проходим по всем столкновениям, чтобы проверить, есть ли что-то важное, ну типа вдруг там стена, а мы и не в курсе
            for (int i = 0; i < cols.Length; i++)
            {
                if ((!cols[i].isTrigger) &&
                    !(cols[i].attachedRigidbody && cols[i].attachedRigidbody.CompareTag(DontClipTag)))
                {
                    initialIntersect = true;
                    break;
                }
            }

            // Если есть столкновение
            if (initialIntersect)
            {
                _mRay.origin += _mPivot.forward*SphereCastRadius;

                _mHits = Physics.RaycastAll(_mRay, _mOriginalDist - SphereCastRadius);
            }
            else
            {
                // Если столкновения не было, выполняем проверку сферой, чтобы увидеть, были ли другие столкновения
                _mHits = Physics.SphereCastAll(_mRay, SphereCastRadius, _mOriginalDist + SphereCastRadius);
            }

            // Сортируем столкновения по расстоянию
            Array.Sort(_mHits, _mRayHitComparer);

            float nearest = Mathf.Infinity;

            for (int i = 0; i < _mHits.Length; i++)
            {
                if (_mHits[i].distance < nearest && (!_mHits[i].collider.isTrigger) &&
                    !(_mHits[i].collider.attachedRigidbody &&
                      _mHits[i].collider.attachedRigidbody.CompareTag(DontClipTag)))
                {
                    nearest = _mHits[i].distance;
                    targetDist = -_mPivot.InverseTransformPoint(_mHits[i].point).z;
                    hitSomething = true;
                }
            }

            // перемещаем камеру в лучшее положение
            protecting = hitSomething;
            _mCurrentDist = Mathf.SmoothDamp(_mCurrentDist, targetDist, ref _mMoveVelocity,
                                           _mCurrentDist > targetDist ? ClipMoveTime : ReturnTime);
            _mCurrentDist = Mathf.Clamp(_mCurrentDist, ClosestDistance, _mOriginalDist);
            _mCam.localPosition = -Vector3.forward*_mCurrentDist;
        }


        // Сравниватель для расстояний
        public class RayHitComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                return ((RaycastHit) x).distance.CompareTo(((RaycastHit) y).distance);
            }
        }
    }
}
