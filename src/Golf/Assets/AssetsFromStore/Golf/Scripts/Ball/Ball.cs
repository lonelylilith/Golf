using UnityEngine;

namespace Golf
{
    [RequireComponent(typeof(Rigidbody))]
    public class Ball : MonoBehaviour
    {
        public enum State { Waiting, PreWait, Moving, Sunk }

        [SerializeField] private float strikeMultiplier = 22f;
        [SerializeField] private AudioSource sfxEmitter;
        [SerializeField] private AudioClip impactSound;
        [SerializeField] private AudioClip holeSound;

        [SerializeField] private float audioDelay = 0.1f;
        [SerializeField] private float velocityForSound = 0.29f;
        private float timeSinceLastAudio;

        [SerializeField] private LayerMask boundaryMask;
        [SerializeField, Range(0f, 1f)] private float restitutionCoef = 0.58f;
        [SerializeField] private float kineticFriction = 0.15f;
        [SerializeField] private float rollDecay = 0.3f;
        [SerializeField] private float restVelocityTolerance = 0.12f;
        [SerializeField] private float sphereRadius = 0.022f;

        public bool IsAiming { get; private set; }
        public float CurrentPowerPercent { get; private set; }
        public Vector3 CurrentAimDirection { get; private set; }
        public State CurState { get; private set; }
        public static Ball Instance;

        public event System.Action OnHit;
        public event System.Action OnBeginMoving;
        public event System.Action OnBeginWaiting;

        private Vector3 recoveryPosition;
        private float stabilizeTimer = 0.0f;
        private Vector3 previousTickVelocity;
        private Rigidbody physicsBody;

        void Awake()
        {
            Instance = this;
            physicsBody = GetComponent<Rigidbody>();
            physicsBody.linearDamping = 0;
            physicsBody.angularDamping = 0;
            physicsBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            physicsBody.interpolation = RigidbodyInterpolation.Interpolate;

            if (sfxEmitter != null)
            {
                sfxEmitter.playOnAwake = false;
                sfxEmitter.spatialize = false;
            }
        }

        private void EmitAcousticSignal(AudioClip track, float amp = 1f)
        {
            if (track == null || sfxEmitter == null) return;

            float currentTime = Time.unscaledTime;
            if (currentTime - timeSinceLastAudio >= audioDelay)
            {
                sfxEmitter.pitch = Random.Range(0.95f, 1.05f);
                sfxEmitter.PlayOneShot(track, amp);
                timeSinceLastAudio = currentTime;
            }
        }

        void Start()
        {
            InputController.Instance.OnBallTouchDown += HandlePointerPress;
            InputController.Instance.OnBallTouchUp += HandlePointerRelease;
            GameManager.Instance.OnBallSunk += ProcessHoleTrigger;
            GameManager.Instance.OnLoadHole += ResetForNextStage;

            recoveryPosition = transform.position;

            if (boundaryMask.value == 0)
            {
                int layerIdx = LayerMask.NameToLayer("Wall");
                boundaryMask = (layerIdx != -1) ? (1 << layerIdx) : boundaryMask;
            }
        }

        private void HandlePointerPress()
        {
            if (GameManager.Instance.BallInHole || CurState != State.Waiting) return;
            IsAiming = true;
        }

        private void HandlePointerRelease()
        {
            if (!IsAiming || GameManager.Instance.BallInHole) return;

            IsAiming = false;
            Hit(CurrentAimDirection * (CurrentPowerPercent * strikeMultiplier));
            CurrentAimDirection = Vector3.zero;
            CurrentPowerPercent = 0.0f;
        }

        void Update()
        {
            if (!IsAiming) return;

            Camera cam = Camera.main;

            Vector3 objProj = cam.WorldToViewportPoint(transform.position);
            objProj.z = 0;
            objProj.x *= cam.aspect;

            Vector2 inputVal = InputController.Instance.InputActions.Gameplay.Point.ReadValue<Vector2>();
            Vector3 ptrProj = cam.ScreenToViewportPoint(inputVal);
            ptrProj.x *= cam.aspect;

            Vector3 diff = objProj - ptrProj;

            Vector3 fwdPlane = cam.transform.forward;
            fwdPlane.y = 0;

            Vector3 directionCalc = cam.transform.right * diff.x + fwdPlane.normalized * diff.y;

            CurrentAimDirection = directionCalc.normalized;
            CurrentPowerPercent = Mathf.Clamp01(diff.magnitude * 3f);
        }

        void FixedUpdate()
        {
            if (CurState != State.Sunk)
            {
                CalculateInclinationAcceleration();
            }

            if (CurState == State.Waiting)
            {
                if (physicsBody.linearVelocity.sqrMagnitude >= 0.001f)
                {
                    physicsBody.linearVelocity = Vector3.Lerp(physicsBody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                }
                else
                {
                    physicsBody.linearVelocity = Vector3.zero;
                    physicsBody.angularVelocity = Vector3.zero;
                }
                return;
            }

            if (CurState == State.Moving || CurState == State.PreWait)
            {
                previousTickVelocity = physicsBody.linearVelocity;

                Vector3 currentVel = physicsBody.linearVelocity;
                float vMag = currentVel.magnitude;

                if (vMag > 0f)
                {
                    float dt = Time.fixedDeltaTime;
                    float scalar = vMag * Mathf.Clamp01(1.0f - kineticFriction * dt);
                    float decayStep = rollDecay * dt;

                    if (scalar <= decayStep + (restVelocityTolerance * 0.5f))
                    {
                        currentVel = Vector3.zero;
                        physicsBody.angularVelocity = Vector3.zero;
                    }
                    else
                    {
                        currentVel = (currentVel / vMag) * (scalar - decayStep);
                    }

                    physicsBody.linearVelocity = currentVel;
                }

                float sqrVel = physicsBody.linearVelocity.sqrMagnitude;
                if (sqrVel > 0.001f)
                {
                    Vector3 velNorm = physicsBody.linearVelocity.normalized;
                    float omega = Mathf.Sqrt(sqrVel) / sphereRadius;
                    physicsBody.angularVelocity = new Vector3(velNorm.z, 0f, -velNorm.x) * omega;
                }

                EvaluateMotionPhases();
            }
        }

        private void CalculateInclinationAcceleration()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit surfaceHit, 0.5f))
            {
                Vector3 n = surfaceHit.normal;

                if (n.y < 0.9998f)
                {
                    Vector3 gForce = new Vector3(0, -9.81f, 0);
                    Vector3 parallelAcc = gForce - Vector3.Dot(gForce, n) * n;
                    physicsBody.linearVelocity += parallelAcc * Time.fixedDeltaTime;
                }
            }
        }

        void OnCollisionEnter(Collision col)
        {
            if ((boundaryMask.value & (1 << col.gameObject.layer)) != 0)
            {
                Vector3 n = col.contacts[0].normal;
                Vector3 vIn = previousTickVelocity;
                vIn.y = 0;

                Vector3 vOut = vIn - 2f * Vector3.Dot(vIn, n) * n;
                vOut.y = 0;

                physicsBody.linearVelocity = vOut * restitutionCoef;

                float mag = physicsBody.linearVelocity.magnitude;
                if (mag > velocityForSound)
                {
                    EmitAcousticSignal(impactSound, Mathf.Clamp01(mag * 0.1f));
                }
            }
        }

        void LateUpdate()
        {
            EvaluateMotionPhases();
        }

        private void EvaluateMotionPhases()
        {
            float currentV = physicsBody.linearVelocity.magnitude;

            switch (CurState)
            {
                case State.Waiting:
                    if (currentV >= restVelocityTolerance) SetState(State.Moving);
                    break;
                case State.Moving:
                    if (currentV < restVelocityTolerance) SetState(State.PreWait);
                    break;
                case State.PreWait:
                    if (Time.time - stabilizeTimer > 0.2f) SetState(State.Waiting);
                    break;
            }
        }

        public void SetState(State newState)
        {
            CurState = newState;

            if (newState == State.Waiting)
            {
                ValidatePlayableArea();
                recoveryPosition = transform.position;
                OnBeginWaiting?.Invoke();
            }
            else if (newState == State.Moving)
            {
                OnBeginMoving?.Invoke();
            }
            else if (newState == State.PreWait)
            {
                stabilizeTimer = Time.time;
            }
        }

        public void Hit(Vector3 hitForce)
        {
            physicsBody.AddForce(hitForce, ForceMode.VelocityChange);
            OnHit?.Invoke();
            EmitAcousticSignal(impactSound, 1.0f);
        }

        private void ValidatePlayableArea()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, 10f))
            {
                if (hitInfo.collider != null && hitInfo.collider.CompareTag("OutOfBounds"))
                {
                    physicsBody.position = recoveryPosition;
                }
            }
        }

        public void SetPosition(Vector3 position)
        {
            physicsBody.position = position;
            physicsBody.linearVelocity = Vector3.zero;
            physicsBody.angularVelocity = Vector3.zero;
        }

        public Vector3 GetPosition()
        {
            return physicsBody.position;
        }

        private void ProcessHoleTrigger()
        {
            SetState(State.Sunk);
            if (sfxEmitter != null && holeSound != null)
            {
                sfxEmitter.pitch = 1f;
                sfxEmitter.PlayOneShot(holeSound);
            }
        }

        private void ResetForNextStage(CourseData course, int hole)
        {
            SetState(State.Waiting);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBallSunk -= ProcessHoleTrigger;
                GameManager.Instance.OnLoadHole -= ResetForNextStage;
            }
            if (InputController.Instance != null)
            {
                InputController.Instance.OnBallTouchDown -= HandlePointerPress;
                InputController.Instance.OnBallTouchUp -= HandlePointerRelease;
            }
        }
    }
}