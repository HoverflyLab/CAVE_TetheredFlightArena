//https://gist.github.com/Chillu1/4c209308dc81104776718b1735c639f7
//https://forum.unity.com/threads/is-there-a-way-to-get-the-current-editor-game-window-display.846040/

/* License only applies to this file.
MIT License
Copyright (c) 2021 Chillu
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TetheredFlight
{
    //This script will only successfully execute on Linux machines which have support for xdotool.
    //This Script creates the stimulus screens (editor windows) and positions them on the appropriate Monitor.
    //The resolutions can be specified within the current Settings Profile.
    //This script assumes that all stimulus screens are left of the user's monitor within the ubuntu system settings and that the stimulus screens are provided in Right to Left order.
    //If you require your stimulus screens to be setup differently e.g Left to Right order or on the right side of the user's monitor this script will need to be edited.
    [InitializeOnLoad]
    public static class CreateStimulusScreens
    {
        private static readonly Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

        private static readonly PropertyInfo showToolbarProperty =
            gameViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo displayProperty = gameViewType.GetProperty("targetDisplay", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly object falseObject = false; // Only box once. This is a matter of principle.
        private static List<EditorWindow> list_Of_Instances = new List<EditorWindow>();

        private static readonly bool fullscreen = true;

        private static EditorWindow Instance = null;

        static CreateStimulusScreens()
        {
            EditorApplication.playModeStateChanged -= ToggleFullScreen;
            if (!fullscreen)
                return;
            EditorApplication.playModeStateChanged += ToggleFullScreen;
        }

        [MenuItem("Window/General/Game (Fullscreen) %#&2", priority = 2)]
        public static void Toggle()
        {
            ToggleFullScreen(PlayModeStateChange.EnteredPlayMode);
        }

        public static void ToggleFullScreen(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredEditMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
            {
                CloseGameWindow();
                return;
            }

            if (gameViewType == null)
            {
                Debug.LogError("Error: GameView type not found.");
                return;
            }

            if (showToolbarProperty == null)
            {
                Debug.LogError("Error: GameView.showToolbar property not found.");
            }

            switch (playModeStateChange)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    return;
                case PlayModeStateChange.EnteredPlayMode: //Used to toggle
                    if (CloseGameWindow())
                        return;
                    break;
            }

            if(UIManager.Instance == null) // human flight scene
            {
                Debug.LogError("Error: UI_Manager does not exist (is okay if doing HumanFlight)");
                return;
            }

            if(UIManager.Instance.Get_isActivating_Stimulus_Screens() == false)
            {
                return;
            }

            List<Vector2Int> stimulusScreens = SettingsManager.Instance.Get_List_Of_Monitor_Resolutions();
            int currentXResolution = 0;

            //Calculate the total resolution of the stimulus screens
            foreach (var resolution in stimulusScreens)
            {
                currentXResolution = currentXResolution + resolution.x;
            }

            //Subtract the X resolution of the users screen as this space is occupied
            currentXResolution = currentXResolution - stimulusScreens[0].x;

            //Start at 1 as the first screen in list (0) is not a stimulus screen, it should be the users screen
            for(int i = 1; i < stimulusScreens.Count; i++)
            {
                list_Of_Instances.Add((EditorWindow) ScriptableObject.CreateInstance(gameViewType));

                showToolbarProperty?.SetValue(list_Of_Instances[i-1], falseObject);
                displayProperty?.SetValue(list_Of_Instances[i-1], i);

                //var desktopResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
                
                Vector2Int screenResolution = stimulusScreens[i];
                currentXResolution = currentXResolution - screenResolution.x;
                var fullscreenRect = new Rect(Vector2.zero, screenResolution);

                Instance = list_Of_Instances[i-1];
                Instance.ShowPopup();
                Instance.Focus();
                Instance.position = fullscreenRect;

                //Moves the screen by the specified value, rightmost screen is created first and must be moved across by the total value minus itself and the users screen.
                //this is because once it has been moved across it will occupy all the resolution from the specified point to the left edge of the users screen.
                //Thus the last screen to be spawned won't be moved across at all (value should be 0). It will occupy all the resolution available between it and the left edge of the 2nd to last screen to be created. 
                string output = RunBash.ExecuteBashCommand("t=$(xdotool getactivewindow windowmove " + currentXResolution + " 0); echo \"$t\";");
            }
            
            StimulusManager.Instance.Activate_Default_Stimulus();
        }

        private static bool CloseGameWindow()
        {
            if(list_Of_Instances.Count <= 0)
            {
                return false;
            }

            for(int i = 0; i < list_Of_Instances.Count; i++)
            {
                if(list_Of_Instances[i] != null)
                {
                    list_Of_Instances[i].Close();
                    list_Of_Instances[i] = null;
                }
            }
            return true;
        }

        public static void Focus_On_Stimulus()
        {
            if(Instance != null)
            {
                Instance.Focus();
            }
        }
    }
}
#endif