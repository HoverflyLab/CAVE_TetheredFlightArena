using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateAllDisplay : MonoBehaviour
{
    // Start is called before the first frame update
    int nDisplaysWanted = 4;
    void Start()
    {
        print("NUMBER OF DISPLAYS AVAILABLE: " + Display.displays.Length);
        print("NUMBER OF DISPLAYS WANTED" + nDisplaysWanted);
        int nDisplays = Mathf.Min(nDisplaysWanted, Display.displays.Length);
        Debug.Log ("displays connected: " + Display.displays.Length);
        for (int i = 1; i < nDisplays; i++) {
            // Display.displays[0] is the primary, default display and is always ON, so start at index 1.
            // Check if additional displays are available and activate each.
            Display.displays[i].Activate();
        }
    }

}
