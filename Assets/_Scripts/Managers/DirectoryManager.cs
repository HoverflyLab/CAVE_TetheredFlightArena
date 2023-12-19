//Created By Raymond Aoukar
using System.IO;
using UnityEngine;

namespace TetheredFlight
{
    //This script is responsible creating the folder structure used to store the saved files, it also makes the appropriate folders available to other scripts in the project.
    public class DirectoryManager : MonoBehaviour
    {
        //Directory Structure
        //CAVE_Data / Personalised Folder / Date / Animal Number / Sequence (settings, sequence settings) / Trial (DLC_data, Object_transforms, Trial_settings)
        //1 file is generated per sequence (sequence settings [contains profile settings]) and another 3+ are generated for each trial in that sequence (Trial_settings, DLC_data, Object_transforms).
        //Stored in these files should be everything you need to analyze the data for each trial, if you wish you should also be able to replay the trial if you have access to the scene and it has not been renamed.

        static public DirectoryManager Instance = null;

        [SerializeField,Tooltip("Automatically set to False during replay")] private bool isSavingData = true;
        [SerializeField] private bool verbose = false;
        
        #region Directories
        private string parent_Folder_Name = "CAVE_Data";
        private string personal_Folder_Directory = null;
        private string daily_Folder_Directory = null;
        private string sequence_Folder_Directory = null;
        private string trial_Folder_Directory = null;
        #endregion

        private string sequence_StartTime = null;
        private string trial_StartTime = null;
        private string startDate = null;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one Directory Manager");
                Destroy(this.gameObject);
            } 
        }    
        
        public void Initialise_Directory()
        {
            if(isSavingData)
            {
                personal_Folder_Directory = Application.dataPath + "/" + parent_Folder_Name + "/" + SettingsManager.Instance.Get_Personal_Folder_Name() + "/";

                //current date as the folder name, within the Experiment Data Folder
                startDate = System.DateTime.Now.ToString("yyyyMMdd").ToString();
                daily_Folder_Directory = personal_Folder_Directory + startDate + "/";

                //creates folder for experiment data based on the date
                if(Directory.Exists(daily_Folder_Directory))
                {
                    if(verbose) { print("Daily Directory Already Exists"); }
                }
                else
                {
                    //creates new folder based on the date if folder doesn't exist
                    if(verbose) { print("Trying to Create Daily Directory"); }
                    Directory.CreateDirectory(daily_Folder_Directory);
                }
            }
        }

        //Called via UI_Manager everytime a sequence is started
        //Creates the Animal Number Folder if provided then creates the Sequence Folder inside
        public void Create_Sequence_Folder(string sequenceName)
        {
            if(isSavingData)
            {
                sequence_StartTime = System.DateTime.Now.ToString("Hmmss").ToString(); 

                if(UIManager.Instance.Get_Animal_Folder_Name().Equals(""))
                {
                    sequence_Folder_Directory = daily_Folder_Directory + "/" + "Unlabeled Animal" + "/" + sequence_StartTime + "_" + sequenceName;
                }
                else
                {
                    sequence_Folder_Directory = daily_Folder_Directory + "/" + UIManager.Instance.Get_Animal_Folder_Name() + "/" + sequence_StartTime + "_" + sequenceName;
                }

                //creates new folder based on the date if folder doesn't exist
                if(verbose) { print("Trying to Create Sequence Folder"); }     
                Directory.CreateDirectory(sequence_Folder_Directory);
            }
        }

        public void Create_Trial_Folder(string trialName)
        {
            if(isSavingData)
            {
                trial_StartTime = System.DateTime.Now.ToString("Hmmss").ToString();
                trial_Folder_Directory = sequence_Folder_Directory + "/" + trial_StartTime + "_" + trialName;
                if(verbose) { print("Trying to Create Trial Folder"); }     
                Directory.CreateDirectory(trial_Folder_Directory);
            }
        }

        #region Getters
        public string Get_Daily_Folder_Directory()
        {
            if(daily_Folder_Directory == null)
            {
                Debug.LogError("Error: daily_Folder_Directory is null (it does not exist so data can't be saved inside it)");
                return null;
            }

            return daily_Folder_Directory;
        }

        public string Get_Sequence_Folder_Directory()
        {
            if(sequence_Folder_Directory == null)
            {
                Debug.LogError("Error: Sequence Directory is null (it does not exist so data can't be saved inside it)");
                return null;
            }

            return sequence_Folder_Directory;
        }

        public string Get_Sequence_StartTime()
        {
            if(sequence_StartTime == null)
            {
                Debug.LogError("Error: sequence_StartTime is null (it does not exist and can't be used when naming folders and files");
                return null;
            }

            return sequence_StartTime;
        }

        public string Get_Trial_Folder_Directory()
        {
            if(trial_Folder_Directory == null)
            {
                Debug.LogError("Error: Trial Directory is null (it does not exist so data can't be saved inside it");
                return null;
            }

            return trial_Folder_Directory;
        }

        public string Get_Trial_StartTime()
        {
            if(trial_StartTime == null)
            {
                Debug.LogError("Error: trial_StartTime is null (it does not exist and can't be used when naming folders and files");
            }

            return trial_StartTime;
        }
        
        public string Get_StartDate()
        {
            return startDate;
        }
        #endregion

        public bool Is_Saving_Data()
        {
            return isSavingData;
        }

        public void Set_Saving_Data(bool issavingdata)
        {
            isSavingData = issavingdata;
        }
    }
}



    

