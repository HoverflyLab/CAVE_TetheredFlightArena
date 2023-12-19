//Created By Raymond Aoukar
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System;
using System.IO;

namespace TetheredFlight
{
    //This script is reponsible for loading the sequence and passing the next trial onto the trial manager.
    //It also determines what happens at the end of a sequence and when all sequences have been completed.
    public class SequenceManager : MonoBehaviour
    {
        public static SequenceManager Instance = null;

        [SerializeField, Expandable] private List<Sequence> list_Of_Sequences = new List<Sequence>();
        [SerializeField, Tooltip("Prints additional debug logs if true.")] private bool verbose = false;

        private Sequence currentSequence = null;
        private Trial currentTrial = null;
        private int sequenceNo = 0;
        private int trialNo = 0;

        private DefaultStimulus defaultStimulus;
        private StreamWriter Writer = null;
        public Action endOfExperiment;

        private string sequenceStartTimeStamp = "";
        private string sequenceFinishTimeStamp = "";

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one Sequence Manager");
                Destroy(this.gameObject);
            }
        }

        public void Start_First_Sequence()
        {
            if(sequenceNo == 0)
            {
                currentSequence = null;

                //Check to see if we have a sequence in the list
                if(list_Of_Sequences.Count > 0)
                {
                    currentSequence = list_Of_Sequences[sequenceNo];
                    DirectoryManager.Instance.Create_Sequence_Folder(currentSequence.Get_Sequence_Name());
                    Start_Sequence(currentSequence);
                }
                else
                {
                    Debug.LogError("Error: No Sequences in list");
                }
            }
            else
            {
                Next_Sequence();
            }
        }

        private void Start_Sequence(Sequence sequence)
        {
            sequenceStartTimeStamp = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");

            Create_Sequence_Output_File();

            trialNo = 0;
            
            //Check to see if our sequence has a trial
            if(currentSequence.Get_Number_Of_Trials() > 0)
            {
                currentTrial = currentSequence.Get_List_Of_Trials()[trialNo];
                
                if (verbose) { print("Starting Sequence - " + currentSequence.Get_Sequence_Name()); }

                DirectoryManager.Instance.Create_Trial_Folder(currentTrial.Get_Trial_Name());
                TrialManager.Instance.InitialiseTrial(currentTrial);
            }
            else
            {
                Debug.LogError("Error: " + currentSequence.name + " - Sequence had no trials");
                Sequence_Complete();
            }
        }

        public void Next_Trial()
        {
            trialNo++;

            //if next trial exists
            if(currentSequence.Get_Number_Of_Trials() > trialNo)
            {
                currentTrial = currentSequence.Get_List_Of_Trials()[trialNo];
                DirectoryManager.Instance.Create_Trial_Folder(currentTrial.Get_Trial_Name());
                TrialManager.Instance.InitialiseTrial(currentTrial);
            }
            else
            {
                Sequence_Complete();
            }
        } 

        private void Sequence_Complete()
        {
            sequenceNo++;

            sequenceFinishTimeStamp = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");

            Create_Sequence_Output_File();

            //If next sequence exists
            if(list_Of_Sequences.Count > sequenceNo)
            {
                print("Sequence Complete");
                StimulusManager.Instance.Activate_Default_Stimulus();
                UIManager.Instance.Open_Next_Sequence_Menu();
            }
            else
            {
                //end experiment
                print("End of Experiment");

                //Play last sequence's default stimulus
                sequenceNo--;
                StimulusManager.Instance.Activate_Default_Stimulus();

                //Takes you back to the menu
                // SceneManager.LoadScene("Menu");
                // UI_Manager.Instance.Previous_Menu();
            
                //Ends the play session
                //EditorApplication.ExecuteMenuItem("Edit/Play");
            }
        }

        public void Next_Sequence()
        {
            //If next sequence exists
            if(list_Of_Sequences.Count > sequenceNo)
            {
                currentSequence = list_Of_Sequences[sequenceNo];
                DirectoryManager.Instance.Create_Sequence_Folder(currentSequence.Get_Sequence_Name());
                Start_Sequence(currentSequence);
            }
            else
            {
                Debug.LogError("Error: The next Sequence does not exist");
            }
        }

        //Called on Sequence Start and on Sequence Complete (on sequence complete overrides previous and records finish time)
        private void Create_Sequence_Output_File()
        {

            if(DirectoryManager.Instance.Is_Saving_Data() == false || DirectoryManager.Instance.Get_Sequence_Folder_Directory() == null)
            {
                return;
            }

            //CSV_filename will become equal to something in the following format 23-02-2021_9-42-18_MyProfile.Sequence.csv
            string CSV_filename = DirectoryManager.Instance.Get_StartDate() + "_" + DirectoryManager.Instance.Get_Sequence_StartTime() + "_" + currentSequence.Get_Sequence_Name() +".Sequence.csv";
            string CSV_filepath = DirectoryManager.Instance.Get_Sequence_Folder_Directory() + "/" + CSV_filename;

            // Create a file to write to.
            using (Writer = File.CreateText(CSV_filepath))
            {
                string screenNames = "Display 1 Resolution";
                for(int i = 1; i < SettingsManager.Instance.Get_List_Of_Monitor_Resolutions().Count; i++)
                {
                    screenNames = screenNames + ",Display " + (i+1) + " Resolution";
                }

                //Profile Settings
                Writer.WriteLine("Profile Name, Personal Folder Name, Distance From Screen, Number of Screens, Desired Frame Rate, " + screenNames + ", Video Resolution, Yaw Method, Yaw Gain, Min Yaw, Yaw at Midpoint, Max Yaw, Min WBAD, WBAD Midpoint, Max WBAD" + 
                ", M_Bottom, M_Top, M_LogLC50, M_Hillside, F_Bottom, F_Top, F_LogLC50, F_Hillside, Thrust Method, Thrust Gain, Min Thrust, Thrust at Midpoint, Max Thrust, Min WBAS, WBAS Midpoint, Max WBAS");
                Writer.WriteLine(SettingsManager.Instance.Get_Profile_Printout());
                

                //Longitudinal Axis
                Writer.WriteLine("");
                Writer.WriteLine("isManualAxis, Upper point X, Upper point Y, Lower point X, Lower point Y");
                Writer.WriteLine(UIManager.Instance.Get_isLongitudinalAxisCalc_Manual().ToString() + ", " +  DataProcessor.Instance.Get_LongitudinalAxisPoints());

                //Sequence Settings
                Writer.WriteLine("");
                Writer.WriteLine("Sequence Name, Number of Trials, Start Timestamp, Finish Timestamp");
                Writer.WriteLine(currentSequence.Get_Sequence_Printout() + "," + sequenceStartTimeStamp + "," + sequenceFinishTimeStamp);

                //Default Stim
                Writer.WriteLine("");
                Writer.WriteLine("Default Stim name, Default Stim type, Stimulus, Default Stim R value, Default Stim G value, Default Stim B value, Default Stim A value, Scene Name, Seconds per revolution");
                Writer.WriteLine(currentSequence.Get_Default_Stimulus().Get_Stimulus_Printout());

                //Interventions
                Writer.WriteLine("");
                Writer.WriteLine("Interpolation Name, Interpolating Object, OOI ID, Object Interpolation Option, Setting Interpolation Option, Start Value, Final Value , Interpolation method");
                foreach(Interpolation interpolation in currentSequence.Get_List_Of_Interpolations())
                {
                    if(interpolation != null)
                    {
                        Writer.WriteLine(interpolation.Get_Interpolation_Printout());
                    }
                }

                Writer.WriteLine("");
                Writer.WriteLine("End of File");
            }	
        }

        public List<Interpolation> Get_Current_Sequence_Interpolations() { return currentSequence.Get_List_Of_Interpolations(); }

        public int Get_Sequence_No() { return sequenceNo + 1; }

        public int Get_No_of_Sequences() { return list_Of_Sequences.Count; }

        public int Get_Trial_No() { return trialNo; }

        //Number of trials in the current sequence
        public int Get_No_Of_Trials() { return currentSequence.Get_Number_Of_Trials(); }

        public Trial Get_Next_Trial() 
        { 
            if(trialNo+1 < Get_No_Of_Trials()) 
            { 
                return currentSequence.Get_List_Of_Trials()[trialNo+1]; 
            } 
            else 
            { 
                return null; 
            } 
        }

        public DefaultStimulus Get_Default_Stimulus()
        {
            return list_Of_Sequences[sequenceNo].Get_Default_Stimulus();
        }

        public void Load_Replay_Sequence(Sequence newSequence)
        {
            list_Of_Sequences.Clear();
            list_Of_Sequences.Add(newSequence);
        }

        public bool Is_Next_Trial_Pre_Stimulus_2D()
        {
            if(Get_Next_Trial() != null)
            {
                if(Get_Next_Trial().Get_Pre_Stimulus_Type() == StimulusType.Stimulus2D)
                {
                    return true;
                }
            }
            return false;
        }

        //When we are not in replay, accurately set the number of trials in each sequence.
        //When in replay number of trials will always be one if this code is run.
        //Thus, we only call it when we are not in replay.
        public void Reset_Number_Of_Trials()
        {
            foreach(Sequence sequence in list_Of_Sequences)
            {
                sequence.Set_Number_Of_Trials(sequence.Get_List_Of_Trials().Count);
            }
        }
    }
}
