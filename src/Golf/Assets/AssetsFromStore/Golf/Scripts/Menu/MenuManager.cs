using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Golf
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject coursesScreen;
        [SerializeField] private CourseSlotUI[] courseSlots;
        [SerializeField] private CourseList courseList;

        // --- НОВОЕ ПОЛЕ ---
        [Header("Audio")]
        [SerializeField] private AudioSource menuBGM; 

        private bool loadingCourse = false;

        void Start()
        {
            SetScreen(mainMenuScreen);
            
            if (menuBGM == null) menuBGM = GetComponentInChildren<AudioSource>();
        }


        void PlayCourse(int courseListIndex)
        {
            if(loadingCourse)
                return;

            PlayerPrefs.SetInt("CourseToPlay", courseListIndex);
            loadingCourse = true;

            if (menuBGM != null)
            {
                StartCoroutine(FadeOutMusic(0.5f));
            }

            string sceneToLoad = courseList.Courses[courseListIndex].GameScene;
            ScreenFade.Instance.BeginTransition(() => SceneManager.LoadScene(sceneToLoad));
        }

        private System.Collections.IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = menuBGM.volume;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                menuBGM.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                yield return null;
            }
            menuBGM.Stop();
        }

        void SetScreen(GameObject screen) {
            mainMenuScreen.SetActive(false);
            coursesScreen.SetActive(false);
            screen.SetActive(true);
            if(screen == coursesScreen) UpdateCoursesScreen();
        }
        public void OnCoursesButton() { SetScreen(coursesScreen); }
        public void OnQuitButton() { Application.Quit(); }
        public void OnBackButton() { SetScreen(mainMenuScreen); }

        void UpdateCoursesScreen() {
            for(int i = 0; i < courseSlots.Length; i++) {
                if(i >= courseList.Courses.Length) { courseSlots[i].gameObject.SetActive(false); continue; }
                courseSlots[i].gameObject.SetActive(true);
                courseSlots[i].Initialize(courseList.Courses[i]);
                Button courseButton = courseSlots[i].GetComponent<Button>();
                courseButton.onClick.RemoveAllListeners();
                int courseIndex = i;
                courseButton.onClick.AddListener(() => PlayCourse(courseIndex));
            }
        }
    }
}