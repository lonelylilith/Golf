using UnityEngine;

namespace Golf
{
    public class Flag : MonoBehaviour
    {
        [SerializeField]
        private Transform flagObject;

        [SerializeField]
        private float startRiseDistance;

        [SerializeField]
        private float maxRiseHeight;

        private Vector3 flagStartPos;

        void Start()
        {
            flagStartPos = flagObject.localPosition;
        }

        void Update()
        {
            // ДОБАВИТЬ ЭТО: проверка на существование мяча
            if (Ball.Instance == null) return;

            float ballDistance = Vector3.Distance(transform.position, Ball.Instance.transform.position);
            float f = Mathf.Clamp01(startRiseDistance - ballDistance);

            flagObject.localPosition = flagStartPos + (Vector3.up * f * maxRiseHeight);
        }
    }
}