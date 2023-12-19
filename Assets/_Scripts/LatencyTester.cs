//Created By Raymond Aoukar 15/11/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.IO;
using System;
using UnityEngine.UI;

namespace TetheredFlight
{
    //This script is used to test the Latency of the System and is activated by setting the isLatencyTest value within the UPDSocket to true [MainMenu scene].
    //It operates similar to the DataProcessor script but additionally activates a canvas which displays a square in the bottom left corner of display 4.
    //This square changes from black to white depending on the data recieved from DLC-live.
    public class LatencyTester : MonoBehaviour
    {
        public static LatencyTester Instance = null;
        
        #region  Static values             
        [SerializeField, Tooltip("This is used to flip Y_values e.g. Y_value = Frame_height - imported Y_value")] private int Frame_height = 240;
        #endregion

        [SerializeField, ReadOnly] private string [] Array_of_points = null;

        #region Corner Point Variables
        [SerializeField, ReadOnly] private Vector2 top_Left_Point = new Vector2();
        [SerializeField, ReadOnly] private Vector2 top_Right_Point = new Vector2();
        [SerializeField, ReadOnly] private Vector2 bottom_Left_Point = new Vector2();
        [SerializeField, ReadOnly] private Vector2 bottom_Right_Point = new Vector2();
        #endregion

        [SerializeField, ReadOnly] private Vector2 midPoint = new Vector2(160f,200f);
        [SerializeField, ReadOnly] private Vector2 squareMidPoint = new Vector2(0f,0f);

        private System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

        #region CSV file Variables
        [SerializeField,Tooltip("Is used to set appropriate list sizes to avoid unneccesary memory allocation, always overestimate")] private int Average_trial_duration_in_minutes = 2;
        private List<string>[] Array_of_lists = null;
        private StreamWriter Writer = null;
        private double Arrival_time_of_last_packet = -1f;
        private double Packet_delta_time = -1f;
        private bool valuesUpdated = false;
        #endregion

        private bool isSavingData = false;
        private bool isClosedLoop = false;
        [SerializeField,ReadOnly] private bool containsUnusedLabels = false;

        #region Canvas Variables
        [SerializeField] private RawImage canvas = default;
        private Color32 whiteColor = new Color32(255,255,255,255);
        private Color32 blackColor = new Color32(0,0,0,255);
        private bool isWhite = false;
        #endregion

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one LatencyTester");
                Destroy(this.gameObject);
            }

            TrialManager.Instance.CloseLoop += CloseLoop;
            TrialManager.Instance.OpenLoop += OpenLoop;
        }

        // Start is called before the first frame update
        void Start()
        {   
            if(DirectoryManager.Instance == null) // If Non-TF Scene
            {
                Debug.LogError("Error: Directory Manager does not exist");
                isSavingData = false;
                return;
            }

            if(DirectoryManager.Instance.Is_Saving_Data() == true)
            { 
                isSavingData = true;
                
                //store scene data (Tethered Animal Avatar data, Positions of actors etc)
                //Value of 4 is used as we are storing the [packet][time since last packet][values updated][frame loaded]
                Array_of_lists = new List<string>[4];
                for(int i = 0; i < Array_of_lists.Length;i++)
                {
                    // Example: Assuming 165 frames per second
                    // 165 * 60 gives the number of frames per minute, times that by expected duration in order estimate total list size.
                    // This is done to reduce the number of times memory needs to be allocated in order to handle the increase in list size.
                    Array_of_lists[i] = new List<string>(SettingsManager.Instance.Get_Target_FrameRate()*60*Average_trial_duration_in_minutes);
                }
            }
        }

        void Update()
        {
            if(isSavingData == true && valuesUpdated == true)
            {
                Array_of_lists[3].Add(Get_Time_In_Milliseconds().ToString());
                valuesUpdated = false;
            }

            if(canvas.enabled == true)
            {
                if(isWhite == true)
                {
                    canvas.color = whiteColor;
                }
                else
                {
                    canvas.color = blackColor;
                }
            }
        }

        public void ActivateCanvas()
        {
            canvas.enabled = true;
        }

        //Currently finds the midpoint via a shortcut using only 2 corners (should be accurate enough).
        private void Calculate_Square_Position() 
        {
            squareMidPoint.x = (top_Left_Point.x + bottom_Right_Point.x)/2;
            squareMidPoint.y = (top_Left_Point.y + bottom_Right_Point.y)/2;

            Update_Latency_Canvas();
        }

        //If the square is above the midpoint make the canvas white, otherwise make it black.
        private void Update_Latency_Canvas() 
        {
            midPoint = DataProcessor.Instance.Get_LongitudinalAxis_Upper_Point();

            //send values to Latency Canvas
            if( squareMidPoint.y > midPoint.y)
            {
                //make canvas black
                isWhite = false;
            }
            else if( squareMidPoint.y < midPoint.y)
            {
                //make canvas white
                isWhite = true;
            }
            else
            {
                Debug.LogWarning("Warning: unexpected value during Latency Test");
            }

            if(isSavingData == true)
            {
                if(valuesUpdated == true)
                {
                    Array_of_lists[3].Add("Values Overwritten");
                }
                
                Array_of_lists[2].Add(Get_Time_In_Milliseconds().ToString());
                valuesUpdated = true;
            }
        }

        public void Get_Latest_UDP_Packet(string packet)
        {
            //      expecting the packet to be recieved in the following format
            //      ID,x,y;ID,x,y;ID,x,y;ID,x,y;Time Frame was Captured;Time Packet was Sent;DLC_Latency#DeltaPacketTime#ValuesUpdate#FrameCreated
            // e.g. 0,123.12314244,313.451551;1,111.12314244,247.451551;2,211.12314244,97.451551;3,85.12314244,17.451551; FrameCapture ; PacketSent ;

            if(packet.Contains(":") == true)
            {
                //iscommand
                //run command
                return;
            }   

            //Is meant to check for an empty packet and also catches packets with only 1 set of values
            //Change the if statement to check for ',' instead if only one set of values is okay  
            if(packet.Contains(";") == false)
            {
                Debug.LogWarning("Warning: Invalid Packet");
                return;
            }

            //Get Packet_delta_time value
            if(isSavingData == true)
            {       
                //if first packet  [as Arrival_time_of_last_packet is initialized with value of -1]
                if(Arrival_time_of_last_packet < 0)
                {
                   // Arrival_time_of_last_packet = GetTimeInMilliseconds();
                    Packet_delta_time = 0;
                }
                else
                {
                    // Packet_delta_time = time between packets
                    Packet_delta_time = Math.Round(Get_Time_In_Milliseconds() - Arrival_time_of_last_packet,5);
                }
                Arrival_time_of_last_packet = Get_Time_In_Milliseconds();
            }

            Array_of_points = packet.Split(';');

            for(int i = 0; i < 4; i++)
            {
                if(Array_of_points[i].Contains(",") == false)
                {
                    //most likely time stamp
                    continue;
                }

                //first char is ID
                char ID = Array_of_points[i][0];

                //cut ID and comma from string then seperate the rest of the string in two via the remaining comma
                //this results in "coords" only containing the x and y value
                string[] coords = Array_of_points[i].Substring(2,Array_of_points[i].Length-3).Split(',');

                switch(ID)
                {                    
                    //According to the DeepLabCut Leading Edge Project config file,
                    //ID 0 should be for the Right Inner / Hinge position
                    //Frame_height - value, gives us the correct Y position as deeplabcut inverts this value
                    //E.g we recieve a Y of 240 from deelabcut when points are at the bottom of the frame (what we really want is 0)
                    case '0': 
                        top_Left_Point.x = float.Parse(coords[0]);
                        top_Left_Point.y = Frame_height - float.Parse(coords[1]);
                    break;

                    // ID ` should be for the Right Outer position
                    case '1': 
                        top_Right_Point.x = float.Parse(coords[0]);
                        top_Right_Point.y = Frame_height - float.Parse(coords[1]);
                    break;

                    // ID 2 should be for the Left Inner / Hinge position
                    case '2': 
                        bottom_Left_Point.x = float.Parse(coords[0]);
                        bottom_Left_Point.y = Frame_height - float.Parse(coords[1]);
                    break;

                    // ID 3 should be for the Left Outer / Hinge position
                    case '3': 
                        bottom_Right_Point.x = float.Parse(coords[0]);
                        bottom_Right_Point.y = Frame_height - float.Parse(coords[1]);
                    break;

                    default: 
                        if(containsUnusedLabels == false)
                        {
                            Debug.LogWarning("Warning: Code should not reach here, for loop only covers cases 0-3.");
                            containsUnusedLabels = true;
                        }
                    break;
                }
            }

            if(isSavingData == true)
            {
                //save raw unchanged packet so the exact same data can be read into Unity during replay.
                Array_of_lists[0].Add(packet);
                
                //Time between packets so replay happens over the same duration
                Array_of_lists[1].Add(Packet_delta_time.ToString());
            }

            if(isClosedLoop == true)
            {
                Calculate_Square_Position();
            }
            else if(isSavingData == true)
            {
                Array_of_lists[2].Add("Open Loop");
                Array_of_lists[3].Add("Open Loop");
            }
        }

        public void Create_DLC_Datafile()
        {
            if(DirectoryManager.Instance.Get_Trial_Folder_Directory() == null)
            {
                return;
            }

            // Will become equal to something in the following format 23-02-2021_9-42-18_DLC-Data.csv
            string CSV_filename = DirectoryManager.Instance.Get_StartDate() + "_" + DirectoryManager.Instance.Get_Trial_StartTime() + "_DLC-Data.csv";
            string CSV_filepath = DirectoryManager.Instance.Get_Trial_Folder_Directory() + "/" + CSV_filename;

            if (!File.Exists(CSV_filepath))
            {
                // Create a file to write to.
                using (Writer = File.CreateText(CSV_filepath))
                {

                    Writer.WriteLine("ID,x,y;ID,x,y;ID,x,y;ID,x,y;Time Frame was Captured;Time Packet was Sent;DLC_Latency#DeltaPacketTime#ValuesUpdate#FrameCreated");
                    print("No of Packets = " + Array_of_lists[0].Count.ToString() + " , No of DeltaTime Packets = " + Array_of_lists[1].Count.ToString() + " , No of times WBA's were updated = " + Array_of_lists[2].Count.ToString() + " , No of times values generated frames = " + Array_of_lists[3].Count.ToString());
                    for(int i = 0; i < Array_of_lists[0].Count; i++)
                    {
                        Writer.WriteLine(Array_of_lists[0][i].ToString() + "#" + Array_of_lists[1][i].ToString() + "#" + Array_of_lists[2][i].ToString() + "#" + Array_of_lists[3][i].ToString());
                    }
                }	
            }                
            else
            {
                Debug.LogWarning("Warning: Tethered Animal Data File Already Exists");
            }

            //After DLC file is created clear packets and delta time values, so that these lists can be reused.
            foreach(List<string> list in Array_of_lists)
            {
                list.Clear();
            }
        }

        private void CloseLoop()
        {
            isClosedLoop = true;
        }

        private void OpenLoop()
        {
            isClosedLoop = false;
        }

        private double Get_Time_In_Milliseconds()
        {
            double currentTime = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            return currentTime / 1000;
        }

        private void OnDestroy()
        {
            TrialManager.Instance.CloseLoop -= CloseLoop;
            TrialManager.Instance.OpenLoop -= OpenLoop;
        }
    }
}
