//Created By Raymond Aoukar
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This Script is used to create the Trial Scriptable Objects, which are a container for all of the data required to start a trial
    //Scripts within the scene are given this data at runtime to help them function.
    [CreateAssetMenu(fileName = "new_Trial", menuName = "ScriptableObjects/Trial", order = 3)]
    public class Trial : ScriptableObject
    {
        #region Trial Variables
        [SerializeField, DisableIf(nameof(isLocked)), Scene] 
        private string sceneName = null;

        private int posInSequence = 0;

        [SerializeField, DisableIf(nameof(isLocked))] 
        private TrialCompletionType completionType = TrialCompletionType.Duration_Or_Proximity;

        [SerializeField, DisableIf(nameof(isLocked)), Tooltip("Duration in seconds"), MinValue(1f)] 
        private float trialDuration = 30;


        [Header("Pre Stimulus Settings")]
        [SerializeField, DisableIf(nameof(isLocked))] 
        private StimulusType preStimulusType = StimulusType.Stimulus2D;
                
        [SerializeField, DisableIf(nameof(isLocked)), MinValue(0f)] 
        private float preStimulusDuration = 3f;

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(preStimulusType), StimulusType.Stimulus2D)] 
        private Stimulus preStimulus = Stimulus.Blank;

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(preStimulusType), StimulusType.Stimulus2D), Tooltip("Only applied when Stimlus is blank")] 
        private Color32 preStimulusColor = new Color32(128,128,128,255);

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(preStimulusType), StimulusType.RotatingScene), Label("Total Revolutions")] 
        private int preStimulusRevolutions = 3;


        [Header("Post Stimulus Settings")]
        [SerializeField, DisableIf(nameof(isLocked))] 
        private StimulusType postStimulusType = StimulusType.Stimulus2D;

        [SerializeField, DisableIf(nameof(isLocked)), MinValue(0f)] 
        private float postStimulusDuration = 3f;

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(postStimulusType), StimulusType.Stimulus2D)]  
        private Stimulus postStimulus = Stimulus.Blank;

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(postStimulusType), StimulusType.Stimulus2D), Tooltip("Only applied when Stimlus is blank")]
        private Color32 postStimulusColor = new Color32(128,128,128,255);

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(postStimulusType), StimulusType.RotatingScene), Label("Total Revolutions")] 
        private int postStimulusRevolutions = 3;


        [Space]
        [SerializeField, DisableIf(nameof(isLocked)), Expandable] 
        private List<Intervention> list_of_Interventions = new List<Intervention>();

        [Header("The specific object of interest you wish to manipulate")]
        [SerializeField, DisableIf(nameof(isLocked)), BoxGroup("Object of Interest Manipulation"), Expandable, Label("Object of Interest")]
        private List<Object_Of_Interest> list_Of_Objects_Of_Interest = new List<Object_Of_Interest>();
        
        //Needs to be improved to contain Position, Rotation and Transform
        [Header("Transform values are added to objects original position")]
        [SerializeField, DisableIf(nameof(isLocked)), BoxGroup("Object of Interest Manipulation"),Label("Change to Object Position")]
        private List<Vector3> list_Of_OOI_Position_Changes = new List<Vector3>();

        [Header("Proximity range (meters), value of 0 disables tracking")]
        [SerializeField,DisableIf(nameof(isLocked)), HideIf(nameof(completionType), TrialCompletionType.Duration), BoxGroup("Object of Interest Manipulation"), Label("Object Proximity Distance")]
        private List<float> list_Of_OOI_Proximities = new List<float>();

        [Header("Duration (seconds) required to be within the proximity")]
        [SerializeField, DisableIf(nameof(isLocked)), HideIf(nameof(completionType), TrialCompletionType.Duration), BoxGroup("Object of Interest Manipulation"), Label("Object Proximity Duration")]
        private List<float> list_Of_OOI_Proximity_Durations = new List<float>();
        #endregion

        private void OnValidate()
        {
            //Responsible for keeping all of the OOI attribute lists at the same size
            //As each OOI needs a transform for position adjustment and possibly a proximity value depending on completion type
            //These values can be set to 0 in order to have no effect on start position and ignore proximity
            #region List Resize
            totalObjects = list_Of_Objects_Of_Interest.Count;
            totalTransforms = list_Of_OOI_Position_Changes.Count;
            totalProximites = list_Of_OOI_Proximities.Count;
            totalProximityDurations = list_Of_OOI_Proximity_Durations.Count;

            //Are lists the same size
            if(totalObjects != totalTransforms)
            {
                //If there are more objects than transforms
                if(totalObjects > totalTransforms)
                {
                    //Add a new transform
                    list_Of_OOI_Position_Changes.Add(new Vector3(0,0,0));
                    OnValidate();
                }
                else
                {
                    //Remove the last transform
                    list_Of_OOI_Position_Changes.RemoveAt(totalTransforms-1);
                    OnValidate();
                }
            }

            //Are lists the same size
            if(totalObjects != totalProximites)
            {
                //If there are more objects than transforms
                if(totalObjects > totalProximites)
                {
                    //Add a new transform
                    list_Of_OOI_Proximities.Add(0.5f);
                    OnValidate();
                }
                else
                {
                    //Remove the last transform
                    list_Of_OOI_Proximities.RemoveAt(totalProximites-1);
                    OnValidate();
                }
            }

            //Are lists the same size
            if (totalObjects != totalProximityDurations)
            {
                //If there are more objects than transforms
                if (totalObjects > totalProximityDurations)
                {
                    //Add a new transform
                    list_Of_OOI_Proximity_Durations.Add(0.5f);
                    OnValidate();
                }
                else
                {
                    //Remove the last transform
                    list_Of_OOI_Proximity_Durations.RemoveAt(totalProximityDurations - 1);
                    OnValidate();
                }
            }
            #endregion

            //Keep Proximities 0 and above
            for (int i = 0; i < list_Of_OOI_Proximities.Count; i++)
            {
                if(list_Of_OOI_Proximities[i] < 0f)
                {
                    list_Of_OOI_Proximities[i] = 0f;
                }
            } 
        }

        #region Getters
        public string Get_Trial_Name() { return this.name; }
        public string Get_Scene_Name() { return sceneName; }
        public int Get_Position_In_Sequence() { return posInSequence; }
        public TrialCompletionType Get_Trial_Completion_Type() { return completionType; }
        public float Get_Trial_Duration() { return trialDuration; }

        //pre
        public StimulusType Get_Pre_Stimulus_Type() { return preStimulusType; }
        public float Get_Pre_Stimulus_Duration() { return preStimulusDuration; }
        public Stimulus Get_Pre_Stimulus() { return preStimulus; }
        public Color32 Get_Pre_Stimulus_Color() { return preStimulusColor; }
        public int Get_Number_Of_Pre_Stimulus_Revolutions() { return preStimulusRevolutions; }

        //post
        public StimulusType Get_Post_Stimulus_Type() { return postStimulusType; }
        public float Get_Post_Stimulus_Duration() { return postStimulusDuration; }
        public Stimulus Get_Post_Stimulus() { return postStimulus; }
        public Color32 Get_Post_Stimulus_Color() { return postStimulusColor; }
        public int Get_Number_Of_Post_Stimulus_Revolutions() { return postStimulusRevolutions; }

        public List<Intervention> Get_List_Of_Interventions() { return list_of_Interventions; }
        public List<Object_Of_Interest> Get_List_Of_Objects_Of_Interest() { return list_Of_Objects_Of_Interest; }
        public List<Vector3> Get_List_Of_OOI_Position_Changes() { return list_Of_OOI_Position_Changes; }
        public List<float> Get_List_Of_OOI_Proximities() {return list_Of_OOI_Proximities; }
        public List<float> Get_List_Of_OOI_Proximity_Durations() { return list_Of_OOI_Proximity_Durations; }

        public string Get_Trial_Printout() 
        { 
            return this.name + "," + sceneName + "," + posInSequence + "," + completionType.ToString() + "," + trialDuration.ToString() + "," + preStimulusDuration.ToString() 
                    + "," + postStimulusDuration.ToString(); 
        }

        public string Get_OOI_Change_Printout(int ChangeNo) 
        {
            return list_Of_Objects_Of_Interest[ChangeNo].Get_Object_Of_Interest_ID().ToString() + "," + list_Of_OOI_Position_Changes[ChangeNo].x.ToString()
                    + "," + list_Of_OOI_Position_Changes[ChangeNo].y.ToString() + "," + list_Of_OOI_Position_Changes[ChangeNo].z.ToString()
                    + "," + list_Of_OOI_Proximities[ChangeNo].ToString() + "," + list_Of_OOI_Proximity_Durations[ChangeNo].ToString(); 
        }

        public string Get_Pre_Stimulus_Printout()
        {
            return preStimulusType.ToString() + "," + preStimulusDuration.ToString() + "," + preStimulus.ToString() + "," + preStimulusColor.r.ToString()
                    + "," + preStimulusColor.b.ToString() + "," + preStimulusColor.b.ToString() + "," + preStimulusColor.a.ToString()
                     + "," + preStimulusRevolutions.ToString();
        }

        public string Get_Post_Stimulus_Printout()
        {
            return postStimulusType.ToString() + "," + postStimulusDuration.ToString() + "," + postStimulus.ToString() + "," + postStimulusColor.r.ToString()
                    + "," + postStimulusColor.b.ToString() + "," + postStimulusColor.b.ToString() + "," + postStimulusColor.a.ToString()
                     + "," + postStimulusRevolutions.ToString();
        }
        #endregion

        #region Setters
        //
        //The following methods should only be used by the replay manager when it instantiates a new scriptable,
        //it uses these methods to recreate the trial saved in the data text files and allows the instantiated scriptable to be run through the Sequence / Trial manager.
        //
        public void Set_Trial_Name(string name) { this.name = name; }
        public void Set_Scene_Name(string name) { sceneName = name; }
        public void Set_Position_In_Sequence(int pos) { posInSequence = pos; }
        public void Set_Trial_Completion_Type(TrialCompletionType type) { completionType = type; }
        public void Set_Trial_Duration(float duration) { trialDuration = duration; }

        //pre
        public void Set_Pre_Stimulus_Type(StimulusType stimType) { preStimulusType = stimType; }
        public void Set_Pre_Stimulus_Duration(float duration) { preStimulusDuration = duration; }
        public void Set_Pre_Stimulus(Stimulus stim) { preStimulus = stim; }
        public void Set_Pre_Stimulus_Color(Color32 color) { preStimulusColor = color; }
        public void Set_Number_Of_Pre_Stimulus_Revolutions(int revolutions) { preStimulusRevolutions = revolutions; }

        //post
        public void Set_Post_Stimulus_Type(StimulusType stimType) { postStimulusType = stimType; }
        public void Set_Post_Stimulus_Duration(float duration) { postStimulusDuration = duration; }
        public void Set_Post_Stimulus(Stimulus stim) { postStimulus = stim; }
        public void Set_Post_Stimulus_Color(Color32 color) { postStimulusColor = color; }
        public void Set_Number_Of_Post_Stimulus_Revolutions(int revolutions) { postStimulusRevolutions = revolutions; }

        public void Add_Intervention_To_List_Of_Interventions(Intervention intervention) { list_of_Interventions.Add(intervention); }
        public void Add_OOI_To_List_Of_Object_Of_Interest(Object_Of_Interest OOI) { list_Of_Objects_Of_Interest.Add(OOI); }
        public void Add_Position_To_List_Of_OOI_Position_Changes(Vector3 position) { list_Of_OOI_Position_Changes.Add(position); }
        public void Add_Proximity_To_List_Of_OOI_Proximities(float proximity) { list_Of_OOI_Proximities.Add(proximity); }
        public void Add_Proximity_Duration_To_List_Of_OOI_Proximity_Durations(float proximityDuration) { list_Of_OOI_Proximity_Durations.Add(proximityDuration); }
        #endregion

        #region Buttons
        [Button]
        private void Lock_Variables()
        {
            isLocked = !isLocked;
        }
        #endregion

        #region Editor Logic Variables
        private bool isLocked = false;
        #endregion

        #region OnValidate Variables
        private int totalObjects = 0;
        private int totalTransforms = 0;
        private int totalProximites = 0;
        private int totalProximityDurations = 0;
        #endregion
    }
}
