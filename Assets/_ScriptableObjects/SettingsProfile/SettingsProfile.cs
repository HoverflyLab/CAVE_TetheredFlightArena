//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This Script is used to create the SettingsProfile Scriptable Objects, which are a container for all the settings data.
    //This data is used to set all approiate values such as framerate and yaw values across the project.
    [CreateAssetMenu(fileName = "new_Profile", menuName = "ScriptableObjects/Settings Profile", order = 1)]
    public class SettingsProfile : ScriptableObject
    {
        [SerializeField, DisableIf(nameof(isLocked)), Tooltip("Specify the folder name where all of your data will be saved")]
        private string personalFolderName = "Default Personal Folder";

        #region System Setup
        [Header("System Setup")]
        // Hide from settings profile since variable is unused [SerializeField, DisableIf(nameof(isLocked)), Tooltip("In Centimeters"), MinValue(0)]
        private float distanceFromMonitors = default;

        [SerializeField, DisableIf(nameof(isLocked)), Tooltip("Including experimenter's monitor"), MinValue(1)]
        private int numberOfMonitors = 4;

        [SerializeField, DisableIf(nameof(isLocked)), Tooltip("Refresh rate of the monitors displaying stimulus")]
        private int targetFrameRate = 165;

        [SerializeField, DisableIf(nameof(isLocked)), Tooltip("Resolution of monitors, in order [User monitor, stimulus monitor 1, stimulus monitor 2, etc")]
        private List<Vector2Int> list_of_monitorResolutions = new List<Vector2Int>();

        [SerializeField, DisableIf(nameof(isLocked)), Tooltip("Resolution of the video being analyzed by DLC-Live (Width,Height)")]
        private Vector2Int videoResolution = new Vector2Int(320,240);
        #endregion

        #region Yaw Variables
        [Header("Turning Speed (Yaw) Settings")]
        [SerializeField] 
        private YawMethod yaw_Method = YawMethod.Constant;

        [SerializeField, Tooltip("The Yaw value in degrees per second"),  ShowIf(nameof(yaw_Method), YawMethod.Constant), DisableIf(nameof(isLocked))] 
        private float yaw_dps = 36f;

        [SerializeField, MinValue(0f), Tooltip("The minimum Yaw value in degrees per second"),  HideIf(nameof(yaw_Method), YawMethod.Constant), DisableIf(nameof(isLocked))] 
        private float minYaw = 0f;

        [SerializeField, MinValue(0f), Tooltip("The Yaw value at the midpoint in degrees per second"),  ShowIf(nameof(yaw_Method), YawMethod.Variable), DisableIf(nameof(isLocked))] 
        private float yawAtMidpoint = 540f;

        [SerializeField, MinValue(0f), Tooltip("The maximum Yaw value in degrees per second"),  HideIf(nameof(yaw_Method), YawMethod.Constant), DisableIf(nameof(isLocked))] 
        private float maxYaw = 1080f;

        [SerializeField, ReadOnly, Tooltip("The minimum Wing Beat Amplitude Difference"),  HideIf(nameof(yaw_Method), YawMethod.Constant), DisableIf(nameof(isLocked))] 
        private float min_WBAD = 0;

        [SerializeField, Tooltip("The Wing Beat Amplitude Difference used to set the midpoint"), ShowIf(nameof(yaw_Method), YawMethod.Variable), DisableIf(nameof(isLocked))] 
        private float midpoint_WBAD = 90f;

        [SerializeField, Range(0,180), Tooltip("The maximum Wing Beat Amplitude Difference"),  HideIf(nameof(yaw_Method), YawMethod.Constant), DisableIf(nameof(isLocked))] 
        private float max_WBAD = 180f;

        //These are placeholder only as the Sigmoid Functionality does not exist *******
        [Header("Male Yaw Sigmoid Values (not implemented)")]
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)] 
        private float m_Bottom = default;
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)] 
        private float m_Top = default;
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)] 
        private float m_LoglC50 = default;
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)] 
        private float m_Hillslope = default;

        [Header("Female Yaw Sigmoid Values (not implemented)")]
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)]
        private float f_Bottom = default;
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)] 
        private float f_Top = default;
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)] 
        private float f_LoglC50 = default;
        //[SerializeField, ShowIf(nameof(yaw_Method), YawMethod.Sigmoid)] 
        private float f_Hillslope = default;
        #endregion

        #region Thrust Variables
        [Header("Movement Speed (Thrust) Settings")]
        [SerializeField] private ThrustMethod thrust_Method = ThrustMethod.Constant;
        [SerializeField, Tooltip("The Thrust value in meters per second"), ShowIf(nameof(thrust_Method), ThrustMethod.Constant), DisableIf(nameof(isLocked))] 
        private float thrust_mps = 2f;

        [SerializeField, MinValue(0f), Tooltip("The minimum Thrust value in meters per second"), HideIf(nameof(thrust_Method), ThrustMethod.Constant), DisableIf(nameof(isLocked))] 
        private float minThrust = 0f;

        [SerializeField, MinValue(0f), Tooltip("The Thrust value at the midpoint in meters per second"), ShowIf(nameof(thrust_Method), ThrustMethod.Variable), DisableIf(nameof(isLocked))] 
        private float thrustAtMidpoint = 0.34f;

        [SerializeField, MinValue(0f), Tooltip("The maximum Thrust value in meters per second"), HideIf(nameof(thrust_Method), ThrustMethod.Constant), DisableIf(nameof(isLocked))] 
        private float maxThrust = 3f;

        [SerializeField, MinValue(0), Tooltip("The minimum Wing Beat Amplitude Sum"), HideIf(nameof(thrust_Method), ThrustMethod.Constant), DisableIf(nameof(isLocked))] 
        private float min_WBAS = 0;

        [SerializeField, Tooltip("The Wing Beat Amplitude Sum used to set the midpoint"), MinValue(0f), ShowIf(nameof(thrust_Method), ThrustMethod.Variable), DisableIf(nameof(isLocked))] 
        private float midpoint_WBAS = 180f;

        [SerializeField, MinValue(0), Tooltip("The maximum Wing Beat Amplitude Sum"), HideIf(nameof(thrust_Method), ThrustMethod.Constant), DisableIf(nameof(isLocked))] 
        private float max_WBAS = 360;
        #endregion

        private void OnValidate()
        {
            totalResolutions = list_of_monitorResolutions.Count;

            if (numberOfMonitors != totalResolutions)
            {
                //If there are more objects than transforms
                if (numberOfMonitors > totalResolutions)
                {
                    //Add a new transform (this should retrigger OnValidate)
                    list_of_monitorResolutions.Add(new Vector2Int(1920, 1080));
                    OnValidate();
                }
                else
                {
                    //Remove the last transform (this should retrigger OnValidate)
                    list_of_monitorResolutions.RemoveAt(totalResolutions - 1);
                    OnValidate();
                }
            }
        }

        #region System Getters
        public string Get_Profile_Name() { return this.name; }
        public string Get_Personal_Folder_Name() { return personalFolderName; }
        public float Get_Distance_From_Monitors() { return distanceFromMonitors; }
        public int Get_Number_Of_Monitors() { return numberOfMonitors; }
        //relative location
        public int Get_Target_Frame_Rate() { return targetFrameRate; }
        public List<Vector2Int> Get_List_Of_Monitor_Resolutions() { return list_of_monitorResolutions; }
        public Vector2Int Get_Video_Resolution() {return videoResolution; }
        //gamma
        #endregion

        #region Yaw Getters
        public YawMethod Get_Yaw_Method() { return yaw_Method; }
        public float Get_Yaw_DPS() { return yaw_dps; }
        public float Get_Min_Yaw() { return minYaw; }
        public float Get_Yaw_At_Midpoint() { return yawAtMidpoint; }
        public float Get_Max_Yaw() { return maxYaw; }
        public float Get_Min_WBAD() { return min_WBAD; }
        public float Get_Midpoint_WBAD() { return midpoint_WBAD; }
        public float Get_Max_WBAD() { return max_WBAD; }

        public float Get_M_Bottom() { return m_Bottom; }
        public float Get_M_Top() { return m_Top; }
        public float Get_M_LoglC50() { return m_LoglC50; }
        public float Get_M_Hillside() { return m_Hillslope; }

        public float Get_F_Bottom() { return f_Bottom; }
        public float Get_F_Top() { return f_Top; }
        public float Get_F_LoglC50() { return f_LoglC50; }
        public float Get_F_Hillside() { return f_Hillslope; }
        #endregion

        #region Thrust Getters
        public ThrustMethod Get_Thrust_Method() { return thrust_Method; }
        public float Get_Thrust_MPS() { return thrust_mps; }
        public float Get_Min_Thrust() { return minThrust; }
        public float Get_Thrust_At_Midpoint() { return thrustAtMidpoint; }
        public float Get_Max_Thrust() { return maxThrust; }
        public float Get_Min_WBAS() { return min_WBAS; }
        public float Get_Midpoint_WBAS() { return midpoint_WBAS; }
        public float Get_Max_WBAS() { return max_WBAS; }
        #endregion

        public string Get_Profile_Printout()
        {
            string screenResolutions = "";

            foreach (Vector2 resolution in list_of_monitorResolutions)
            {
                screenResolutions += resolution.x.ToString() + ":" + resolution.y.ToString() + ",";
            }

            //Remove last semi-colon so it is easier to split the string when it is being loaded by Replay_Manager.
            //commented as likely no longer necessary due to the use of a comma instead of a semi colon when setting screenResolutions variable
            //screenResolutions = screenResolutions.TrimEnd(';');

            return this.name + "," + personalFolderName.ToString() + "," + distanceFromMonitors.ToString() + "," + numberOfMonitors.ToString()
                    + "," + targetFrameRate.ToString() + "," + screenResolutions + videoResolution.x + ":" + videoResolution.y + ","
                    + YawSettingsAsString() + SigmoidSettingsAsString() + ThrustSettingsAsString();
        }

        private string YawSettingsAsString()
        {
            return  yaw_Method.ToString() + "," + yaw_dps.ToString() + "," + minYaw.ToString() + "," + yawAtMidpoint.ToString()
                + "," + maxYaw.ToString() + "," + min_WBAD.ToString() + "," + midpoint_WBAD.ToString() + "," + max_WBAD.ToString();
        }

        private string SigmoidSettingsAsString()
        {
            return "," + m_Bottom.ToString() + "," + m_Top.ToString() + "," + m_LoglC50.ToString() + "," + m_Hillslope.ToString()
                    + "," + f_Bottom.ToString() + "," + f_Top.ToString() + "," + f_LoglC50.ToString() + "," + f_Hillslope.ToString();
        }

        private string ThrustSettingsAsString()
        {
            return "," + thrust_Method.ToString() + "," + thrust_mps.ToString() + "," + minThrust.ToString() + "," + thrustAtMidpoint.ToString()
                    + "," + maxThrust.ToString() + "," + min_WBAS.ToString() + "," + midpoint_WBAS.ToString() + "," + max_WBAS.ToString();
        }

        #region Setters
        //
        //The following methods should only be used by the replay manager when it instantiates a new scriptable,
        //it uses these methods to recreate the SettingsProfile saved in the data text files and allows for them to be reloaded into the Settings_Manager.
        //
        public void Set_Profile_Name(string name) { this.name = name; }
        public void Set_Personal_Folder_Name(string name) { personalFolderName = name; }
        public void Set_Distance_From_Monitors(float distance) { distanceFromMonitors = distance; }
        public void Set_Number_Of_Monitors(int number) { numberOfMonitors = number; }
        public void Set_Frame_Rate(int framerate) { targetFrameRate = framerate; }
        public void Set_List_Of_Monitor_Resolutions(List<Vector2Int> list) { list_of_monitorResolutions = list; }
        public void Set_Video_Resolution(Vector2Int res) { videoResolution = res; }

        public void Set_Yaw_Method(YawMethod method) { yaw_Method = method; }
        public void Set_Yaw_DPS(float yaw) { yaw_dps = yaw; }
        public void Set_Min_Yaw(float min) { minYaw = min; }
        public void Set_Yaw_At_Midpoint(float midpoint) { yawAtMidpoint = midpoint; }
        public void Set_Max_Yaw(float max) { maxYaw = max; }
        public void Set_Min_WBAD(float min) { min_WBAD = min; }
        public void Set_Midpoint_WBAD(float wbad) { midpoint_WBAD = wbad; }
        public void Set_Max_WBAD(float max) { max_WBAD = max; }

        public void Set_M_Bottom(float value) { m_Bottom = value; }
        public void Set_M_Top(float value) { m_Top = value; }
        public void Set_M_LoglC50(float value) { m_LoglC50 = value; }
        public void Set_M_Hillside(float value) { m_Hillslope = value; }

        public void Set_F_Bottom(float value) { f_Bottom = value; }
        public void Set_F_Top(float value) { f_Top = value; }
        public void Set_F_LoglC50(float value) { f_LoglC50 = value; }
        public void Set_F_Hillside(float value) { f_Hillslope = value; }

        public void Set_Thrust_Method(ThrustMethod method) { thrust_Method = method; }
        public void Set_Thrust_MPS(float thrust) { thrust_mps = thrust; }
        public void Set_Min_Thrust(float min) { minThrust = min; }
        public void Set_Thrust_At_Midpoint(float midpoint) { thrustAtMidpoint = midpoint; }
        public void Set_Max_Thrust(float max) { maxThrust = max; }
        public void Set_Min_WBAS(float min) { min_WBAS = min; }
        public void Set_Midpoint_WBAS(float wbas) { midpoint_WBAS = wbas; }
        public void Set_Max_WBAS(float max) { max_WBAS = max; }
        #endregion

        [Button]
        private void Lock_Variables()
        {
            isLocked = !isLocked;
        }

        #region Editor Logic Variables
        private bool isLocked = false;
        #endregion

        #region OnValidate Variables
        private int totalResolutions = 0;
        #endregion
    }
}
