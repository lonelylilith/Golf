using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Добавляем

namespace Golf
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text holeText;
        [SerializeField] private TMP_Text strokeText;

        private bool goingToMenu;

        void Awake()
        {
            GameManager.Instance.OnLoadHole += OnLoadHole;
        }

        void Start()
        {
            Ball.Instance.OnHit += UpdateStrokeText;

            var input = InputController.Instance.InputActions;

            input.Gameplay.Restart.performed += OnRestartAction;
        }

        void OnDestroy()
        {
            if (InputController.Instance != null)
            {
                var input = InputController.Instance.InputActions;
                if(input != null)
                {
                    input.Gameplay.Restart.performed -= OnRestartAction;

                }
            }
        }

        void Update() { }

        void OnLoadHole(CourseData courseData, int hole) {
            holeText.text = $"Лунка {hole}";
            UpdateStrokeText();
        }
        void UpdateStrokeText() { strokeText.text = $"Ход {GameManager.Instance.CurrentStroke}"; }
        public void OnResetCameraButton() { FindAnyObjectByType<CameraController>().SetPosition(Ball.Instance.transform.position); }
        public void OnMenuButton() {
            if(goingToMenu) return;
            goingToMenu = true;
            ScreenFade.Instance.BeginTransition(() => SceneManager.LoadScene("Menu"));
        }

        private void OnRestartAction(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            GameManager.Instance.RestartHole();
        }

    }
}