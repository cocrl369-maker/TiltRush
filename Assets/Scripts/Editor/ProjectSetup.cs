using UnityEditor;
using UnityEngine;

namespace TiltRush.EditorTools
{
    /// <summary>
    /// Runs automatically in the Unity editor: puts the project settings the game needs in place,
    /// so you do not have to click them yourself.
    /// - Landscape only (left and right), never portrait - also written into the Android build.
    /// </summary>
    [InitializeOnLoad]
    static class ProjectSetup
    {
        static ProjectSetup()
        {
            bool changed = false;
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.AutoRotation) { PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation; changed = true; }
            if (PlayerSettings.allowedAutorotateToPortrait) { PlayerSettings.allowedAutorotateToPortrait = false; changed = true; }
            if (PlayerSettings.allowedAutorotateToPortraitUpsideDown) { PlayerSettings.allowedAutorotateToPortraitUpsideDown = false; changed = true; }
            if (!PlayerSettings.allowedAutorotateToLandscapeLeft) { PlayerSettings.allowedAutorotateToLandscapeLeft = true; changed = true; }
            if (!PlayerSettings.allowedAutorotateToLandscapeRight) { PlayerSettings.allowedAutorotateToLandscapeRight = true; changed = true; }
            if (changed) Debug.Log("TILT RUSH: player settings set to landscape only.");
        }
    }
}
