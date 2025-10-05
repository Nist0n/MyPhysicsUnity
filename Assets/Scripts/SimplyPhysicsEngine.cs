using System.Collections.Generic;
using UnityEngine;
namespace Group_9
{

    [RequireComponent(typeof(ForceVisuliizers))]
    public class SimplyPhysicsEngine : MonoBehaviour
    {
        [SerializeField] private float mass;
        [SerializeField] private bool isGravity;
        [SerializeField] private Vector3 windForce;


        private ForceVisuliizers _forceVisualizers;
        private Vector3 _netForce;
        private Vector3 _velocity = Vector3.zero;



        private void Start()
        {
            _forceVisualizers = GetComponent<ForceVisuliizers>();
        }


        private void FixedUpdate()
        {
            _netForce = Vector3.zero;
            _forceVisualizers.ForceClear();

            if (isGravity)
            {
                Vector3 grtavity = Physics.gravity * mass;
                ApplyForce(grtavity, Color.cyan, name: "Gravity");
            }


            ApplyForce(windForce, Color.blue, name: "WindForce");


            Vector3 acceleration = _netForce / mass;
            IntegrateMotion(acceleration);


            _forceVisualizers.AddForce(_netForce, Color.red, name: "ForceMAIN");

        }


        private void IntegrateMotion(Vector3 acceleration)
        {
            _velocity += acceleration * Time.fixedDeltaTime;
            transform.position += _velocity * Time.fixedDeltaTime;
        }

        private void ApplyForce(Vector3 force, Color colorForce, string name)
        {
            _netForce += force;
            _forceVisualizers.AddForce(force, colorForce, name);
        }

    }
}