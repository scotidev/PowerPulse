using UnityEngine;
using UnityEngine.UI;
using PowerPulse.Core;

namespace PowerPulse.UI
{
    /// <summary>
    /// Optional click sound for a UI button. Add it next to a Button and assign
    /// a clip. Phase 11 has no click sound by design; this is a placeholder for
    /// when the audio phase lands.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [SerializeField, Tooltip("Clip played when the button is clicked.")]
        private AudioClip m_ClickClip;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(PlayClick);
            }
        }

        #endregion

        #region PRIVATE METHODS

        private void PlayClick()
        {
            if (m_ClickClip == null || ServiceLocator.Current == null)
            {
                return;
            }

            ServiceLocator.Current.Get<IAudioService>()?.PlaySFX(m_ClickClip);
        }

        #endregion
    }
}
