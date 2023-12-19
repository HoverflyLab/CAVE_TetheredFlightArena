//Created By Raymond Aoukar 30/09/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This Script is used to create the DefaultStimulus Scriptable Objects, which are used to store the data necessary to run the stimulus.
    //A DefaultStimulus must be added to each Sequence, the DefaultStimulus chosen is run before the start stimulus button is pressed.
    //DefaultStimulus are necessary as some kind of stimulus must be playing while the software is being configured or between sequences when the experimenter might need time to write notes.
    [CreateAssetMenu(fileName = "new_DefaultStimulus", menuName = "ScriptableObjects/DefaultStimulus", order = 4)]
    public class DefaultStimulus : ScriptableObject
    {
        #region Settings Variables
        [SerializeField, DisableIf(nameof(isLocked))] private StimulusType stimulusType = StimulusType.Stimulus2D;
        [SerializeField, ShowIf(nameof(stimulusType), StimulusType.Stimulus2D), DisableIf(nameof(isLocked))] private Stimulus stimulus = Stimulus.Blank;
        [SerializeField, ShowIf(nameof(showColor)), DisableIf(nameof(isLocked))] private Color32 stimulusColor = new Color32(128,128,128,255);
        [SerializeField, HideIf(nameof(stimulusType), StimulusType.Stimulus2D), Scene, DisableIf(nameof(isLocked))] private string sceneName = null;
        [SerializeField, ShowIf(nameof(stimulusType), StimulusType.RotatingScene), DisableIf(nameof(isLocked)), MinValue(0.2f)] private float secondsPerRevolution = 4f;
        #endregion

        #region Getters
        public string Get_Stimulus_Name() { return this.name; }
        public StimulusType Get_Stimulus_Type() { return stimulusType; }
        public Stimulus Get_Stimulus() { return stimulus; }
        public Color32 Get_Stimulus_Color() { return stimulusColor; }
        public string Get_Scene_Name() { return sceneName; }
        public float Get_Seconds_Per_Revolution() { return secondsPerRevolution; }

        public string Get_Stimulus_Printout() 
        {
            return this.name + "," + stimulusType.ToString() + "," + stimulus.ToString() + "," + stimulusColor.r.ToString() + "," + stimulusColor.g.ToString() 
                            + "," + stimulusColor.b.ToString() + "," + stimulusColor.a.ToString() + "," + sceneName 
                            + "," + secondsPerRevolution.ToString(); 
        }
        #endregion

        #region Setters
        public void Set_Stimulus_Name(string name) { this.name = name + "_replay"; }
        public void Set_Stimulus_Type(StimulusType stimType) { stimulusType = stimType; }
        public void Set_Stimulus(Stimulus stim) { stimulus = stim; }
        public void Set_Stimulus_Color(Color32 newcolor) { stimulusColor = newcolor; }
        public void Set_Scene_Name(string name) { sceneName = name; }
        public void Set_Seconds_Per_Revolution(float revolutions) { secondsPerRevolution = revolutions; }
        #endregion

        public void OnValidate()
        {
            if(stimulusType == StimulusType.Stimulus2D && stimulus == Stimulus.Blank)
            {
                showColor = true;
            }
            else if(showColor == true)
            {
                showColor = false;
            }
        }

        private bool isLocked = false;
        private bool showColor = false;
        
        [Button]
        private void Lock_Variables()
        {
            isLocked = !isLocked;
        }
    }
}
