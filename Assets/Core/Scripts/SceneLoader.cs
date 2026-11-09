using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PowerPulse.Core
{
    /// <summary>
    /// Loads scenes asynchronously with a black fade in/out, hiding the loading
    /// gap (important on WebGL, where loading can take a while). Lives on a
    /// persistent GameObject created by the Bootstraper — it needs a MonoBehaviour
    /// host to run coroutines.
    /// </summary>
    public class SceneLoader : MonoBehaviour, ISceneLoader
    {
        #region STATIC

        private const float c_FadeDuration = 0.5f;

        #endregion


        #region FIELDS

        private CanvasGroup m_FadeGroup;
        private bool m_IsLoading;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            CreateFadeOverlay();
        }

        #endregion


        #region PUBLIC METHODS

        /// <inheritdoc />
        public void LoadScene(string sceneName)
        {
            if (m_IsLoading)
                return;

            StartCoroutine(LoadRoutine(sceneName));
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>Builds a full-screen black overlay used for the fade, in code.</summary>
        private void CreateFadeOverlay()
        {
            GameObject canvasObject = new GameObject("Fade Overlay");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            m_FadeGroup = canvasObject.AddComponent<CanvasGroup>();
            m_FadeGroup.alpha = 0f;
            m_FadeGroup.blocksRaycasts = false;

            GameObject imageObject = new GameObject("Fade Image");
            imageObject.transform.SetParent(canvasObject.transform, false);

            Image image = imageObject.AddComponent<Image>();
            image.color = Color.black;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Fades out, loads the scene asynchronously and fades back in.</summary>
        private IEnumerator LoadRoutine(string sceneName)
        {
            m_IsLoading = true;
            m_FadeGroup.blocksRaycasts = true;

            yield return Fade(0f, 1f, c_FadeDuration);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

            while (operation != null && !operation.isDone)
                yield return null;

            yield return Fade(1f, 0f, c_FadeDuration);

            m_FadeGroup.blocksRaycasts = false;
            m_IsLoading = false;
        }

        /// <summary>
        /// Fades the overlay between two alpha values. Uses unscaled time so it
        /// keeps working while the game is paused (time scale 0).
        /// </summary>
        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            m_FadeGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                m_FadeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            m_FadeGroup.alpha = to;
        }

        #endregion
    }
}
