using UnityEngine;
using System.Collections;

namespace Golf
{
    [DefaultExecutionOrder(-1)]
    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private CourseList courseList;

        // --- НОВОЕ: Аудио ---
        [Header("Audio")]
        [SerializeField] private AudioSource gameBGM;

        public CourseData CurrentCourse { get; private set; }
        public GameObject CurrentHoleObject { get; private set; }
        public int CurrentHole { get; private set; }
        public bool BallInHole { get; private set; }
        
        // Свойства для счета
        public int CurrentStroke
        {
            get
            {
                if(Strokes == null) return -1;
                return Strokes[CurrentHole - 1];
            }
            private set
            {
                if(Strokes == null || CurrentHole > Strokes.Length) return;
                Strokes[CurrentHole - 1] = value;
            }
        }
        public int CurrentPar { get; private set; }
        public int[] Strokes { get; private set; }

        public event System.Action<CourseData> OnLoadCourse;
        public event System.Action<CourseData, int> OnLoadHole;
        public event System.Action OnBallSunk;

        public static GameManager Instance;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // --- НОВОЕ: Запуск музыки ---
            if (gameBGM != null)
            {
                // Если галочка "Play On Awake" не стоит, запускаем вручную
                if (!gameBGM.isPlaying) gameBGM.Play();
            }
            else
            {
                // Попытка найти аудио на самом объекте GameManager, если забыли привязать
                gameBGM = GetComponent<AudioSource>();
                if (gameBGM != null && !gameBGM.isPlaying) gameBGM.Play();
            }

            if(!PlayerPrefs.HasKey("CourseToPlay"))
            {
                Debug.LogError("'CourseToPlay' PlayerPref has not been found! Make sure to load the game from the Menu scene.");
                return;
            }

            LoadCourse(PlayerPrefs.GetInt("CourseToPlay"));

            Ball.Instance.OnHit += OnBallHit;
        }

        // --- НОВОЕ: Метод для плавного затухания (вызывается из HUDManager) ---
        public void FadeOutMusic(float duration)
        {
            if (gameBGM != null)
            {
                StartCoroutine(FadeOutMusicRoutine(duration));
            }
        }

        private IEnumerator FadeOutMusicRoutine(float duration)
        {
            float startVolume = gameBGM.volume;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                gameBGM.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                yield return null;
            }
            gameBGM.Stop();
        }

        // ... Дальше стандартный код без изменений ...

        void LoadCourse(int courseListIndex)
        {
            CurrentCourse = courseList.Courses[courseListIndex];
            Strokes = new int[CurrentCourse.Holes.Length];

            if(CurrentCourse.Holes.Length == 0)
            {
                Debug.LogError($"Course '{CurrentCourse.DisplayName}' has no holes.");
                return;
            }

            LoadHole(1);
        }

        void LoadHole(int hole)
        {
            CurrentHole = hole;
            BallInHole = false;

            if(hole > CurrentCourse.Holes.Length)
            {
                Debug.LogError($"Hole {hole} doesn't exist in the course.");
                return;
            }

            if(CurrentCourse.Holes[hole - 1].HolePrefab == null)
            {
                Debug.LogError($"Hole {hole} doesn't have a prefab.");
                return;
            }

            CurrentPar = CurrentCourse.Holes[hole - 1].Par;

            if(CurrentHoleObject != null)
            {
                Destroy(CurrentHoleObject);
            }

            CurrentHoleObject = Instantiate(CurrentCourse.Holes[hole - 1].HolePrefab, Vector3.zero, Quaternion.identity);

            GameObject ballStart = GameObject.FindGameObjectWithTag("BallStart");

            if(ballStart == null)
            {
                Debug.LogError($"Hole {hole} doesn't have a ball start.");
                return;
            }

            Ball.Instance.SetPosition(ballStart.transform.position);
            ballStart.SetActive(false);

            OnLoadHole?.Invoke(CurrentCourse, hole);
        }

        public void BallSinked()
        {
            BallInHole = true;
            OnBallSunk?.Invoke();

            StartCoroutine(PostBallSink());
        }

        IEnumerator PostBallSink()
        {
            yield return new WaitForSeconds(2.0f);

            if(CurrentHole < CurrentCourse.Holes.Length)
                ScreenFade.Instance.BeginTransition(() => LoadHole(CurrentHole + 1));
            else
                ScoreboardUI.Instance.ToggleScoreboard(true);
        }

        void OnBallHit()
        {
            CurrentStroke++;
        }

         public void RestartHole()
        {
            if (Strokes != null && CurrentHole <= Strokes.Length)
            {
                Strokes[CurrentHole - 1] = 0; 
            }
            LoadHole(CurrentHole);
        }
    }
}