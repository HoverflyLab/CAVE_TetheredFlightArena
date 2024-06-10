//Created By Raymond Aoukar
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //These values are never used, this will be necessary to implement the Sigmoid method of Thrust and Yaw calculation.
    public struct Sigmoid_Values
    {
        public float bottom;
        public float top;
        public float loglC50;
        public float Hillslope;
    }

    //This Script applies the users settings to the project
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance = null;
        
        [SerializeField, Expandable] private SettingsProfile settingsProfile = null;
        [SerializeField, Tooltip("Prints additional debug logs if true.")] private bool verbose = false;

        private Sigmoid_Values male_Yaw_SigmoidValues;
        private Sigmoid_Values female_Yaw_SigmoidValues;
        private int updateEveryX = 10;
        private int count = 0;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one Settings Manager");
                Destroy(this.gameObject);
            } 

            if(settingsProfile != null)
            {
                Initialise_Settings();
            }
            else
            {
                Debug.LogError("Error: No profile settings has been assigned");
            }
        }

        private void Initialise_Settings() 
        {
            Application.targetFrameRate = settingsProfile.Get_Target_Frame_Rate();
            QualitySettings.vSyncCount = 0;

            //sets all FixedUpdate methods to run x times per second as specified by the framerate variable.
            Time.fixedDeltaTime = 1f / settingsProfile.Get_Target_Frame_Rate();
            
            //sets all Update methods to run x times per second as specified by the framerate variable.
            //If you enable FrameLimiter in the settings_manager inspector window it will be used instead of the Unity inbuilt frame limiter.
            //The Unity inbuilt seems to perform better.
            if(FrameLimiter.Instance != null && FrameLimiter.Instance.enabled == true)
            {
                if(verbose) { print("Frame Limiter Active"); }
                FrameLimiter.Instance.UpdateFPSLimit(settingsProfile.Get_Target_Frame_Rate());
            }
            else
            {
                if(verbose) { print("Application targeFrameRate Active"); }
                Application.targetFrameRate = settingsProfile.Get_Target_Frame_Rate();
            }

            male_Yaw_SigmoidValues = new Sigmoid_Values();
            female_Yaw_SigmoidValues = new Sigmoid_Values();

            male_Yaw_SigmoidValues.bottom = settingsProfile.Get_M_Bottom();
            male_Yaw_SigmoidValues.top = settingsProfile.Get_M_Top();
            male_Yaw_SigmoidValues.loglC50 = settingsProfile.Get_M_LoglC50();
            male_Yaw_SigmoidValues.Hillslope = settingsProfile.Get_M_Hillside();

            female_Yaw_SigmoidValues.bottom = settingsProfile.Get_F_Bottom();
            female_Yaw_SigmoidValues.top = settingsProfile.Get_F_Top();
            female_Yaw_SigmoidValues.loglC50 = settingsProfile.Get_F_LoglC50();
            female_Yaw_SigmoidValues.Hillslope = settingsProfile.Get_F_Hillside();
        }

        //Check how stable FPS is via log
        //private void Update()
        //{
        //    if(count == updateEveryX)
        //    {
        //        Debug.LogError($"Current fps - {1 / Time.deltaTime}");
        //        count = 0;
        //    }
        //    else
        //    {
        //        count++;
        //    }
        //}

        #region Getters
        public string Get_Profile_Name()
        {
            return settingsProfile.Get_Profile_Name();
        }

        public string Get_Personal_Folder_Name()
        {
            return settingsProfile.Get_Personal_Folder_Name();
        }

        public Vector2Int Get_Video_Resolution()
        {
            return settingsProfile.Get_Video_Resolution();
        }

        public float Get_Yaw_DPS()
        {
            return settingsProfile.Get_Yaw_DPS();
        }

        public float Get_Thrust_MPS()
        {
            return settingsProfile.Get_Thrust_MPS();
        }

        public int Get_Target_FrameRate()
        {
            return settingsProfile.Get_Target_Frame_Rate();
        }

        public Sigmoid_Values Get_Male_Yaw_SigmoidValues()
        {
            return male_Yaw_SigmoidValues;
        }

        public Sigmoid_Values Get_Female_Yaw_SigmoidValues()
        {
            return female_Yaw_SigmoidValues;
        }

        public void Update_Tethered_Animal_Controller_YawThrust_Values()
        {
            TetheredAnimalAvatarController.Instance.Set_Yaw_Values(settingsProfile.Get_Yaw_Method(), settingsProfile.Get_Yaw_DPS(), 
            settingsProfile.Get_Min_Yaw(), settingsProfile.Get_Yaw_At_Midpoint(), settingsProfile.Get_Max_Yaw(), 
            settingsProfile.Get_Midpoint_WBAD(),settingsProfile.Get_Min_WBAD(),settingsProfile.Get_Max_WBAD());
            

            TetheredAnimalAvatarController.Instance.Set_Thrust_Values(settingsProfile.Get_Thrust_Method(), settingsProfile.Get_Thrust_MPS(), 
            settingsProfile.Get_Min_Thrust(), settingsProfile.Get_Thrust_At_Midpoint(), settingsProfile.Get_Max_Thrust(), 
            settingsProfile.Get_Midpoint_WBAS(),settingsProfile.Get_Min_WBAS(),settingsProfile.Get_Max_WBAS());
        }

        public List<Vector2Int> Get_List_Of_Monitor_Resolutions()
        {
            return settingsProfile.Get_List_Of_Monitor_Resolutions();
        }

        public string Get_Profile_Printout()
        {
            return settingsProfile.Get_Profile_Printout();
        }
        #endregion

        public void Set_New_Profile(SettingsProfile newProfile)
        {
            if(newProfile != null)
            {
                settingsProfile = newProfile;
                Initialise_Settings();
            }
        }
    }
}
