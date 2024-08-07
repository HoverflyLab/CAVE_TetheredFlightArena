//Created By Raymond Aoukar 07/07/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.IO;
using System;
using UnityEditor;

namespace TetheredFlight
{ 
    //This is the ObjectOfInterest Script, not to be confused with the Object_Of_Interest Scriptable Object which is different.
    //This script is only accessible on Objects of Interest within a Unity scene during runtime whereas the scriptable objects are always accessible.

    //This script exists on every OOI within the scene and is used to track their movements, add them to the OOI Manager and control their behaviour.
    public class ObjectOfInterest : MonoBehaviour
    {
        [SerializeField, ReadOnly] private Objects_Of_Interest_Parent _OOI_Parent = null;
        [SerializeField, ReadOnly, Tooltip("Set this value by adding the OOI to the OOI parent")] private int UniqueID = -1;
        [SerializeField] private Object_Of_Interest ScriptablePair = null;
        [SerializeField] private GameObject VisibleComponent = null;
        [SerializeField] private ObjectOfInterestType ObjectType = ObjectOfInterestType.StaticOOI;
        [SerializeField] private bool isHiddenOnStart = false;

        [Space]
        #region  Dynamic OOI Variables
        [SerializeField, ShowIf(nameof(ObjectType),ObjectOfInterestType.DynamicOOI), Tooltip("Speed in meters per second")] private float objectSpeed = 1f;
        [SerializeField, ShowIf(nameof(ObjectType),ObjectOfInterestType.DynamicOOI)] private GameObject patrolPointParent = null;
        [SerializeField, ShowIf(nameof(ObjectType),ObjectOfInterestType.DynamicOOI)] private bool isPatrolRepeatable = true;
        [SerializeField, ShowIf(nameof(ObjectType),ObjectOfInterestType.DynamicOOI)] private Dynamic_OOI_Behaviours initialBehaviour = Dynamic_OOI_Behaviours.Patrol;
        [SerializeField, ShowIf(nameof(ObjectType),ObjectOfInterestType.DynamicOOI)] private Dynamic_OOI_Behaviours encounterBehaviour = Dynamic_OOI_Behaviours.None;
        [SerializeField, ShowIf(nameof(isShowingInitialOOI)), Tooltip("The Initial OOI you wish to target")] private Object_Of_Interest initialOOI = null;
        [SerializeField, ShowIf(nameof(isShowingAfterEncounterOOI)), Tooltip("The OOI you wish to target after the encounter")] private Object_Of_Interest afterEncounterOOI = null;
        [SerializeField, ShowIf(nameof(isChasing)), Tooltip("When chasing this OOI will stop this distance away from the target, will resume chasing if target moves outside this distance")] private float minimumApproachDistance = 0.2f;

        [Space]
        [SerializeField, HideIf(nameof(encounterBehaviour), Dynamic_OOI_Behaviours.None)] private EncounterType encounterType = EncounterType.Duration;
        [SerializeField, ShowIf(nameof(isDurationEncounter)), Tooltip("Duration in seconds until the encounter is triggered")] private float triggerDelay = 1f;
        [SerializeField, ShowIf(nameof(isProximityEncounter)), Tooltip("The OOI that will trigger the encounter when it is within the set proximity")] private Object_Of_Interest OOI_To_Encounter = null;
        [SerializeField, ShowIf(nameof(isProximityEncounter)), MinValue(0.1f), Tooltip("Radius in meters, take care when setting radius < 1")] private float encounterRadius = 1.2f;
        [SerializeField, ShowIf(nameof(isProximityEncounter)), Tooltip("Duration in seconds, a duration of 0 immediately triggers the encounter when OOI is within the radius")] private float encounterDuration = 1f;
        
        private Dynamic_OOI_Behaviours currentBehaviour = Dynamic_OOI_Behaviours.Hover;
        private List<Transform> list_Of_PatrolPoints = new List<Transform>();
        private int currentPatrolPoint = 0;
        private float waypointDistanceValue = 0.05f; //in meters
        private ProximityFlag proximityFlag = ProximityFlag.False;
        private Coroutine proximityRoutine = null;
        private Transform initialOOI_Transform = null;
        private Transform OOI_To_Encounter_Transform = null;
        private Transform afterEncounterOOI_Transform = null;

        //Approach Intervention Variables
        private Transform approachTarget = null;    //target this OOI is approaching
        private Vector3 approachTargetOriginalLocation = new Vector3(); //original location of the target being approached
        private bool isTrackingTarget = false;      //Is chasing target's current location or original location
        private float approachSpeed = 2.5f;
        private float approachDistance = 5f;
        private Vector3 approachDirection = new Vector3();
        private Vector3 approachOffset = new Vector3();
        private float successDistance = 0.1f;
        private float successDelay = 1f;
        private Vector3 locationBeforeApproach = new Vector3();
        private Vector3 rotationBeforeApproach = new Vector3();
        private bool approachOverride = false;
        private bool isApproachComplete = false;
        Transform tempApproachTransform = null;
        private Coroutine approachCoroutine = null;
        #endregion

        #region  Data Variables
        private List<string>[] PositionData = new List<string>[3];
        private List<string>[] RotationData = new List<string>[3];
        private List<string>[] ScaleData = new List<string>[3];
        private List<string> TimeData = new List<string>();
        #endregion
        
        #region  Reading and Writing Variables
        private string[][] values = null;
        private int FrameNo = 0;
        private FileStream replayStream = null;
        private StreamWriter Writer = null;
        #endregion

        #region  Inspector variables
        private bool isShowingInitialOOI = false;
        private bool isShowingAfterEncounterOOI = false;
        private bool isDurationEncounter = false;
        private bool isProximityEncounter = false;
        private bool isChasing = false;
        #endregion

        private bool parentedToTarget = false;
        private bool loopClosed = false;
        private bool capturingTrialData = false;
        private bool replayActive = false;

        [Button]
        private void AddToOOIParent()
        {
            //If ID has not been assigned and OOI_Parent exists
            //This is used to Grant this Object of Interest it's Unique ID.
            if (UniqueID == -1 && _OOI_Parent != null)
            {
                if(ScriptablePair != null)
                {
                    _OOI_Parent.Add_OOI_To_List(this);
                }
                else
                {
                    Debug.LogWarning("Warning: Must assign this OOI's scriptable pair before adding it to the OOI Parent");
                }
                
            }
        }

        //Sets the UniqueID for this script based on the ID of the scriptablePair assigned to it.
        [Button]
        private void UpdateIDViaScriptablePairID()
        {
            if (ScriptablePair != null)
            {   
                UniqueID = ScriptablePair.Get_Object_Of_Interest_ID();
                #if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
                #endif
            }
        }

        private void OnValidate()
        {
            //Logic for hiding and showing the Dynamic OOI Variables within the inspector
            if(ObjectType == ObjectOfInterestType.DynamicOOI)
            {
                if (initialBehaviour == Dynamic_OOI_Behaviours.Chase || encounterBehaviour == Dynamic_OOI_Behaviours.Chase) 
                {
                    isChasing = true;
                }
                else 
                {
                    isChasing = false;
                }

                //Behaviours that require the initial OOI to function
                if (initialBehaviour == Dynamic_OOI_Behaviours.Chase || initialBehaviour == Dynamic_OOI_Behaviours.Flee || initialBehaviour == Dynamic_OOI_Behaviours.StayRelative)
                {
                    isShowingInitialOOI = true;
                }
                else
                {
                    isShowingInitialOOI = false;
                }

                if(encounterBehaviour != Dynamic_OOI_Behaviours.None)
                {
                    if(encounterType == EncounterType.Duration)
                    {
                        isDurationEncounter = true;
                        isProximityEncounter = false;
                    }
                    else
                    {
                        isDurationEncounter = false;
                        isProximityEncounter = true;
                    }
                }
                else
                {
                    isDurationEncounter = false;
                    isProximityEncounter = false;
                }

                //Behaviours that require the after encounter OOI to function
                if(encounterBehaviour == Dynamic_OOI_Behaviours.Chase || encounterBehaviour == Dynamic_OOI_Behaviours.Flee || encounterBehaviour == Dynamic_OOI_Behaviours.StayRelative)
                {
                    isShowingAfterEncounterOOI = true;
                }
                else
                {
                    isShowingAfterEncounterOOI = false;
                }
            }
            else
            {
                isShowingInitialOOI = false;
                isShowingAfterEncounterOOI = false;
                if(isChasing == true) { isChasing = false; }; //avoids value not being used warning
                if (isDurationEncounter == true){isDurationEncounter = false;}; //avoids value not being used warning
                if(isProximityEncounter == true){isProximityEncounter = false;}; //avoids value not being used warning
            }
        }

        private void Awake()
        {
            if(UniqueID < 0)
            {
                Debug.LogWarning("Warning: Object of Interest has not been assigned a Unique ID");
                Destroy(this.gameObject);
            }

            if(OOIManager.Instance != null) 
            {
                OOIManager.Instance.Add_OOI_To_List(this);
            }
            else 
            {
                Debug.LogError("Error: No OOI manager in this scene, deleting this OOI as it won't function without the manager");
                Destroy(this);
            }

            //Initialise the lists within the Array
            for(int i = 0; i < PositionData.Length; i++)
            {
                PositionData[i] = new List<string>();
            }

            for(int i = 0; i < RotationData.Length; i++)
            {
                RotationData[i] = new List<string>();
            }

            for(int i = 0; i < ScaleData.Length; i++)
            {
                ScaleData[i] = new List<string>();
            }

            if(isHiddenOnStart == true)
            {
                VisibleComponent.SetActive(false);
            }

            TrialManager.Instance.PreStimulusStarted += OnPreStimulusStarted;
            TrialManager.Instance.CloseLoop += OnLoopClosed;
            TrialManager.Instance.OpenLoop += OnLoopOpened;
            TrialManager.Instance.PostStimulusComplete += OnPostStimulusComplete;
        }

        void Start()
        {
            if(ObjectType == ObjectOfInterestType.DynamicOOI)
            {
                if(initialBehaviour == Dynamic_OOI_Behaviours.None)
                {
                    //None only exists as an option in order to disable encounters, 
                    //there always be an initial behaviour for dynamic OOI.
                    Debug.LogWarning("Warning: OOI with ID " + UniqueID + " has no initial behaviour.");
                    initialBehaviour = Dynamic_OOI_Behaviours.Hover;
                }

                currentBehaviour = initialBehaviour;
                
                //if we have an encounter behaviour and our EncounterType is duration
                if(encounterBehaviour != Dynamic_OOI_Behaviours.None || encounterType == EncounterType.Duration)
                {
                    Trigger_Delay();
                }

                if(patrolPointParent == null)
                {
                    Debug.LogWarning("OOI with ID " + UniqueID + " has not had its patrolpointparent assigned, this is only an issue if it is trying to use the patrol behaviour");
                }
                else 
                {
                    //load the patrol points and seperate them from this game object
                    for (int i = 0; i < patrolPointParent.transform.childCount; i++)
                    {
                        list_Of_PatrolPoints.Add(patrolPointParent.transform.GetChild(i));
                    }

                    patrolPointParent.transform.parent = OOIManager.Instance.transform;
                }
                
                if(isShowingInitialOOI == true)
                {
                    if(initialOOI == null)
                    {
                        Debug.LogError("OOI with ID " + UniqueID + " InitialOOI value is null and needs to be assigned");
                    }

                    initialOOI_Transform = GetOOITransform(initialOOI);
                }

                if(encounterBehaviour != Dynamic_OOI_Behaviours.None)
                {
                    if(OOI_To_Encounter == null)
                    {
                        Debug.LogError("OOI with ID " + UniqueID + " OOI_To_Encounter value is null and needs to be assigned");
                    }

                    OOI_To_Encounter_Transform = GetOOITransform(OOI_To_Encounter);
                }

                if(isShowingAfterEncounterOOI == true)
                {
                    if(afterEncounterOOI == null)
                    {
                        Debug.LogError("OOI with ID " + UniqueID + " afterEncounterOOI value is null and needs to be assigned");
                    }

                    afterEncounterOOI_Transform = GetOOITransform(afterEncounterOOI);
                }
            }
        }

        //Get Object of Interest transform within the scene, from the Objects_Of_Interest_Manager
        private Transform GetOOITransform(Object_Of_Interest OOI)
        {
            if(OOI != null)
            {
                return OOIManager.Instance.Get_OOI_From_ID(OOI.Get_Object_Of_Interest_ID()).transform;
            }
            else
            {
                Debug.LogError("Error: The OOI should not be null and needs to be assigned");
                return null;
            }
        }

        // FixedUpdate is called 165 times a second regardless of framerate
        void FixedUpdate()
        {
            if(loopClosed == true && replayActive == false)
            {
                if(approachOverride == false) //When set to approach via intervention we override any Dynamic OOI behaviours
                {
                    if(ObjectType == ObjectOfInterestType.DynamicOOI)
                    {
                        CheckForProximityEncounter();
                        Dynamic_OOI_Behaviour();
                    }
                }
                else
                {
                    ApproachBehaviour();
                }
            }

            //If it is a replay read values from file and update this objects position
            if(replayActive == true)
            {
                Update_Transfrom_From_File();
            }
        }

        void Update()
        {
            //If we are saving data and it is not a replay then record the values for every frame
            if(DirectoryManager.Instance.Is_Saving_Data() == true && capturingTrialData == true)
            {
                Record_Current_Transform();
            }
        }

        private void CheckForProximityEncounter()
        {
            //If no encounter behaviour is planned or the encounter type is not proximity do not check
            if(encounterBehaviour == Dynamic_OOI_Behaviours.None || encounterType != EncounterType.Proximity)
            {
                return;
            }

            //if proximity has been triggered stop checking
            if(proximityFlag == ProximityFlag.Triggered)
            {
                return;
            }

            //If false, check to see if the tethered animal is now within the proximity
            if (proximityFlag == ProximityFlag.False)
            {
                //if the distance between the tracked OOI and the tethered animal is below the allotted proximity value complete the trial
                if (Vector3.Distance(this.transform.position, OOI_To_Encounter_Transform.position) <= encounterRadius)
                {
                    proximityFlag = ProximityFlag.True;
                    proximityRoutine = StartCoroutine(Trigger_Proximity());
                }
            }
            else if (proximityFlag == ProximityFlag.True)
            {
                //if the distance between the tracked OOI and the tethered animal is below the allotted proximity value complete the trial
                if (Vector3.Distance(this.transform.position, OOI_To_Encounter_Transform.position) <= encounterRadius)
                {
                    // Still in proximity so do nothing
                }
                else
                {
                    //No longer in proximity so cancel coroutine
                    StopCoroutine(proximityRoutine);
                    proximityFlag = ProximityFlag.False;
                }
            }
        }

        IEnumerator Trigger_Proximity() 
        {
            yield return new WaitForSecondsRealtime(encounterDuration);

            //Set to triggered to disable proximity check.
            proximityFlag = ProximityFlag.Triggered;
            TriggerEncounterBehaviour();
        }

        IEnumerator Trigger_Delay()
        {
            yield return new WaitForSecondsRealtime(triggerDelay);
            TriggerEncounterBehaviour();
        }

        #region Dynamic OOI Behaviours
        private void Dynamic_OOI_Behaviour()
        {
            switch(currentBehaviour)
            {
                case Dynamic_OOI_Behaviours.Hover:
                    //Do nothing, stay still
                    break;
                
                case Dynamic_OOI_Behaviours.Patrol:
                    //move toward next patrolpoint
                    PatrolBehaviour();
                    break;

                case Dynamic_OOI_Behaviours.Chase:
                    //move toward OOI
                    ChaseBehaviour();
                    break;

                case Dynamic_OOI_Behaviours.Flee:
                    //move away from OOI
                    FleeBehaviour();
                    break;

                case Dynamic_OOI_Behaviours.StayRelative:
                    //Child this OOI to the Target so they move together
                    KeepRelativeToTarget();
                    break;
            }
        }
        
        private void PatrolBehaviour()
        {
            //if we are within the distanceValue of the current waypoint
            if(Vector3.Distance(this.transform.position, list_Of_PatrolPoints[currentPatrolPoint].position) <= waypointDistanceValue)
            {
                //and if next PatrolPoint exists make it the next patrol point, If not go back to start if repeatable otherwise do nothing.
                if(currentPatrolPoint + 1 < list_Of_PatrolPoints.Count)
                {
                    currentPatrolPoint++;
                }
                else if(isPatrolRepeatable == true)
                {
                    currentPatrolPoint = 0;
                }
                else
                {
                    return;
                }
            }

            transform.LookAt(list_Of_PatrolPoints[currentPatrolPoint]);
            transform.position += transform.forward * objectSpeed * Time.deltaTime;
        }

        //Look at object and move toward it
        private void ChaseBehaviour()
        {
            if(currentBehaviour == initialBehaviour)
            {
                transform.LookAt(initialOOI_Transform);

                //If we are not within minimum distance continue moving toward target
                if(Vector3.Distance(transform.position,initialOOI_Transform.position) > minimumApproachDistance) 
                {
                    transform.position += objectSpeed * Time.deltaTime * transform.forward;
                }
            }
            else
            {
                transform.LookAt(afterEncounterOOI_Transform);

                //If we are not within minimum distance continue moving toward target
                if (Vector3.Distance(transform.position, afterEncounterOOI_Transform.position) > minimumApproachDistance)
                {
                    transform.position += objectSpeed * Time.deltaTime * transform.forward;
                }
            }
        }

        //Look away from object and move away from it
        private void FleeBehaviour()
        {
            if(currentBehaviour == initialBehaviour)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - initialOOI_Transform.position);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(transform.position - afterEncounterOOI_Transform.position);
            }
            
            transform.position += objectSpeed * Time.deltaTime * transform.forward;
        }

        private void KeepRelativeToTarget() 
        {
            if(parentedToTarget == true) 
            {
                return;
            }

            if (currentBehaviour == initialBehaviour) //If statements like this would be redundant if there was a target_Transform which updated when behaviour changed.
            {
                this.transform.parent = initialOOI_Transform;
                parentedToTarget = true;
            }
            else 
            {
                this.transform.parent = afterEncounterOOI_Transform;
                parentedToTarget = true;
            }
        }
        #endregion

        private void Record_Current_Transform()
        {
            PositionData[0].Add(transform.position.x.ToString());
            PositionData[1].Add(transform.position.y.ToString());
            PositionData[2].Add(transform.position.z.ToString());
            
            RotationData[0].Add(transform.eulerAngles.x.ToString());
            RotationData[1].Add(transform.eulerAngles.y.ToString());
            RotationData[2].Add(transform.eulerAngles.z.ToString());

            //Use local scale as we cannot set Global Scale from recorded values
            ScaleData[0].Add(transform.localScale.x.ToString());
            ScaleData[1].Add(transform.localScale.y.ToString());
            ScaleData[2].Add(transform.localScale.z.ToString());

            TimeData.Add(Time.deltaTime.ToString());
        }

        private void Update_Transfrom_From_File()
        {
            //If this is the last frame and end of replay has not been set to true
            if(FrameNo >= PositionData[0].Count)
            {
                if(ReplayManager.Instance.Get_Is_End_Of_Replay() == false)
                {
                    ReplayManager.Instance.Set_is_End_Of_Replay(true);
                }
                
                return;
            }

            //if this line contains a notification, skip it
            if(PositionData[0][FrameNo].Contains("N:"))
            {
                FrameNo++;
                return;
            }

            //if this line contains a command, apply it
            if(PositionData[0][FrameNo].Contains("C:"))
            {
                //remove "C:" chars from string and parse it as an Intervention_Action
                ReplayActions action = (ReplayActions)Enum.Parse(typeof(ReplayActions), PositionData[0][FrameNo].Substring(2));

                switch(action)
                {
                    case ReplayActions.Hide_OOI:
                        VisibleComponent.SetActive(false);
                        break;

                    case ReplayActions.Show_OOI:
                        VisibleComponent.SetActive(true);
                        break;

                    case ReplayActions.Edit_OOI_Transform:
                        //Do nothing as OOI will move frame by frame from recorded data.
                        break;

                    case ReplayActions.ApproachingObject:
                        //Do nothing as OOI will move frame by frame from recorded data.
                        break;

                    case ReplayActions.OOI_Encounter_Triggered:
                        //Do nothing as OOI will move frame by frame from recorded data.
                        break;

                    default:
                        Debug.Log("Code should not reach here, only Hide and Show and ApproachObject are expected");
                    break;
                }
            }
            else //it contains position data
            {
                transform.position = new Vector3(float.Parse(PositionData[0][FrameNo]),float.Parse(PositionData[1][FrameNo]),float.Parse(PositionData[2][FrameNo]));
                transform.eulerAngles = new Vector3(float.Parse(RotationData[0][FrameNo]),float.Parse(RotationData[1][FrameNo]),float.Parse(RotationData[2][FrameNo]));  
                transform.localScale = new Vector3(float.Parse(ScaleData[0][FrameNo]),float.Parse(ScaleData[1][FrameNo]),float.Parse(ScaleData[2][FrameNo]));                  
            }

            FrameNo++;            
        }

        //Send the entire replay file through at the start 
        public void GetReplayData(string replaydata)
        {
            replayStream = new FileStream(replaydata, FileMode.Open, FileAccess.ReadWrite);

            //Initialise with a large amount of values to reduce reallocation costs
            values = new string[100000][];
            FrameNo = 0;

            using(var reader = new StreamReader(replayStream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    //Skip first 4 rows (frameNo 0 - 3), as it is the Object information and heading fields, not the values we want to use for replay.
                    if(FrameNo >= 4)
                    {
                        values[FrameNo] = line.Split(',');
                        PositionData[0].Add(values[FrameNo][0]);   //Transform x
                        PositionData[1].Add(values[FrameNo][1]);   //Transform y
                        PositionData[2].Add(values[FrameNo][2]);   //Transform z
                        RotationData[0].Add(values[FrameNo][3]);   //Rotation x
                        RotationData[1].Add(values[FrameNo][4]);   //Rotation y
                        RotationData[2].Add(values[FrameNo][5]);   //Rotation z
                        ScaleData[0].Add(values[FrameNo][6]);       //Scale x
                        ScaleData[1].Add(values[FrameNo][7]);       //Scale y
                        ScaleData[2].Add(values[FrameNo][8]);       //Scale z
                        TimeData.Add(values[FrameNo][9]);
                    }

                    FrameNo++;
                }
            }
        }

        public void BeginReplay()
        {
            FrameNo = 0;
            replayActive = true;
        }

        public void Create_Object_Datafile()
        {
            if(DirectoryManager.Instance.Get_Trial_Folder_Directory() == null)
            {
                return;
            }

            // CSV_filename will become equal to something in the following format 23-02-2021_9-42-18_Tree_3.Transform.csv
            string CSV_filename = DirectoryManager.Instance.Get_StartDate() + "_" + DirectoryManager.Instance.Get_Trial_StartTime() + "_" + this.gameObject.name.ToString() + "_" + UniqueID.ToString() +".Transform.csv";
            string CSV_filepath = DirectoryManager.Instance.Get_Trial_Folder_Directory() + "/" + CSV_filename;

            if (!File.Exists(CSV_filepath))
            {
                // Create a file to write to
                using (Writer = File.CreateText(CSV_filepath))
                {

                    Writer.WriteLine("OOI ID,Name,Object Type,Speed,Initial Behaviour,EncounterBehaviour");
                    Writer.WriteLine(UniqueID.ToString() + "," + this.name + "," + ObjectType.ToString() + "," + objectSpeed.ToString() + "," + initialBehaviour.ToString() + "," + encounterBehaviour.ToString());
                    Writer.WriteLine("");
                    Writer.WriteLine("X position, Y position, Z position, X rotation, Y rotation, Z rotation, X scale, Y scale, Z scale, TimeSinceLastUpdate");

                    for(int i = 0; i < PositionData[0].Count; i++)
                    {
                        //Print    position x y z    then      rotation x y z     then       time
                        Writer.WriteLine(PositionData[0][i] + "," + PositionData[1][i] + "," + PositionData[2][i] 
                                    + "," + RotationData[0][i] + "," + RotationData[1][i] + "," + RotationData[2][i]
                                    + "," + ScaleData[0][i] + "," + ScaleData[1][i] + "," + ScaleData[2][i]
                                    + "," + TimeData[i]);
                    }
                }	
            }
            else
            {
                Debug.LogWarning("Warning: Object Data File Already Exists");
            }
        }
        
        public int GetID()
        {
            return UniqueID;
        }

        public void SetID(int newID)
        {
            if(UniqueID == -1)
            {
                UniqueID = newID;
                UpdateScriptablePair();
                #if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
                #endif
            }
            else
            {
                Debug.LogWarning("Warning: OOI has already been assigned an ID");
            }
        }

        private void UpdateScriptablePair()
        {
            ScriptablePair.Set_Object_Of_Interest_ID(UniqueID);
            ScriptablePair.Set_Object_Of_Interest_Type(ObjectType);
        }

        public bool IsDynamic() 
        {
            if(ObjectType == ObjectOfInterestType.DynamicOOI)
            {
                return true;
            }
            return false;
        }

        public void OnPreStimulusStarted()
        {
            capturingTrialData = true;
            AddNotificationToDataFile("PreStim Started");
        }

        public void OnLoopClosed()
        {
            loopClosed = true;
            AddNotificationToDataFile("Loop Closed");
        }

        public void OnLoopOpened()
        {
            loopClosed = false;
            AddNotificationToDataFile("Loop Opened");
        }

        public void OnPostStimulusComplete()
        {
            capturingTrialData = false;
            AddNotificationToDataFile("Post Stim Finished");
        }

        #region Commands
        public void AddCommandToDataFile(string command)
        {
            //Add show command to output file
            PositionData[0].Add("C:" + command);

            //row of zeroes for the rest of the lists
            PositionData[1].Add("");
            PositionData[2].Add("");
            RotationData[0].Add("");
            RotationData[1].Add("");
            RotationData[2].Add("");
            ScaleData[0].Add("");
            ScaleData[1].Add("");
            ScaleData[2].Add("");
            string temptime = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");
            TimeData.Add(temptime);
        }

        public void AddNotificationToDataFile(string notification)
        {
            //Add show command to output file
            PositionData[0].Add("N:" + notification);

            //row of zeroes for the rest of the lists
            PositionData[1].Add("");
            PositionData[2].Add("");
            RotationData[0].Add("");
            RotationData[1].Add("");
            RotationData[2].Add("");
            ScaleData[0].Add("");
            ScaleData[1].Add("");
            ScaleData[2].Add("");
            string temptime = System.DateTime.Now.ToString("yyyyMMdd_Hmmss.ffffff");
            TimeData.Add(temptime);
        }

        public void ShowInScene() 
        {
            //Add Show Command to output file
            AddCommandToDataFile(ReplayActions.Show_OOI.ToString());

            VisibleComponent.SetActive(true);
        }

        public void HideInScene() 
        {
            //Add hide command to output file
            AddCommandToDataFile(ReplayActions.Hide_OOI.ToString());

            VisibleComponent.SetActive(false);
        }

        public void EditTransform()
        {
            //Add Edit Transform command to output file
            AddCommandToDataFile(ReplayActions.Edit_OOI_Transform.ToString());
        }

        public void BeginApproach()
        {
            //Add Approaching command to output file
            AddCommandToDataFile(ReplayActions.ApproachingObject.ToString());

            InitialiseApproach();
        }

        private void TriggerEncounterBehaviour()
        {
            //Add Encounter Behaviour Timestamp to output file
            AddNotificationToDataFile(ReplayActions.OOI_Encounter_Triggered.ToString());

            currentBehaviour = encounterBehaviour;
        }
        #endregion

        public void SetupApproachVariables(Object_Of_Interest TargetOOI, bool istrackingtarget, float approachspeed, float approachdistance, Vector3 approachdirection, Vector3 approachoffset, float successdistance, float successdelay) 
        {
            if(approachTarget != null) { Debug.LogWarning("This OOI " + UniqueID + " has already had its approach variables set. Are there 2 approach interventions utilising this same OOI?"); }

            approachTarget = OOIManager.Instance.Get_OOI_From_ID(TargetOOI.Get_Object_Of_Interest_ID()).transform;
            isTrackingTarget = istrackingtarget;
            approachSpeed = approachspeed;
            approachDistance = approachdistance;
            approachDirection = approachdirection;
            approachOffset = approachoffset;
            successDistance = successdistance;
            successDelay = successdelay;
        }

        //Runs before approachOverride is set and FixedUpdate executes the approach
        private void InitialiseApproach() 
        {
            if(approachTarget == null)
            {
                Debug.LogError("approachTarget is null so approach intervention cannot be executed");
                return;
            }

            if(approachCoroutine != null) 
            {
                StopCoroutine(approachCoroutine);
            }
            
            locationBeforeApproach = transform.position;
            rotationBeforeApproach = transform.eulerAngles;

            isApproachComplete = false;
            approachTargetOriginalLocation = approachTarget.position;

            if(tempApproachTransform == null) 
            {
                //Setup a temp transform so we can use the convienient .forward command when calculating the starting location of this OOI
                tempApproachTransform = new GameObject().transform;
            }
             
            //Set temp transforms values to the same values as our approach target so we can calculate our values based on its location and rotation.
            tempApproachTransform.position = approachTarget.position;
            tempApproachTransform.eulerAngles = approachTarget.eulerAngles + approachDirection;

            //set this objects position relative to the target OOI (Its original direction + approach direction) then spawn it at the approach distance in that direction.
            this.transform.position = approachTargetOriginalLocation + (tempApproachTransform.forward * approachDistance);

            approachOverride = true;
        }

        private void ApproachBehaviour()
        {
            if(isApproachComplete == false) 
            {
                //If this object should change heading every from to track the target.
                if (isTrackingTarget == true)
                {
                    transform.LookAt(approachTarget.position + approachOffset);

                    //If we are not within the success distance continue moving toward target
                    if (Vector3.Distance(transform.position, (approachTarget.position + approachOffset)) > successDistance)
                    {
                        transform.position += approachSpeed * Time.deltaTime * transform.forward;
                    }
                    else
                    {
                        approachCoroutine = StartCoroutine(ApproachComplete());
                        isApproachComplete = true;
                    }
                }
                else //this object heads toward the target's location when the intervention started
                {
                    transform.LookAt(approachTargetOriginalLocation + approachOffset);

                    //If we are not within the success distance continue moving toward target
                    if (Vector3.Distance(transform.position, (approachTargetOriginalLocation + approachOffset)) > successDistance)
                    {
                        transform.position += approachSpeed * Time.deltaTime * transform.forward;
                    }
                    else
                    {
                        approachCoroutine = StartCoroutine(ApproachComplete());
                        isApproachComplete = true;
                    }
                }
            }
        }

        IEnumerator ApproachComplete() 
        {
            yield return new WaitForSecondsRealtime(successDelay);

            this.transform.position = locationBeforeApproach;
            this.transform.eulerAngles = rotationBeforeApproach;
            approachOverride = false;
        }

        public void SetObjectSpeed(float newSpeed)
        {
            objectSpeed = newSpeed;
        }

        private void OnDestroy()
        {
            if(proximityRoutine != null)
            {
                StopCoroutine(proximityRoutine);
            }

            if(approachCoroutine != null) 
            {
                StopCoroutine(approachCoroutine);
            }
            
            TrialManager.Instance.PreStimulusStarted -= OnPreStimulusStarted;
            TrialManager.Instance.CloseLoop -= OnLoopClosed;
            TrialManager.Instance.OpenLoop -= OnLoopOpened;
            TrialManager.Instance.PostStimulusComplete -= OnPostStimulusComplete;
        }

        void reset()
        {
            _OOI_Parent.Remove_OOI_From_List(this);
        }
    }
}
