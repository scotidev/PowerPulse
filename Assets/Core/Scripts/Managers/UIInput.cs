using UnityEngine.InputSystem;

namespace PowerPulse.Core
{
    /// <summary>
    /// Static accessor for the UI action map. Keeps the UIManager free of
    /// repeated FindAction calls and gives the UI input a single home.
    /// </summary>
    public static class UIInput
    {
        /// <summary>Name of the UI action map.</summary>
        public const string c_MapName = "UI";

        /// <summary>Name of the pause action (Esc).</summary>
        public const string c_Pause = "Pause";

        /// <summary>Name of the confirm action (Enter/E).</summary>
        public const string c_Confirm = "Confirm";

        /// <summary>Name of the cancel action (Esc).</summary>
        public const string c_Cancel = "Cancel";

        /// <summary>Finds a UI action by name. Returns null when the asset is missing.</summary>
        public static InputAction Find(InputActionAsset asset, string actionName)
        {
            if (asset == null)
            {
                return null;
            }

            return asset.FindAction(actionName);
        }
    }
}
