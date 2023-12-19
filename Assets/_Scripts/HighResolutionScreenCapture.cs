using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

public class HighResolutionScreenCapture : MonoBehaviour
{
    [SerializeField] private string Filename = "ScreenCapture_Default";
    private string ScreenCaptureDateTime = "";

    [Button]
    private void TriggerCapture()
    {
        if(EditorApplication.isPlaying == true)
        {
            ScreenCaptureDateTime = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");
            ScreenCapture.CaptureScreenshot("External Files/ScreenCaptures/" + ScreenCaptureDateTime + "_" + Filename + ".png");
            print("ScreenCaptured and sent to External Files > ScreenCaptures");
        }
        else
        {
            print("Must be in play mode to use this functionality.");
        }
    }
}
