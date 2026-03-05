using UnityEngine;

namespace Golf
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryRenderer : MonoBehaviour
    {
        [SerializeField] private int pointsCount = 40;
        [SerializeField] private float timeStep = 0.05f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
        }

        public void DrawWithAirEuler(float mass, float radius, Vector3 startPos, Vector3 v0, float dragCoeff, float density, Vector3 wind)
        {
            _line.positionCount = pointsCount;
            float area = Mathf.PI * radius * radius;
            
            Vector3 currP = startPos;
            Vector3 currV = v0;

            for (int i = 0; i < pointsCount; i++)
            {
                _line.SetPosition(i, currP);

                Vector3 vRel = currV - wind;
                float speed = vRel.magnitude;
                Vector3 dragForce = (-0.5f * density * dragCoeff * area * speed) * vRel;
                Vector3 acceleration = Physics.gravity + (dragForce / mass);

                currV += acceleration * timeStep;
                currP += currV * timeStep;
            }
        }
    }
}