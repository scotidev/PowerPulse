using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PowerPulse.Audio;
using PowerPulse.Core;

namespace PowerPulse.UI
{
    /// <summary>
    /// Plays a hover SFX when the mouse pointer enters a UI Button.
    /// Add this component to any Button GameObject.
    /// Requires an AudioManager in the scene (persistent service).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIHoverSound : MonoBehaviour, IPointerEnterHandler
    {
        #region SERIALIZED FIELDS

        [SerializeField, Tooltip("Whether to use the catalog hover sound (UI_Hover) or a custom clip.")]
        private bool m_UseCatalogHoverSound = true;

        [SerializeField, Tooltip("Custom hover clip (used when UseCatalogHoverSound is false).")]
        private AudioClip m_CustomHoverClip;

        [SerializeField, Tooltip("Volume multiplier for the hover sound (0-1).")]
        private float m_Volume = 0.5f;

        [SerializeField, Tooltip("Pitch multiplier for the hover sound.")]
        private float m_Pitch = 1f;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            // Ensure we have a Button component
            var button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("[UIHoverSound] No Button component found on this GameObject.", this);
            }
        }

        #endregion

        #region IPointerEnterHandler

        /// <summary>
        /// Called when the pointer enters the button's rect.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            var audioService = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IAudioService>() : null;
            
            if (audioService == null)
            {
                return;
            }

            if (m_UseCatalogHoverSound)
            {
                // Play from catalog using SfxId.UI_Hover
                audioService.PlaySfx(SfxId.UI_Hover);
            }
            else if (m_CustomHoverClip != null)
            {
                // Play custom clip
                audioService.PlaySfxRaw(m_CustomHoverClip, m_Volume, m_Pitch);
            }
        }

        #endregion
    }
}