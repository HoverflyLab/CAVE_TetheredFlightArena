//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using NaughtyAttributes;

namespace TetheredFlight
{
    // This Script follows a different naming convention than the rest of the project to highlight variable names and avoid confusion between similarly named variables.
    // The DataProcessor Script receives the packets directly from the UDPSocket class and uses the information inside to calculate the left and right wing beat amplitude (WBA), 
    // The values are then passed to the TetheredAnimalAvatarController script which uses this information to update the yaw and thrust of the Tethered Animal Avatar.
    public class DataProcessor : MonoBehaviour
    {
        public static DataProcessor Instance = null;

        //once these values are set they should not change
        #region  Static values             
        [SerializeField, ReadOnly] private float Axis_Angle = 0f;
        #endregion

        #region Points and Angle Variables
        [SerializeField, ReadOnly] private string [] Array_of_points = null;

        #region LongitudinalAxis Point Variables
        [SerializeField, ReadOnly, Tooltip("Is true when manually setting the longitudinal Axis")] private bool Static_Longitudinal_Axis = false;
        [SerializeField] private Vector2 Wings_Thorax_Upper = new Vector2(160f,200f);
        [SerializeField] private Vector2 Wings_Thorax_Lower = new Vector2(160f,100f);
        #endregion

        #region Hinge (Inner) Point Variables
        [SerializeField] private bool Use_manual_hinge_points = false;
        //Right_inner and Left_inner points are the Right and Left Hinge variables
        [SerializeField, ShowIf(nameof(Use_manual_hinge_points))] private Vector2 Wings_Hinge_Right = new Vector2();
        [SerializeField, ShowIf(nameof(Use_manual_hinge_points))] private Vector2 Wings_Hinge_Left = new Vector2();
        #endregion

        #region Outer Point Variables
        [SerializeField, ReadOnly] private Vector2 Wings_Distal_Right = new Vector2();
        [SerializeField, ReadOnly] private Vector2 Wings_Distal_Left = new Vector2();
        #endregion

        [SerializeField, ReadOnly] private float WBA_Right = 0f;  
        [SerializeField, ReadOnly] private float WBA_Left = 0f;        
        #endregion

        //This is used to flip Y_values e.g. Y_value = Frame_height - Y_value
        //Frame_height is updated via SettingsProfile on start
        private int Frame_height = 240; 
        
        private float LongitudinalAxis_slope_width = 0f;
        private float LongitudinalAxis_slope_height = 0f;
        private float WBA_SlopeWidth_Right = 0f;
        private float WBA_SlopeHeight_Right = 0f;
        private float WBA_SlopeWidth_Left = 0f;
        private float WBA_SlopeHeight_Left = 0f;

        private System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

        #region CSV file Variables
        [SerializeField,Tooltip("Is used to set appropriate list sizes to avoid unneccesary memory allocation, always overestimate")] private int Average_trial_duration_in_minutes = 2;
        private List<string>[] Array_of_lists = null;
        private StreamWriter Writer = null;
        private double Arrival_time_of_last_packet = -1f;
        private double Packet_delta_time = -1f;
        private bool valuesUpdated = false;
        private int packetsThisTrial = 0;
        #endregion

        #region Running average variables
        [SerializeField, MinValue(5)] private int runningAverageSize = 5;
        private Queue<Vector2> upper_LongitudinalAxis_Point_Queue = new Queue<Vector2>();
        private Vector2 upperPointRunningAverage = new Vector2();
        private Queue<Vector2> lower_LongitudinalAxis_Point_Queue = new Queue<Vector2>();
        private Vector2 lowerPointRunningAverage = new Vector2();  
        #endregion

        private bool isSavingData = false;
        private LoopStatus LoopStatus = LoopStatus.BetweenTrials;
        private bool containsUnusedLabels = false;
        
        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Error: There can only be one DataProcessor script");
                Destroy(this.gameObject);
            } 

            TrialManager.Instance.PreStimulusStarted += OnPreStimStarted;
            TrialManager.Instance.CloseLoop += OnLoopClosed;
            TrialManager.Instance.OpenLoop += OnLoopOpened;
            TrialManager.Instance.PostStimulusComplete += OnPostStimCompleted;
        }

        // Start is called before the first frame update
        void Start()
        {
            Frame_height = SettingsManager.Instance.Get_Video_Resolution().y;

            //If we have manually set the longitudinal axis
            if(UIManager.Instance.Get_isLongitudinalAxisCalc_Manual() == true)
            {
                Static_Longitudinal_Axis = true;
                LongitudinalAxis_slope_width = (Wings_Thorax_Lower.x - Wings_Thorax_Upper.x);
                LongitudinalAxis_slope_height = (Wings_Thorax_Lower.y - Wings_Thorax_Upper.y);

                Axis_Angle = (Mathf.Abs(Mathf.Atan(LongitudinalAxis_slope_width/LongitudinalAxis_slope_height))) * Mathf.Rad2Deg;
            }

            if(DirectoryManager.Instance == null) // If Non-TF Scene
            {
                Debug.LogError("Error: Directory Manager does not exist");
                isSavingData = false;
                return;
            }

            if(DirectoryManager.Instance.Is_Saving_Data() == true)
            { 
                isSavingData = true;
                
                //store scene data (Hoverfly data, Positions of actors etc)
                //Value of 4 is used as we are storing the [packet][time since last packet][values updated][frame loaded]
                Array_of_lists = new List<string>[4];
                for(int i = 0; i < Array_of_lists.Length;i++)
                {
                    // Example: Assuming 165 frames per second
                    // 165 * 60 gives the number of frames per minute, times that by expected duration in order estimate total list size.
                    // This is done to reduce the number of times additional memory needs to be allocated in order to handle the increased list size.
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
        }

        //calculates the Right and Left wing angles
        private void Calculate_Wing_Angles() 
        {
            //if we are automatically setting the longitudinal axis
            if(Static_Longitudinal_Axis == false)
            {
                LongitudinalAxis_slope_width = (Wings_Thorax_Upper.x - Wings_Thorax_Lower.x);
                LongitudinalAxis_slope_height = (Wings_Thorax_Upper.y - Wings_Thorax_Lower.y);

                Axis_Angle = (Mathf.Atan(LongitudinalAxis_slope_width/LongitudinalAxis_slope_height)) * Mathf.Rad2Deg;
            }

            WBA_SlopeWidth_Right = Wings_Distal_Right.x - Wings_Hinge_Right.x;
            WBA_SlopeHeight_Right = Wings_Distal_Right.y - Wings_Hinge_Right.y;

            WBA_SlopeWidth_Left = Wings_Distal_Left.x - Wings_Hinge_Left.x;
            WBA_SlopeHeight_Left = Wings_Distal_Left.y - Wings_Hinge_Left.y;

            //calculates the absolute arctan and returns the answer in degrees
            WBA_Right = (Mathf.Atan(WBA_SlopeWidth_Right/WBA_SlopeHeight_Right)) * Mathf.Rad2Deg;
            if(WBA_SlopeHeight_Right >= 0)
            {
                //calculates the absolute arctan and returns the answer in degrees
                WBA_Right = 180.00f - Mathf.Abs(WBA_Right - Axis_Angle);                   
            }
            else
            {
                WBA_Right = Mathf.Abs(WBA_Right - Axis_Angle);
            }

            //calculates the absolute arctan and returns the answer in degrees
            WBA_Left = (Mathf.Atan(WBA_SlopeWidth_Left/WBA_SlopeHeight_Left))  * Mathf.Rad2Deg;
            if(WBA_SlopeHeight_Left >= 0)
            {
                WBA_Left = 180.00f - Mathf.Abs(WBA_Left - Axis_Angle);
            }
            else
            {
                WBA_Left = Mathf.Abs(WBA_Left - Axis_Angle);
            }

            Update_Tethered_Animal_Position();
        }

        private void Update_Tethered_Animal_Position() 
        {
            if (TetheredAnimalAvatarController.Instance != null)
            {
                TetheredAnimalAvatarController.Instance.Update_Angles(WBA_Right, WBA_Left);
            }
            //else if (Non-TFController.Instance != null)
            //{
            //    Non-TFController.Instance.Update_Angles(WBA_Right, WBA_Left);
            //}
            else
            {
                Debug.LogError("Error: No TetheredAnimalController present in scene");
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

        public void Set_LongitudinalAxis_Upper_Points(Vector2 UpperPos)
        {
            Wings_Thorax_Upper = UpperPos;
        }

        public void Set_LongitudinalAxis_Lower_Points(Vector2 LowerPos)
        {
            Wings_Thorax_Lower = LowerPos;
        }

        //This method is sent packets (strings) from the UDPSocket as they are received.
        //These packets are then dissasembled and the appropriate values assigned to variables.
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

            foreach(string point in Array_of_points)
            {
                if(point.Contains(",") == false)
                {
                    //most likely time stamp
                    continue;
                }
                string[] coords = point.Split(',');

                //Index [0] of Coords is ID
                string ID = coords[0].ToString();

                //Index [1] is label x position
                //Index [2] is label y position
                
                switch(ID)
                {                    
                    //According to the DeepLabCut Project config file,
                    //ID 0 should be for the Wings_Hinge_Right label
                    //Frame_height - value, gives us the correct Y position as deeplabcut inverts this value
                    //E.g we recieve a Y of 240 from deelabcut when points are at the bottom of the frame (what we really want is 0)
                    case "0": 
                        if(Use_manual_hinge_points == false)
                        {
                            Wings_Hinge_Right.x = float.Parse(coords[1]);
                            Wings_Hinge_Right.y = Frame_height - float.Parse(coords[2]);
                        }
                    break;

                    // ID 1 should be for the Wings_Distal_Right label
                    case "1":    
                        Wings_Distal_Right.x = float.Parse(coords[1]);
                        Wings_Distal_Right.y = Frame_height - float.Parse(coords[2]);
                    break;

                    // ID 2 should be for the Wings_Hinge_Left label
                    case "2": 
                        if(Use_manual_hinge_points == false)
                        {
                            Wings_Hinge_Left.x = float.Parse(coords[1]);
                            Wings_Hinge_Left.y = Frame_height - float.Parse(coords[2]);
                        }
                    break;

                    // ID 3 should be for the Wings_Distal_Left label
                    case "3": 
                        Wings_Distal_Left.x = float.Parse(coords[1]);
                        Wings_Distal_Left.y = Frame_height - float.Parse(coords[2]);
                    break;

                    // ID 4 should be for the Wings_Thorax_Upper label
                    case "4": 
                        if(UIManager.Instance.Get_isLongitudinalAxisCalc_Manual() == false)
                        {
                            Vector2 temp = new Vector2(float.Parse(coords[1]), Frame_height - float.Parse(coords[2]));
                            Wings_Thorax_Upper = GetNewRunningAverageForUpperPoint(temp);
                        }
                    break;

                    // ID 5 should be for the Wings_Thorax_Lower label
                    case "5": 
                        if(UIManager.Instance.Get_isLongitudinalAxisCalc_Manual() == false)
                        {
                            Vector2 temp = new Vector2(float.Parse(coords[1]), Frame_height - float.Parse(coords[2]));
                            Wings_Thorax_Lower = GetNewRunningAverageForLowerPoint(temp);
                        }
                    break;
                    
                    default: 
                        if(containsUnusedLabels == false)
                        {
                            Debug.LogWarning("Warning: Ignoring extra labels, only first 6 labels are used when closing the loop.");
                            containsUnusedLabels = true;
                        }
                    break;
                }
            }

            if(isSavingData == true)
            {
                //save raw unchanged packet so the exact same data can be read back into Unity for replay.
                Array_of_lists[0].Add(packet);
                
                //Time between packets
                Array_of_lists[1].Add(Packet_delta_time.ToString());

                if(LoopStatus != LoopStatus.BetweenTrials)
                {
                    packetsThisTrial++;
                }
            }

            if(LoopStatus == LoopStatus.LoopClosed)
            {
                Calculate_Wing_Angles();
            }
            else if(isSavingData == true)
            {
                Array_of_lists[2].Add(LoopStatus.ToString());
                Array_of_lists[3].Add(LoopStatus.ToString());
            }
        }

        public void Create_DLC_Datafile()
        {
            print("No of Packets during trial = " + Array_of_lists[0].Count.ToString());

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
                    Writer.WriteLine("ID,x,y;ID,x,y;ID,x,y;ID,x,y;ID,x,y;ID,x,y;Time Frame was Captured;Time Packet was Sent;DLC_Latency#DeltaPacketTime#ValuesUpdate#FrameCreated");
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

        private void OnPreStimStarted()
        {
            packetsThisTrial = 0;
            LoopStatus = LoopStatus.PreStim;
        }

        private void OnLoopClosed()
        {
            LoopStatus = LoopStatus.LoopClosed;
        }

        private void OnLoopOpened()
        {
            LoopStatus = LoopStatus.PostStim;
        }

        private void OnPostStimCompleted()
        {
            LoopStatus = LoopStatus.BetweenTrials;
            TrialManager.Instance.Set_Packets_This_Trial(packetsThisTrial);
        }

        private double Get_Time_In_Milliseconds()
        {
            double currentTime = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            return currentTime / 1000;
        }

        private Vector2 GetNewRunningAverageForUpperPoint(Vector2 newPoint)
        {
            //If we are at the size limit dequeue oldest member of the list (default behaviour of a dequeue)
            if(upper_LongitudinalAxis_Point_Queue.Count >= runningAverageSize)
            {
                upper_LongitudinalAxis_Point_Queue.Dequeue();
            }

            //Add new vector to the back of the queue
            upper_LongitudinalAxis_Point_Queue.Enqueue(newPoint);

            //reset variable
            upperPointRunningAverage = new Vector2();

            //Add all the current points in the list together
            foreach(Vector2 point in upper_LongitudinalAxis_Point_Queue)
            {
                upperPointRunningAverage = upperPointRunningAverage + point;
            }

            //get average of all the points
            upperPointRunningAverage = upperPointRunningAverage / upper_LongitudinalAxis_Point_Queue.Count;

            return upperPointRunningAverage;
        }

        private Vector2 GetNewRunningAverageForLowerPoint(Vector2 newPoint)
        {
            //If we are at the size limit dequeue oldest member of the list (default behaviour of a dequeue)
            if(lower_LongitudinalAxis_Point_Queue.Count >= runningAverageSize)
            {
                lower_LongitudinalAxis_Point_Queue.Dequeue();
            }

            //Add new vector to the back of the queue
            lower_LongitudinalAxis_Point_Queue.Enqueue(newPoint);

            //reset variable
            lowerPointRunningAverage = new Vector2();

            //Add all the current points in the list together
            foreach(Vector2 point in lower_LongitudinalAxis_Point_Queue)
            {
                lowerPointRunningAverage = lowerPointRunningAverage + point;
            }

            //get average of all the points
            lowerPointRunningAverage = lowerPointRunningAverage / lower_LongitudinalAxis_Point_Queue.Count;

            return lowerPointRunningAverage;
        }

        public Vector2 Get_LongitudinalAxis_Upper_Point()
        {
            return Wings_Thorax_Upper;
        }

        public Vector2 Get_LongitudinalAxis_Lower_Point()
        {
            return Wings_Thorax_Lower;
        }

        public String Get_LongitudinalAxisPoints()
        {
            return Wings_Thorax_Upper.x.ToString()+ "," + Wings_Thorax_Upper.y.ToString() + "," + Wings_Thorax_Lower.x.ToString()+ "," + Wings_Thorax_Lower.y.ToString();
        }

        public void Reset_Packet_Arrival_Time()
        {
            Arrival_time_of_last_packet = Get_Time_In_Milliseconds();
        }

        private void OnDestroy()
        {
            TrialManager.Instance.PreStimulusStarted -= OnPreStimStarted;
            TrialManager.Instance.CloseLoop -= OnLoopClosed;
            TrialManager.Instance.OpenLoop -= OnLoopOpened;
            TrialManager.Instance.PostStimulusComplete -= OnPostStimCompleted;
        }
    }   
}
