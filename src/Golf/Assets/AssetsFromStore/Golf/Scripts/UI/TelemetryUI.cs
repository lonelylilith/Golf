using UnityEngine;
using TMPro;

namespace Golf
{
    public class TelemetryUI : MonoBehaviour
    {
        public static TelemetryUI Instance;

        [SerializeField] private GameObject panelGolfStats;

        [SerializeField] private TMP_Text txtVelocity;
        [SerializeField] private TMP_Text txtRotation;
        [SerializeField] private TMP_Text txtIncline;
        [SerializeField] private TMP_Text txtResistance;

        private Rigidbody projectileBody;

        private float updateCooldown;
        private const float TICK_INTERVAL = 0.1f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (panelGolfStats) panelGolfStats.SetActive(true);
        }

        private void Update()
        {
            updateCooldown += Time.deltaTime;
            if (updateCooldown < TICK_INTERVAL) return;
            updateCooldown = 0f;

            RefreshProjectileData();
        }

        private void RefreshProjectileData()
        {
            if (Ball.Instance == null) return;

            if (projectileBody == null || projectileBody.gameObject != Ball.Instance.gameObject)
            {
                projectileBody = Ball.Instance.GetComponent<Rigidbody>();
            }

            if (projectileBody == null) return;

            float currentSpeed = projectileBody.linearVelocity.magnitude;
            txtVelocity.text = $"Скорость: {currentSpeed:F2} м/с";

            float spinRate = projectileBody.angularVelocity.magnitude;
            txtRotation.text = $"Вращение: {spinRate:F1} рад/с";

            RaycastHit groundHit;
            bool isGrounded = Physics.Raycast(Ball.Instance.transform.position, Vector3.down, out groundHit, 0.5f);

            if (isGrounded)
            {
                float slopeDeg = Vector3.Angle(Vector3.up, groundHit.normal);
                txtIncline.text = $"Наклон: {slopeDeg:F1}°";
            }
            else
            {
                txtIncline.text = "Наклон: 0.0°";
            }

            if (currentSpeed > 0.1f)
            {
                bool onCollider = isGrounded && groundHit.collider != null;
                string surfaceType = onCollider ? "Трава" : "Воздух";
                float dragAmount = onCollider ? 0.3f : 0.05f;

                txtResistance.text = $"Сопротивление ({surfaceType}): {dragAmount:F2}";
            }
            else
            {
                txtResistance.text = "Сопротивление: Покой";
            }
        }
    }
}