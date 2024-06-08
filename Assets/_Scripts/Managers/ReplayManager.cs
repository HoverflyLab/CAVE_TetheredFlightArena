//Created By Raymond Aoukar 15/09/2021
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEditor;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This script is responsible for loading previously saved files and allowing the data to be used for replay.
    public class ReplayManager : MonoBehaviour
    {
        static public ReplayManager Instance = null;

        [SerializeField] private bool verbose = false;
        [SerializeField, ReadOnly] private bool isEndOfReplay = false;

        private FileStream fileStream = null;
        public StreamReader reader = null;

        //These scriptables act as containers, the saved data is then loaded into them
        #region Blank Scriptables
        private SettingsProfile replaySettings = null;
        private Sequence replaySequence = null;
        private Trial replayTrial = null;
        #endregion

        private int lineNumber = 0;
        private int numberOfScreens = 0;
        private string[] tempValues = default;

        private string trialFolderPath = default;
        private string trialFilePath = default;

        private string sequenceFolderPath = default;
        private string sequenceFilePath = default;
        private int ColNo = 0; //ColumnNo

        //Stores Directory Manager value, which is then used to reset to the originl value if replay is disabled. 
        private bool IsSavingData;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one Replay Manager");
                Destroy(this.gameObject);
            }

            SceneManager.sceneLoaded += ReplayScene_Loaded;
        }

        public void Activate_Replay_Manager(bool setActive = true)
        {
            this.enabled = setActive;
        }

        private void Disable_Replay_Manager()
        {
            DirectoryManager.Instance.Set_Saving_Data(IsSavingData);
            UIManager.Instance.Previous_Menu();
            this.enabled = false;
        }

        //Equivalent to Start
        private void InitialiseScriptables()
        {
            IsSavingData = DirectoryManager.Instance.Is_Saving_Data();
            DirectoryManager.Instance.Set_Saving_Data(false);

            replaySettings = ScriptableObject.CreateInstance("SettingsProfile") as SettingsProfile;
            replaySequence = ScriptableObject.CreateInstance("Sequence") as Sequence;
            replayTrial = ScriptableObject.CreateInstance("Trial") as Trial;
        }

        public void LocateTrialFolder()
        {
            //TODO: Reimplement our folder selection method as the EditorUtility functions don't work in build.
            //Likely best to just use a free package from the unity store for this
#if !UNITY_EDITOR
            return;
#endif

#pragma warning disable CS0162
#if UNITY_EDITOR
            trialFolderPath = EditorUtility.OpenFolderPanel("Select the Trial Folder you wish to replay"," ", " ");
#endif

            if(trialFolderPath != string.Empty)
            {
                InitialiseScriptables();
                Load_Replay_Files(trialFolderPath);
            }
            else
            {
                Debug.LogWarning("Warning: No filepath was provided");
                Disable_Replay_Manager();
            }
            #pragma warning restore CS0162
        }

        private void Load_Replay_Files(string trialFolderPath)
        {
            Find_and_Load_Sequence();

            Find_and_Load_Trial();

            if(Application.CanStreamedLevelBeLoaded(replayTrial.Get_Scene_Name()))
            {
                SceneManager.LoadScene(replayTrial.Get_Scene_Name());
            }
            else
            {
                Debug.LogError("Error: Scene provided by file cannot be loaded (check that the scene has not been renamed and that it has been added to the build settings)");
                Disable_Replay_Manager();
            }
        }

        private void Find_and_Load_Sequence()
        {
            //As the trial folder is within the sequence folder, we go one folder back in the path to get to the sequence folder
            sequenceFolderPath = trialFolderPath.Remove(trialFolderPath.LastIndexOf("/"));

            //Checks to make sure atleast 1 Sequence file is found
            if(Directory.GetFiles(sequenceFolderPath, "*.Sequence.csv").GetLength(0) > 0)
            {
                //Returns the first file that ends in .Sequence.csv from the sequenceFolderPath
                sequenceFilePath = Directory.GetFiles(sequenceFolderPath, "*.Sequence.csv")[0];
            }
            else
            {
                Debug.LogError("Error: No Sequence File Found");
                Disable_Replay_Manager();
            }
             
            fileStream = new FileStream(sequenceFilePath, FileMode.Open, FileAccess.ReadWrite);
            reader = new StreamReader(fileStream);

            lineNumber = 0;
            numberOfScreens = 0;
            
            while(reader.EndOfStream == false)
            {
                tempValues = reader.ReadLine().Split(',');

                //Profile settings
                //Using lineNumber and tempValues length is a crude and error prone way of retrieving the data
                if(lineNumber == 1)
                {
                    ColNo = 0;
                    replaySettings.Set_Profile_Name(tempValues[ColNo++] + "_replay");
                    replaySettings.Set_Personal_Folder_Name(tempValues[ColNo++]);
                    replaySettings.Set_Distance_From_Monitors(float.Parse(tempValues[ColNo++]));
                    replaySettings.Set_Number_Of_Monitors(int.Parse(tempValues[ColNo++]));
                    numberOfScreens = int.Parse(tempValues[ColNo-1]);

                    replaySettings.Set_Frame_Rate(int.Parse(tempValues[ColNo++]));

                    //Parse resolutions one at a time, depending on the number of resolutions subsequent values will be found in different locations.
                    //resolutions are expected in the following format 1920:1080,1920:1080,1920:1080,1920:1080
                    List<Vector2Int> list_Of_Resolutions = new List<Vector2Int>();

                    for (int i = 0; i < numberOfScreens; i++)
                    {
                        string[] resolution;

                        if (tempValues[ColNo + i].Contains(":") == true)
                        {
                            resolution = tempValues[ColNo + i].Split(':');
                            list_Of_Resolutions.Add(new Vector2Int(int.Parse(resolution[0]), int.Parse(resolution[1])));
                        }
                        else
                        {
                           Debug.LogError("Error: Field did not contain a colon and is likely not a resolution");
                        }
                    }

                    var videoResTemp = tempValues[ColNo++ + numberOfScreens].Split(":");
                    replaySettings.Set_Video_Resolution(new Vector2Int(int.Parse(videoResTemp[0]),int.Parse(videoResTemp[1])));
                    
                    //As all OOI movement during replay is determined by xyz location and rotation 
                    //stored in the OOI data files, there may be no reason to load this data.
                    replaySettings.Set_Yaw_Method((YawMethod)Enum.Parse(typeof(YawMethod), tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Yaw_DPS(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Min_Yaw(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Yaw_At_Midpoint(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Max_Yaw(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Min_WBAD(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Midpoint_WBAD(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Max_WBAD(float.Parse(tempValues[ColNo++ + numberOfScreens]));

                    replaySettings.Set_M_Bottom(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_M_Top(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_M_LoglC50(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_M_Hillside(float.Parse(tempValues[ColNo++ + numberOfScreens]));

                    replaySettings.Set_F_Bottom(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_F_Top(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_F_LoglC50(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_F_Hillside(float.Parse(tempValues[ColNo++ + numberOfScreens]));

                    replaySettings.Set_Thrust_Method((ThrustMethod)Enum.Parse(typeof(ThrustMethod), tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Thrust_MPS(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Min_Thrust(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Thrust_At_Midpoint(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Max_Thrust(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Min_WBAS(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Midpoint_WBAS(float.Parse(tempValues[ColNo++ + numberOfScreens]));
                    replaySettings.Set_Max_WBAS(float.Parse(tempValues[ColNo++ + numberOfScreens]));

                    SettingsManager.Instance.Set_New_Profile(replaySettings);
                }

                //Longitudinal Axis Points
                else if(lineNumber == 4)
                {
                    //If longitudinal Axis was manually setup
                    if(bool.Parse(tempValues[0]) == true)
                    {
                        UIManager.Instance.Set_isLongitudinalAxisCalc_Manual(true);
                        DataProcessor.Instance.Set_LongitudinalAxis_Upper_Points(new Vector2(float.Parse(tempValues[1]),float.Parse(tempValues[2])));
                        DataProcessor.Instance.Set_LongitudinalAxis_Lower_Points(new Vector2(float.Parse(tempValues[3]),float.Parse(tempValues[4])));
                    }
                    else
                    {
                        //During replay OOI are controlled by location and rotation data stored in their .transform files
                        //As such no longitudinal axis data needs to be loaded.
                        UIManager.Instance.Set_isLongitudinalAxisCalc_Manual(false);
                    }
                }

                //Sequence settings
                else if(lineNumber == 7)
                {
                    replaySequence.Set_Sequence_Name(tempValues[0] + "_replay");
                    replaySequence.Set_Number_Of_Trials(int.Parse(tempValues[1]));
                }

                //Default Stim settings
                else if(lineNumber == 10)
                {
                    DefaultStimulus defaultStim = ScriptableObject.CreateInstance("DefaultStimulus") as DefaultStimulus;

                    defaultStim.Set_Stimulus_Name(tempValues[0] + "_replay");
                    defaultStim.Set_Stimulus_Type((StimulusType)Enum.Parse(typeof(StimulusType), tempValues[1]));
                    defaultStim.Set_Stimulus((Stimulus)Enum.Parse(typeof(Stimulus), tempValues[2]));

                    Color32 tempColor = new Color32(byte.Parse(tempValues[3]),byte.Parse(tempValues[4]),byte.Parse(tempValues[5]),byte.Parse(tempValues[6]));
                    defaultStim.Set_Stimulus_Color(tempColor);

                    defaultStim.Set_Scene_Name(tempValues[7]);
                    defaultStim.Set_Seconds_Per_Revolution(float.Parse(tempValues[8]));

                    replaySequence.Set_Default_Stimulus(defaultStim);
                }

                //Possible Interpolations
                else if(lineNumber >= 13 && tempValues.GetLength(0) == 8)
                {
                    Interpolation interpolation = ScriptableObject.CreateInstance("Interpolation") as Interpolation;
                    
                    interpolation.Set_Interpolation_Name(tempValues[0] + "_replay");
                    interpolation.Set_Is_Interpolation_Object(bool.Parse(tempValues[1]));
                    
                    //Create and add the Object Of Interest scriptable to the Interpolation
                    Object_Of_Interest objectofinterest = ScriptableObject.CreateInstance("Object_Of_Interest") as Object_Of_Interest;
                    objectofinterest.Set_Object_Of_Interest_Name("OOI_replay");
                    objectofinterest.Set_Object_Of_Interest_ID(int.Parse(tempValues[2]));
                    interpolation.Set_Object_To_Interpolate(objectofinterest);

                    interpolation.Set_Object_Interpolation_Option((OOI_InterpolationOptions)Enum.Parse(typeof(OOI_InterpolationOptions), tempValues[3]));
                    interpolation.Set_Settings_Interpolation_Option((Settings_InterpolationOptions)Enum.Parse(typeof(Settings_InterpolationOptions), tempValues[4]));
                    interpolation.Set_Range_Of_Interpolation(new Vector2(float.Parse(tempValues[5]),float.Parse(tempValues[6])));
                    interpolation.Set_Interpolation_Method((InterpolationMethod)Enum.Parse(typeof(InterpolationMethod), tempValues[7]));

                    replaySequence.Add_New_Interpolation(interpolation);
                }

                lineNumber++;
            }
        }

        private void Find_and_Load_Trial()
        {
            //Checks to make sure atleast 1 trial file is found
            if(Directory.GetFiles(trialFolderPath, "*.Trial.csv").GetLength(0) > 0)
            {
                //Returns the first file that ends in .Sequence.csv from the trialFolderPath
                trialFilePath = Directory.GetFiles(trialFolderPath, "*.Trial.csv")[0];
            }
            else
            {
                Debug.LogError("Error: No Trial File Found");
                UIManager.Instance.Previous_Menu();
                this.enabled = false;
            }
             
            fileStream = new FileStream(trialFilePath, FileMode.Open, FileAccess.ReadWrite);
            reader = new StreamReader(fileStream);

            lineNumber = 0;
            ColNo = 0;

            while(reader.EndOfStream == false)
            {
                tempValues = reader.ReadLine().Split(',');

                //Trial settings
                //Using lineNumber and tempValues length is a crude and error prone way of retrieving the data
                if(lineNumber == 1)
                {
                    ColNo = 0;
                    replayTrial.Set_Trial_Name(tempValues[ColNo++] + "_replay");
                    replayTrial.Set_Scene_Name(tempValues[ColNo++]);
                    replayTrial.Set_Position_In_Sequence(int.Parse(tempValues[ColNo++]));
                    replayTrial.Set_Trial_Completion_Type((TrialCompletionType)Enum.Parse(typeof(TrialCompletionType), tempValues[ColNo++]));
                    replayTrial.Set_Trial_Duration(float.Parse(tempValues[ColNo++]));
                    replayTrial.Set_Pre_Stimulus_Duration(float.Parse(tempValues[ColNo++]));
                    replayTrial.Set_Post_Stimulus_Duration(float.Parse(tempValues[ColNo++]));
                }

                //Pre stim settings
                else if(lineNumber == 4)
                {
                    replayTrial.Set_Pre_Stimulus_Type((StimulusType)Enum.Parse(typeof(StimulusType), tempValues[0]));
                    replayTrial.Set_Pre_Stimulus_Duration(float.Parse(tempValues[1]));
                    replayTrial.Set_Pre_Stimulus((Stimulus)Enum.Parse(typeof(Stimulus), tempValues[2]));

                    Color32 tempColor = new Color32(byte.Parse(tempValues[3]),byte.Parse(tempValues[4]),byte.Parse(tempValues[5]),byte.Parse(tempValues[6]));
                    replayTrial.Set_Pre_Stimulus_Color(tempColor);

                    replayTrial.Set_Number_Of_Pre_Stimulus_Revolutions(int.Parse(tempValues[7]));
                }

                //Post stim settings
                else if(lineNumber == 5)
                {
                    replayTrial.Set_Post_Stimulus_Type((StimulusType)Enum.Parse(typeof(StimulusType), tempValues[0]));
                    replayTrial.Set_Post_Stimulus_Duration(float.Parse(tempValues[1]));
                    replayTrial.Set_Post_Stimulus((Stimulus)Enum.Parse(typeof(Stimulus), tempValues[2]));

                    Color32 tempColor = new Color32(byte.Parse(tempValues[3]),byte.Parse(tempValues[4]),byte.Parse(tempValues[5]),byte.Parse(tempValues[6]));
                    replayTrial.Set_Post_Stimulus_Color(tempColor);

                    replayTrial.Set_Number_Of_Post_Stimulus_Revolutions(int.Parse(tempValues[7]));
                }

                //Intervention
                else if(lineNumber >= 8 && tempValues.GetLength(0) == 32) //Update once approaching values have been added
                {
                    ColNo = 0;
                    Intervention intervention = ScriptableObject.CreateInstance("Intervention") as Intervention;

                    intervention.Set_Intervention_Name(tempValues[ColNo++] + "_replay");
                    intervention.Set_Action((InterventionActions)Enum.Parse(typeof(InterventionActions), tempValues[ColNo++]));

                    //Create and add the Object Of Interest scriptable to the Intervention
                    Object_Of_Interest objectofinterest = ScriptableObject.CreateInstance("Object_Of_Interest") as Object_Of_Interest;
                    objectofinterest.Set_Object_Of_Interest_Name("OOI_replay");
                    objectofinterest.Set_Object_Of_Interest_ID(int.Parse(tempValues[ColNo++]));
                    intervention.Set_TargetOOI(objectofinterest);

                    intervention.Set_Position(new Vector3(float.Parse(tempValues[ColNo++]),float.Parse(tempValues[ColNo++]),float.Parse(tempValues[ColNo++])));
                    intervention.Set_Rotation(new Vector3(float.Parse(tempValues[ColNo++]),float.Parse(tempValues[ColNo++]),float.Parse(tempValues[ColNo++])));
                    intervention.Set_Scale(new Vector3(float.Parse(tempValues[ColNo++]),float.Parse(tempValues[ColNo++]),float.Parse(tempValues[ColNo++])));

                    Object_Of_Interest approachingOOI = ScriptableObject.CreateInstance("Object_Of_Interest") as Object_Of_Interest;
                    approachingOOI.Set_Object_Of_Interest_Name("ApproachingOOI_replay");
                    approachingOOI.Set_Object_Of_Interest_ID(int.Parse(tempValues[ColNo++]));
                    intervention.Set_ApproachingOOI(approachingOOI);

                    intervention.Set_isTrackingTarget(bool.Parse(tempValues[ColNo++]));
                    intervention.Set_ApproachSpeed(float.Parse(tempValues[ColNo++]));
                    intervention.Set_ApproachDistance(float.Parse(tempValues[ColNo++]));
                    intervention.Set_ApproachDirection(new Vector3(float.Parse(tempValues[ColNo++]), float.Parse(tempValues[ColNo++]), float.Parse(tempValues[ColNo++])));
                    intervention.Set_ApproachOffset(new Vector3(float.Parse(tempValues[ColNo++]), float.Parse(tempValues[ColNo++]), float.Parse(tempValues[ColNo++])));
                    intervention.Set_ApproachProximity(float.Parse(tempValues[ColNo++]));
                    intervention.Set_ApproachProximityDuration(float.Parse(tempValues[ColNo++]));

                    //Create and add the Object Of Interest scriptable to the Intervention
                    Object_Of_Interest targetOOI = ScriptableObject.CreateInstance("Object_Of_Interest") as Object_Of_Interest;
                    targetOOI.Set_Object_Of_Interest_Name("OOI_replay");
                    targetOOI.Set_Object_Of_Interest_ID(int.Parse(tempValues[ColNo++]));
                    intervention.Set_TriggerOOI(targetOOI);

                    intervention.Set_InterventionType((InterventionType)Enum.Parse(typeof(InterventionType), tempValues[ColNo++]));
                    intervention.Set_NumberOfFrames(int.Parse(tempValues[ColNo++]));
                    intervention.Set_Trigger((InterventionTriggers)Enum.Parse(typeof(InterventionTriggers), tempValues[ColNo++]));
                    intervention.Set_TriggerDelayDuration(float.Parse(tempValues[ColNo++]));
                    intervention.Set_TriggerProximity(float.Parse(tempValues[ColNo++]));
                    intervention.Set_TriggerProximityDuration(float.Parse(tempValues[ColNo++]));
                    intervention.Set_isRepeatable(bool.Parse(tempValues[ColNo++]));
                    
                    replayTrial.Add_Intervention_To_List_Of_Interventions(intervention);
                }

                //OOI changes
                else if(lineNumber >= 11 && tempValues.GetLength(0) == 6)
                {
                    if(tempValues[0] == "ID of Changed OOI")
                    {
                        return;
                    }
                    
                    //Create and add the Object Of Interest scriptable to the trial
                    Object_Of_Interest objectofinterest = ScriptableObject.CreateInstance("Object_Of_Interest") as Object_Of_Interest;

                    objectofinterest.Set_Object_Of_Interest_Name("OOI_replay");
                    objectofinterest.Set_Object_Of_Interest_ID(int.Parse(tempValues[0]));
                    replayTrial.Add_OOI_To_List_Of_Object_Of_Interest(objectofinterest);

                    replayTrial.Add_Position_To_List_Of_OOI_Position_Changes(new Vector3(float.Parse(tempValues[1]),float.Parse(tempValues[2]),float.Parse(tempValues[3])));
                    replayTrial.Add_Proximity_To_List_Of_OOI_Proximities(float.Parse(tempValues[4]));
                    replayTrial.Add_Proximity_Duration_To_List_Of_OOI_Proximity_Durations(float.Parse(tempValues[5]));
                }

                lineNumber++;
            }

            replaySequence.Add_Replay_Trial(replayTrial);
        }

        //Triggered via the action sceneLoaded
        private void ReplayScene_Loaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if(this.enabled == true)
            {
                SequenceManager.Instance.Load_Replay_Sequence(replaySequence);
                Load_Transform_Files();
            }
        }

        //Gets the file for each Object of Interest within the scene, and passes it to them using their Unique ID.
        private void Load_Transform_Files()
        {
            string tempFilePath = default;

            if(verbose) { print("Loading replay data"); }

            if(OOIManager.Instance.Get_List_Of_OOI().Count == 0)
            {
                Debug.LogError("Error: Not a valid Trial, no Objects of interest could be found");
                Disable_Replay_Manager();
                return;
            }

            foreach(ObjectOfInterest obj in OOIManager.Instance.Get_List_Of_OOI())
            {
                string concat = "*" + obj.GetID() + ".Transform.csv";

                //Checks to make sure atleast 1 transform file is found
                if(Directory.GetFiles(trialFolderPath, concat).GetLength(0) > 0)
                {
                    //Returns the first file that ends in (OOI_ID).Transform.csv
                    tempFilePath = Directory.GetFiles(trialFolderPath, concat)[0];
                    obj.GetReplayData(tempFilePath); 
                    if( verbose ) { print("Object with ID - " + obj.GetID() + " received - " + tempFilePath); }
                }
                else
                {
                    Debug.LogError("Error: No file found for OOI with ID - " + obj.GetID());
                }
            }
        }

        //Triggered Via UI Button
        public void Start_Replay()
        {
            foreach(ObjectOfInterest obj in OOIManager.Instance.Get_List_Of_OOI())
            {
                obj.BeginReplay();
            }
        }

        public bool Get_Is_End_Of_Replay()
        {
            return isEndOfReplay;
        }

        public void Set_is_End_Of_Replay(bool val)
        {
            print("End of Replay");
            isEndOfReplay = val;
            UIManager.Instance.Previous_Menu();
        }
    }
}
