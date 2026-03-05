using UnityEngine;

namespace Golf
{
    public class BallTracker : MonoBehaviour
    {
        [SerializeField]
        private GameObject ball;

        [SerializeField]
        private Vector3 offset;

        void Start()
        {
            transform.parent = null;
        }

        void Update()
        {
            // ДОБАВИТЬ ЭТО: Если ссылка на мяч пуста, пытаемся найти новый мяч или выходим
            if (ball == null) 
            {
                GameObject newBall = GameObject.FindGameObjectWithTag("Ball");
                if (newBall != null) ball = newBall;
                else return; 
            }

            transform.position = ball.transform.position + offset;
        }
    }
}