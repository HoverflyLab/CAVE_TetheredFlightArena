//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System;

namespace TetheredFlight
{
    //This Script manages the loading of Trials and handles various alterations such as interpolations and interventions.
    public class TrialManager : MonoBehaviour
    {
        public static TrialManager Instance = null;

        [SerializeField, Tooltip("Prints additional debug logs if true.")] private bool verbose = false;

        private Trial trial = null;
        private Coroutine currentCoroutine = null;
        private int trialNo = 0;
        private List<Interpolation> Interpolations = new List<Interpolation>();
        private float interpValue = default;
        private Vector2 interpRange = new Vector2();
        private TrialCompletedBy trialCompletedBy = TrialCompletedBy.Unknown;
        private int completedByID = -1;
        private string completedByName = "";
        private string CSV_filepath = "";

        private string prestimStartTimeStamp = "";
        private string loopClosedTimeStamp = "";
        private string poststimStartTimeStamp = "";
        private string poststimFinishTimeStamp = "";

        #region trial variables
        private float trialDuration = default;
        private float preStimDuration = default; 
        private float postStimDuration = default;
        private TrialCompletionType completionType = TrialCompletionType.Duration;
        private Transform OOI_transform = null;
        private List<Object_Of_Interest> list_of_OOI = new List<Object_Of_Interest>();
        private int packetsThisTrial = 0; //updated by DataProcessor 
        #endregion

        private StreamWriter Writer = null;
        private bool waitingForScene = false;

        public Action PreStimulusStarted;
        public Action CloseLoop;
        public Action OpenLoop;
        public Action PostStimulusComplete;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one Trial Manager");
                Destroy(this.gameObject);
            }
            
            SceneManager.sceneLoaded += Trial_Scene_Loaded;
        }

        public void InitialiseTrial(Trial _trial)
        {
            if(_trial == null)
            {
                Debug.LogError("Error: Trial was null");
                return;
            }

            currentCoroutine = null;

            trial = _trial;
            trialNo = SequenceManager.Instance.Get_Trial_No();
            trialCompletedBy = TrialCompletedBy.Unknown;
            CSV_filepath = "";
            completedByID = -1;
            completedByName = "";

            UIManager.Instance.Update_Stimulus_Menu(SequenceManager.Instance.Get_Sequence_No(), SequenceManager.Instance.Get_No_of_Sequences(), trialNo + 1, 
            SequenceManager.Instance.Get_No_Of_Trials(), trial.Get_Pre_Stimulus_Duration(), trial.Get_Trial_Duration(), trial.Get_Post_Stimulus_Duration());

            //Necessary for replaying trials as we can't rely on sequence manager for this value (used for interpolations)
            trial.Set_Position_In_Sequence(trialNo);

            Create_Trial_Output_File();

            //For first trial in the sequence find the list of interpolations
            if (trialNo == 0)
            {
                Interpolations = SequenceManager.Instance.Get_Current_Sequence_Interpolations();
            }

            Apply_Variables();

            if (verbose) { print("Starting Trial - " + trial.Get_Trial_Name()); }
            waitingForScene = true;
            //StartCoroutine(LoadYourAsyncScene(trial.Get_Scene_Name()));
            SceneManager.LoadScene(trial.Get_Scene_Name());
        }

        // IEnumerator LoadYourAsyncScene(string sceneName)
        // {
        //     // The Application loads the Scene in the background as the current Scene runs.
        //     // This is particularly good for creating loading screens.
        //     // You could also load the Scene by using sceneBuildIndex. In this case Scene2 has
        //     // a sceneBuildIndex of 1 as shown in Build Settings.

        //     AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        //     // Wait until the asynchronous scene fully loads
        //     while (!asyncLoad.isDone)
        //     {
        //         yield return null;
        //     }
        // }

        #region Scene Setup Methods
        //Sets the basic trial variables such as 
        private void Apply_Variables()
        {
            trialDuration = trial.Get_Trial_Duration();
            preStimDuration = trial.Get_Pre_Stimulus_Duration();
            postStimDuration = trial.Get_Post_Stimulus_Duration();
            
            //If the list of interpolations is not empty then apply them
            //As this comes last it overrides previously set values
            //Thus, Interpolations override trial values.
            if (Interpolations.Count > 0)
            {
                Apply_Settings_Interpolations();
            }

            completionType = trial.Get_Trial_Completion_Type();
            list_of_OOI = trial.Get_List_Of_Objects_Of_Interest();
        }

        private void Apply_Settings_Interpolations()
        {
            foreach (Interpolation interpolation in Interpolations)
            {
                if (interpolation == null)
                {
                    //skips to next item in the foreach loop without ending the loop
                    continue;
                }

                //If the interpolation is for a setting
                if(interpolation.Is_Interpolating_Object() == false)
                {
                    interpRange = interpolation.Get_Range_Of_Interpolation();
                    interpValue = Get_Interpolation_Value(SequenceManager.Instance.Get_No_Of_Trials(), trial.Get_Position_In_Sequence(), interpRange.x,  interpRange.y, interpolation.Get_Interpolation_Method());

                    switch (interpolation.Get_Settings_Interpolation_Option())
                    {
                        case Settings_InterpolationOptions.Trial_Duration:
                            trialDuration = interpValue;
                            break;

                        default:
                            Debug.LogWarning("Warning: Code should not reach here");
                            break;
                    }
                }
            }
        }

        private void Position_And_Track_OOI()
        {
            bool isInList = false;

            for (int i = 0; i < list_of_OOI.Count; i++)
            {
                if (list_of_OOI[i] == null)
                {
                    continue;
                }

                //If object of interest in trial is also in the sequence interpolations list,
                //Flag it as isInList so we know not to change its position, because the sequence Interpolation should override the trial setting
                foreach (Interpolation interpolation in Interpolations)
                {
                    if(interpolation.Is_Interpolating_Object() == true)
                    {
                        if(list_of_OOI[i].Get_Object_Of_Interest_ID() == interpolation.Get_Object_To_Interpolate().Get_Object_Of_Interest_ID())
                        {
                            isInList = true;
                            break;
                        }
                    }
                }

                if(isInList == false)
                {
                    OOI_transform = OOIManager.Instance.Get_OOI_From_ID(list_of_OOI[i].Get_Object_Of_Interest_ID()).gameObject.transform;

                    if (OOI_transform != null && trial.Get_List_Of_OOI_Position_Changes()[i] != null)
                    {
                        OOI_transform.localPosition += trial.Get_List_Of_OOI_Position_Changes()[i];
                    }
                }

                //If the current trial is completable via proximity to an Object
                //and If the provided object of interest we are checking has a valid proximity value (proximity value of 0 is interpreted as do not track)
                if(trial.Get_Trial_Completion_Type() == TrialCompletionType.Duration_Or_Proximity && trial.Get_List_Of_OOI_Proximities()[i] > 0)
                {
                    OOIManager.Instance.Track_Proximity_To_OOI(list_of_OOI[i].Get_Object_Of_Interest_ID(), trial.Get_List_Of_OOI_Proximities()[i], trial.Get_List_Of_OOI_Proximity_Durations()[i]);
                }

                isInList = false;
            }
        }

        private void Apply_Object_Interpolations()
        {
            foreach (Interpolation interpolation in Interpolations)
            {
                if (interpolation == null)
                {
                    //skips to next item in the foreach loop without ending the loop
                    continue;
                }

                //If the interpolation is for an object
                if(interpolation.Is_Interpolating_Object() == true)
                {
                    ObjectOfInterest tempOOI = OOIManager.Instance.Get_OOI_From_ID(interpolation.Get_Object_To_Interpolate().Get_Object_Of_Interest_ID());

                    if(tempOOI == null)
                    {
                        Debug.LogWarning("Warning: Could not retrieve Object of Interest from OOI_Manager");

                        //skips to next item in the foreach loop without ending the loop
                        continue;
                    }

                    OOI_transform = tempOOI.gameObject.transform;

                    interpRange = interpolation.Get_Range_Of_Interpolation();
                    interpValue = Get_Interpolation_Value(SequenceManager.Instance.Get_No_Of_Trials(), trialNo, interpRange.x,  interpRange.y, interpolation.Get_Interpolation_Method());

                    //Applies the interpolation to the correct value
                    switch (interpolation.Get_Object_Interpolation_Option())
                    {
                        //case OOI_InterpolationOptions.Player_Transform:
                        //    Debug.LogWarning("Not Implemented");
                        //    break;

                        //case OOI_InterpolationOptions.Player_Position:
                        //    Debug.LogWarning("Not Implemented");
                        //    break;

                        //case OOI_InterpolationOptions.Player_Rotation:
                        //    Debug.LogWarning("Not Implemented");
                        //    break;

                        //case OOI_InterpolationOptions.Player_Scale:
                        //    Debug.LogWarning("Not Implemented");
                        //    break;

                        case OOI_InterpolationOptions.Position_X:
                            OOI_transform.localPosition += new Vector3(interpValue,0,0);
                            break;

                        case OOI_InterpolationOptions.Position_Y:
                            //If object of interest is tethered animal set Ypos through method, as flycontroller script overrides external changes to Y position.
                            if(OOI_transform == TetheredAnimalAvatarController.Instance.gameObject.transform)
                            {
                                TetheredAnimalAvatarController.Instance.Set_Y_Pos(interpValue);
                            }
                            OOI_transform.localPosition += new Vector3(0,interpValue,0);
                            break;

                        case OOI_InterpolationOptions.Position_Z:
                            OOI_transform.localPosition += new Vector3(0,0,interpValue);
                            break;

                        case OOI_InterpolationOptions.Rotation_X:
                            OOI_transform.localEulerAngles += new Vector3(interpValue,0,0);
                            break;

                        case OOI_InterpolationOptions.Rotation_Y:
                            OOI_transform.localEulerAngles += new Vector3(0,interpValue,0);
                            break;

                        case OOI_InterpolationOptions.Rotation_Z:
                            OOI_transform.localEulerAngles += new Vector3(0,0,interpValue);
                            break;

                        case OOI_InterpolationOptions.Scale_X:
                            OOI_transform.localScale += new Vector3(interpValue,0,0);
                            break;

                        case OOI_InterpolationOptions.Scale_Y:
                            OOI_transform.localScale += new Vector3(0,interpValue,0);
                            break;

                        case OOI_InterpolationOptions.Scale_Z:
                            OOI_transform.localScale += new Vector3(0,0,interpValue);
                            break;

                        case OOI_InterpolationOptions.Dynamic_OOI_Speed:
                            tempOOI.SetObjectSpeed(interpValue);
                            break;

                        default:
                            Debug.LogWarning("Warning: Code should not reach here");
                            break;
                    }
                }
            }
        }

        private void Setup_Interventions()
        {
            foreach(Intervention intervention in trial.Get_List_Of_Interventions())
            {
                if(intervention != null)
                {
                    if(intervention.Get_TriggerDelayDuration() > trialDuration)
                    {
                        Debug.LogWarning("Warning: The Intervention [" + intervention.Get_Intervention_Name() + "] is set to activate after Trial [" + trial.Get_Trial_Name() + "] has already finished");
                    }

                    InterventionsManager.Instance.Setup_Intervention(intervention);
                }
            }
        }
        #endregion

        //Called via action after Trial Scene has loaded
        private void Trial_Scene_Loaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if(waitingForScene == true && scene.name == trial.Get_Scene_Name())
            {
                waitingForScene = false;

                //If 2D stimulus is not our chosen pre stim, interupt it. (it may be set to active via the default stimulus)
                if(trial.Get_Pre_Stimulus_Type() != StimulusType.Stimulus2D)
                {
                    StimulusManager.Instance.interupt_Stimulus();
                }

                Start_Pre_Stimulus();
            }
        }

        private void Start_Pre_Stimulus()
        {
            //Initialise Scene variables after scene has loaded
            Position_And_Track_OOI();
            Apply_Object_Interpolations();
            
            //If not main scene and trial is viable
            if(UIManager.Instance.Is_Menu_Scene() == false && trial != null)
            {
                if (verbose) { print("Starting Pre Stimulus"); }
                prestimStartTimeStamp = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");
                //Activates UDP (recording)
                PreStimulusStarted.Invoke();
                StimulusManager.Instance.Activate_Stimulus(trial.Get_Pre_Stimulus_Type(), StimulusRole.Pre_Stimulus, trial.Get_Pre_Stimulus(), trial.Get_Pre_Stimulus_Color(), trial.Get_Number_Of_Pre_Stimulus_Revolutions(), preStimDuration);
                UIManager.Instance.Update_Showing("Pre Stimulus");
            }
            else
            {
                Debug.LogError("Error: Not a viable trial, You cannot use a Menu Scene as a stimulus");
            }
        }

        //Called via action after Pre Stimulus has finished
        public void Start_Trial()
        {
            End_Current_Coroutine();

            //As some interventions are based on duration they must be setup right before the trial begins
            Setup_Interventions();

            currentCoroutine = StartCoroutine(Trial_Duration());
        }

        IEnumerator Trial_Duration()
        {
            loopClosedTimeStamp = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");
            //Activates Closed Loop (DataProcessor) and OOI transform recording
            CloseLoop.Invoke();
            UIManager.Instance.Update_Showing("Trial");

            yield return new WaitForSecondsRealtime(trialDuration);
            trialCompletedBy = TrialCompletedBy.Duration;

            if(verbose) { print("Completed via Duration"); }
            
            Trial_Complete();
        }

        private void Trial_Complete()
        {   
            currentCoroutine = null;

            //Deactivates Closed Loop (DataProcessor) and OOI transform recording
            OpenLoop.Invoke();
            Start_Post_Stimulus();
        }

        private void Start_Post_Stimulus()
        {
            if (verbose) { print("Starting Post Stimulus"); }

            poststimStartTimeStamp = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");
            StimulusManager.Instance.Activate_Stimulus(trial.Get_Post_Stimulus_Type(), StimulusRole.Post_Stimulus, trial.Get_Post_Stimulus(), trial.Get_Post_Stimulus_Color(),trial.Get_Number_Of_Post_Stimulus_Revolutions(), postStimDuration);
            UIManager.Instance.Update_Showing("Post Stimulus");
        }

        //Called from Stimulus Manager
        public void Post_Stim_Complete()
        {
            poststimFinishTimeStamp = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");
            //Deactivates UDP (recording)
            PostStimulusComplete.Invoke();
            Save_Trial_Data();
        }

        public void Set_Packets_This_Trial(int packetcount)
        {
            packetsThisTrial = packetcount;
        }

        //Appends output file with data collected during the trial e.g. which OOI are in the scene and how the trial was completed.
        public void Save_Trial_Data()
        {
            if(DirectoryManager.Instance.Is_Saving_Data() == true)
            {
                if(verbose) { print("Appending Trial Data"); }

                //Append Trial Output file and add the list of OOI and completed by section
                if (File.Exists(CSV_filepath))
                {
                    using (Writer = File.AppendText(CSV_filepath))
                    {
                        Writer.WriteLine("");
                        Writer.WriteLine("ID of OOI in scene, Name, Dynamic");

                        //Append Trial Output file to include all OOI in scene, needs to be done here as scene has not been loaded when Create_Trial_Output_File() runs.
                        foreach (ObjectOfInterest OOI in OOIManager.Instance?.Get_List_Of_OOI())
                        {
                            Writer.WriteLine(OOI.GetID().ToString() + "," + OOI.gameObject.name + "," + OOI.IsDynamic().ToString());
                        }

                        Writer.WriteLine("");
                        Writer.WriteLine("Trial Completed by,Name,ID");

                        switch (trialCompletedBy) 
                        {
                            case TrialCompletedBy.Unknown:
                                Writer.WriteLine(trialCompletedBy.ToString());
                                break;

                            case TrialCompletedBy.Duration:
                                Writer.WriteLine(trialCompletedBy.ToString());
                                break;

                            case TrialCompletedBy.Proximity:
                                Writer.WriteLine(trialCompletedBy.ToString() + "," + completedByName + "," + completedByID.ToString());
                                break;

                            case TrialCompletedBy.Skip:
                                Writer.WriteLine(trialCompletedBy.ToString());
                                break;

                            case TrialCompletedBy.Stop:
                                Writer.WriteLine(trialCompletedBy.ToString());
                                break;
                        }

                        Writer.WriteLine("");
                        Writer.WriteLine("Prestim Start Timestamp, Closedloop Start Timestamp, Poststim Start Timestamp, Poststim Finish Timestamp, No of packets received");
                        Writer.WriteLine(prestimStartTimeStamp + "," + loopClosedTimeStamp + "," + poststimStartTimeStamp + "," + poststimFinishTimeStamp + "," + packetsThisTrial);

                        Writer.WriteLine("");
                        Writer.WriteLine("End of File");
                    }
                }

                if(verbose) { print("Saving DLC Data"); }

                if(UDPSocket.Instance.isLatencyTestActive() == true)
                {
                    LatencyTester.Instance?.Create_DLC_Datafile();
                }
                else
                {
                    DataProcessor.Instance?.Create_DLC_Datafile();
                }
                
                if(verbose) { print("Saving OOI transform Data"); }

                OOIManager.Instance?.Create_Datafiles_For_Tracked_Objects();
            }
        }

        private void Create_Trial_Output_File()
        {
            if(DirectoryManager.Instance.Is_Saving_Data() == false || DirectoryManager.Instance.Get_Sequence_Folder_Directory() == null)
            {
                return;
            }

            if(verbose) { print("Creating Trial Data"); }

            //CSV_filename will become equal to something in the following format 23-02-2021_9-42-18_MyProfile.Trial.csv
            string CSV_filename = DirectoryManager.Instance.Get_StartDate() + "_" + DirectoryManager.Instance.Get_Trial_StartTime() + "_" + trial.Get_Trial_Name() +".Trial.csv";
            CSV_filepath = DirectoryManager.Instance.Get_Trial_Folder_Directory() + "/" + CSV_filename;

            if (!File.Exists(CSV_filepath))
            {
                // Create a file to write to.
                using (Writer = File.CreateText(CSV_filepath))
                {
                    //Trial Settings
                    Writer.WriteLine("Trial Name, Scene Name, Position in Sequence, Completion Type, Trial Duration, Pre Stimulus Duration, Post Stimulus Duration");
                    Writer.WriteLine(trial.Get_Trial_Printout());

                    //Stimulus Values
                    Writer.WriteLine("");
                    Writer.WriteLine("Stimulus Type, Stimulus Duration, Stimulus, Stim R value, stim G value, stim B value, stim A value, number of revolutions");
                    Writer.WriteLine(trial.Get_Pre_Stimulus_Printout());
                    Writer.WriteLine(trial.Get_Post_Stimulus_Printout());


                    //Interventions
                    Writer.WriteLine("");
                    Writer.WriteLine("Intervention Name, Action, OOI ID, Position x, Position y, Position z, Rotation x , Rotation y, Rotation z, Scale x, Scale y, Scale z, Approaching OOI ID," +
                        " isTrackingTarget, Approach Speed, Approach Distance, Approach Direction x, Approach Direction y, Approach Direction z, Approach Offset x, Approach Offset y, Approach Offset z, " +
                        "Success Distance, Success Delay, Target OOI ID, Intervention Type, Number of Frames, Trigger, Trigger Delay, Proximity, Proximity Delay, isRepeatable");

                    foreach(Intervention intervention in trial.Get_List_Of_Interventions())
                    {
                        if(intervention != null)
                        {
                            Writer.WriteLine(intervention.Get_Intervention_Printout());
                        }
                    }

                    //OOI changes
                    Writer.WriteLine("");
                    Writer.WriteLine("ID of Changed OOI, Change to x position, Change to y position, Change to z position, Proximity, Proximity Duration");
                    for(int i = 0; i < trial.Get_List_Of_OOI_Position_Changes().Count; i++)
                    {
                        if(trial.Get_List_Of_OOI_Position_Changes()[i] != null)
                        {
                            Writer.WriteLine(trial.Get_OOI_Change_Printout(i));
                        }
                    }
                }	
            }
            else
            {
                Debug.LogWarning("Warning: File Already Exists");
            }
        }

        //Called via UI Button
        public void SkipTrial()
        {
            if(currentCoroutine != null)
            {
                End_Current_Coroutine();
                trialCompletedBy = TrialCompletedBy.Skip;
                Trial_Complete();
            }
        }

        //Called via UI Button
        public void StopTrial()
        {
            if(currentCoroutine != null)
            {
                End_Current_Coroutine();
                trialCompletedBy = TrialCompletedBy.Stop;
                Trial_Complete();

                #if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
                #endif

                //OpenLoop.Invoke();
                //StimulusManager.Instance.interupt_Stimulus();
                //UIManager.Instance.Previous_Menu();
            }
        }

        private float Get_Interpolation_Value(int totalSteps, int currentStep, float startValue, float FinalValue, InterpolationMethod Method)
        {
            float currentValue = default;

            if(totalSteps <= 1)
            {
                Debug.LogWarning("Warning: Not enough steps to interpolate correctly, Add more trials to sequence");
                return startValue;
            }
            
            switch (Method)
            {
                case InterpolationMethod.Linear:

                    //currentValue = starting value + ((increment per step) * currentStep)
                    //First trial uses the starting value, as increment per trial is multiplied by 0 
                    //total steps(number of trials) is minus'd by 1 to account for the starting trial never being incremented
                    currentValue = startValue + ((Mathf.Abs(FinalValue - startValue) / (totalSteps - 1)) * currentStep);
                    return currentValue;
                
                case InterpolationMethod.Log10:

                    bool isStartValueNegative = false;
                    float originalStartValue = default;

                    //Check for negative numbers
                    if(startValue < 0)
                    {
                        isStartValueNegative = true;
                        originalStartValue = Mathf.Abs(startValue);
                        startValue = 0;
                        FinalValue = FinalValue + originalStartValue;
                    }

                    //Check for Zeroes, if not zero get the Log10(value)
                    //If is zero and start or end of interpolation return the relevant value
                    //Otherwise don't get the log10 (gives error) and just use the 0 value during calculations
                    if(startValue != 0f)
                    {
                        startValue = Mathf.Log10(startValue);
                    }
                    else if (currentStep == 0f)
                    {
                        if(isStartValueNegative == true)
                        {
                            return -originalStartValue;
                        }
                        else
                        {
                            return startValue;
                        }
                    }

                    if(FinalValue != 0f)
                    {
                        FinalValue = Mathf.Log10(FinalValue);
                    }
                    else if (currentStep == totalSteps - 1)
                    {
                        return FinalValue;
                    }

                    //Log10 interpolation value = 10 ^ (currentValue as linear interpolation)
                    currentValue = Mathf.Pow(10, startValue + ((Mathf.Abs(FinalValue - startValue) / (totalSteps - 1)) * currentStep));

                    if(isStartValueNegative == true)
                    {
                        currentValue = -originalStartValue + currentValue;
                    }

                    return currentValue;

                default:
                    Debug.LogError("Error: Code should not reach here");
                    return 0;
            }
        }

        //triggered by proximity to target from the Objects_Of_Interest_Manager
        public void ProximityComplete(String name, int ID)
        {
            if (verbose) { print("Completed Via Proximity"); }

            End_Current_Coroutine();
            completedByName = name;
            completedByID = ID;
            trialCompletedBy = TrialCompletedBy.Proximity;
            Trial_Complete();
            
        }

        private void End_Current_Coroutine()
        {
            if(currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
        }

        private void OnDestroy()
        {
            End_Current_Coroutine();
            SceneManager.sceneLoaded -= Trial_Scene_Loaded;
        }
    }
}