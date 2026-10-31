using UnityEngine;

namespace TiltRush
{
    /// <summary>Which texts / controls to show: phone (tilt + touch) or PC (keyboard, only for testing).</summary>
    public static class Platform
    {
        public static bool IsPhone { get { return Application.isMobilePlatform || RemoteConnected; } }

        /// <summary>
        /// Unity Remote 5 (free app) is connected: the editor receives the phone's tilt and touches.
        /// Lets you test the tilt controls by pressing Play in the editor - no build needed.
        /// </summary>
        public static bool RemoteConnected
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorApplication.isRemoteConnected;
#else
                return false;
#endif
            }
        }

        public static string MoveHint
        {
            get { return IsPhone ? "Tilt your phone to move." : "Move with WASD or the arrow keys (PC testing)."; }
        }

        public static string CoopHint
        {
            get
            {
                return IsPhone
                    ? "Co-op: lay the phone flat on the table between you. Player 1 drags a thumb stick on the LEFT half, player 2 on the RIGHT half."
                    : "Co-op on PC: player 1 = WASD, player 2 = arrow keys.";
            }
        }
    }
}
