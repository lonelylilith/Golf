using UnityEngine;

namespace Golf
{
    public class QuadraticDrag : MonoBehaviour
    {
        private float mass = 0.045f;
        private float radius = 0.0215f;
        private float dragCoefficient = 0.47f;
        private float airDensity = 1.225f;
        private Vector3 wind = Vector3.zero;
        private Rigidbody _rb;
        private float _area;
        private bool _isLanded = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_isLanded) return;

            // Расчет силы сопротивления воздуха
            Vector3 vRel = _rb.linearVelocity - wind;
            float speed = vRel.magnitude;
            if (speed < 0.1f) return;

            Vector3 dragForce = -0.5f * airDensity * dragCoefficient * _area * speed * vRel;
            _rb.AddForce(dragForce, ForceMode.Force);
        }

        public void SetPhysicalParams(float mass, float radius, float dragCoeff, float density, Vector3 windVec, Vector3 v0)
        {
            this.mass = mass;
            this.radius = radius;
            this.dragCoefficient = dragCoeff;
            this.airDensity = density;
            this.wind = windVec;
            this._area = Mathf.PI * radius * radius;

            _rb.mass = mass;
            _rb.linearVelocity = v0;
            _rb.useGravity = true;
            _isLanded = false;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (_isLanded) return;

            // ВАЖНО: Мяч приземляется ТОЛЬКО если коснулся слоя Course
            // Игнорируем столкновения с пушкой (слой Wall или Default)
            if (other.gameObject.layer == LayerMask.NameToLayer("Course"))
            {
                Landed();
            }
        }

        private void Landed()
        {
            _isLanded = true;
            
            if (TryGetComponent<Ball>(out var golfBall))
            {
                golfBall.enabled = true;
                // Переводим мяч в состояние Moving, чтобы включилось трение гольфа
                golfBall.SetState(Ball.State.Moving);
            }
            
            this.enabled = false; 
        }
    }
}