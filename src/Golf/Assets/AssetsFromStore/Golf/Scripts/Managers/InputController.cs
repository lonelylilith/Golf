using UnityEngine;
using UnityEngine.InputSystem; // Важно

namespace Golf
{
    public class InputController : MonoBehaviour
    {
        [SerializeField]
        private LayerMask ballTouchLayerMask;

        public event System.Action OnBallTouchEnter;
        public event System.Action OnBallTouchExit;
        public event System.Action OnBallTouchDown;
        public event System.Action OnBallTouchUp;

        private bool isTouchOverBall;
        public bool IsInteractingWithBall { get; private set; }

        public static InputController Instance;
        
        public GameInput InputActions { get; private set; }

        void Awake()
        {
            Instance = this;
            
            InputActions = new GameInput();
        }

        private void OnEnable()
        {
            InputActions.Gameplay.Enable();

            InputActions.Gameplay.Click.started += OnClickStarted;
            InputActions.Gameplay.Click.canceled += OnClickCanceled;
        }

        private void OnDisable()
        {
            InputActions.Gameplay.Click.started -= OnClickStarted;
            InputActions.Gameplay.Click.canceled -= OnClickCanceled;
            InputActions.Gameplay.Disable();
        }

        private void OnClickStarted(InputAction.CallbackContext context)
        {
            if (isTouchOverBall)
            {
                IsInteractingWithBall = true;
                OnBallTouchDown?.Invoke();
            }
        }

        private void OnClickCanceled(InputAction.CallbackContext context)
        {
            if (IsInteractingWithBall)
            {
                IsInteractingWithBall = false;
                OnBallTouchUp?.Invoke();
            }
        }

        void Update()
        {
            Vector2 mousePos = InputActions.Gameplay.Point.ReadValue<Vector2>();

            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit ballHit;
            Physics.Raycast(ray, out ballHit, 1000, ballTouchLayerMask);

            if(ballHit.collider != null && !isTouchOverBall)
            {
                isTouchOverBall = true;
                OnBallTouchEnter?.Invoke();
            }
            else if(ballHit.collider == null && isTouchOverBall)
            {
                isTouchOverBall = false;
                OnBallTouchExit?.Invoke();
            }
        }
    }
}