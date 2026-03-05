using UnityEngine;
using UnityEngine.InputSystem;

namespace Golf
{
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float zoomSensitivity = 2f; // Увеличили для скорости

        [Header("Rotation (Y-Axis Only)")]
        [SerializeField] private float rotationSpeed = 0.15f;

        private Vector3 lastFrameTouchPos;
        private Camera cam;
        
        // Переменная для горизонтального поворота
        private float yaw;
        // Переменная для фиксированного наклона (берется из настроек объекта в Unity)
        private float fixedPitch;

        void Awake()
        {
            cam = GetComponentInChildren<Camera>();
            GameManager.Instance.OnLoadHole += (a, b) => OnLoadHole();

            // Запоминаем начальные углы из инспектора
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            fixedPitch = angles.x; // Сохраняем наклон, который ты выставил в редакторе
        }

        void OnLoadHole()
        {
            SetPosition(Ball.Instance.GetPosition());
        }

        void Update()
        {

            // Если мяча нет (момент между уничтожением и выстрелом), ничего не делаем
            if (Ball.Instance == null) return;

            if (InputController.Instance.IsInteractingWithBall)
                return;

            var input = InputController.Instance.InputActions;

            // --- 1. ZOOM (ЗУМ) ---
            float scrollVal = input.Gameplay.Zoom.ReadValue<Vector2>().y;
            if (Mathf.Abs(scrollVal) > 0.01f)
            {
                // Применяем чувствительность к прокрутке
                Zoom(scrollVal * 0.01f * zoomSensitivity);
            }

            // --- 2. ROTATION (ВРАЩЕНИЕ ТОЛЬКО ПО Y) ---
            if (input.Gameplay.RotateCamera.IsPressed())
            {
                // Нам нужна только X-составляющая движения мыши (влево-вправо)
                float mouseX = input.Gameplay.Look.ReadValue<Vector2>().x;

                yaw += mouseX * rotationSpeed;

                // Применяем толькоYaw. Pitch остается неизменным (fixedPitch)
                transform.rotation = Quaternion.Euler(fixedPitch, yaw, 0);
            }

            // --- 3. PANNING (ПЕРЕМЕЩЕНИЕ НА ЛКМ) ---
            if (input.Gameplay.Click.IsPressed())
            {
                if (input.Gameplay.Click.triggered)
                    lastFrameTouchPos = cam.ScreenToViewportPoint(input.Gameplay.Point.ReadValue<Vector2>());

                Vector3 touchPos = cam.ScreenToViewportPoint(input.Gameplay.Point.ReadValue<Vector2>());
                Vector3 touchDelta = touchPos - lastFrameTouchPos;

                // Направления перемещения всегда привязаны к текущему повороту камеры
                // Но мы проецируем их на землю (плоскость XZ), чтобы камера не "ныряла" в пол
                Vector3 camForwardXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 camRightXZ = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

                float sensitivity = cam.orthographicSize * 2f;
                
                // Перемещаем камеру
                Vector3 move = (camRightXZ * -touchDelta.x * sensitivity * cam.aspect) + 
                               (camForwardXZ * -touchDelta.y * sensitivity);

                transform.position += move;

                lastFrameTouchPos = touchPos;
            }
        }

        public void SetPosition(Vector3 pos)
        {
            // Устанавливаем камеру над мячом, сохраняя её текущую высоту
            transform.position = new Vector3(pos.x, transform.position.y, pos.z);
        }

        public void Zoom(float delta)
        {
            // delta положительная при прокрутке вверх
            cam.orthographicSize -= delta * 5f; 
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}