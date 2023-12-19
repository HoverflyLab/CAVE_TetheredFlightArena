//Created By Raymond Aoukar 19/07/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    public struct Proximity_Data_Struct
    {
        public int Intervention_ID;
        public Transform triggerOOITransform;
        public float proximity;
        public float proximityDuration;
    }

    public struct Transform_Over_Frames_Struct
    {
        public Transform TriggerOOITransform;
        public int originalNumberofFrames;
    }

    //This script handles all of the logic associated with Interventions.
    public class InterventionsManager : MonoBehaviour
    {
        public static InterventionsManager Instance = null;

        [SerializeField, ReadOnly] private List<Intervention> list_Of_Interventions = new List<Intervention>();
        [SerializeField] private bool verbose = false;

        private Transform tetheredAnimal = null;
        private List<Coroutine> duration_Routines = new List<Coroutine>();

        private List<Proximity_Data_Struct> Proximity_Data_Structs = new List<Proximity_Data_Struct>();
        private List<Coroutine> proximity_Routines = new List<Coroutine>();
        private List<ProximityFlag> proximity_Flags = new List<ProximityFlag>();

        private List<Transform_Over_Frames_Struct> Transform_Over_Frames_Structs = new List<Transform_Over_Frames_Struct>();
        private List<Transform> currentTransform = new List<Transform>();
        private List<int> framesLeft = new List<int>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Error: There can only be one Interventions Manager");
                Destroy(this.gameObject);
            }
            TrialManager.Instance.OpenLoop += Trial_Complete;
        }

        private void Start()
        {
            if(TetheredAnimalAvatarController.Instance != null)
            {
                tetheredAnimal = TetheredAnimalAvatarController.Instance.gameObject.transform;
            }
            else
            {
                Debug.LogError("Error: TetheredAnimalController not found");
            }
        }

        private void Update()
        {
            Transform_Over_Frames();
        }

        private void Transform_Over_Frames()
        {
            if (Transform_Over_Frames_Structs.Count > 0)
            {
                for (int i = 0; i < Transform_Over_Frames_Structs.Count; i++)
                {
                    var temp = Transform_Over_Frames_Structs[i];

                    if (framesLeft[i] > 0)
                    {
                        framesLeft[i] = framesLeft[i] - 1;
                        
                        //Step our current transform toward the value provided by the triggerOOI Transform
                        currentTransform[i].localPosition += (temp.TriggerOOITransform.localPosition / temp.originalNumberofFrames);
                        currentTransform[i].localEulerAngles += (temp.TriggerOOITransform.localEulerAngles / temp.originalNumberofFrames);
                        currentTransform[i].localScale += (temp.TriggerOOITransform.localScale / temp.originalNumberofFrames);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            Check_Proximities();
        }

        private void Check_Proximities() 
        {
            if (Proximity_Data_Structs.Count > 0 && tetheredAnimal != null)
            {
                for (int i = 0; i < Proximity_Data_Structs.Count; i++)
                {
                    //If false check to see if the tethered animal is now within the proximity
                    if (proximity_Flags[i] == ProximityFlag.False)
                    {
                        //if the distance between the tracked OOI and the tethered animal is below the allotted proximity value complete the trial
                        if (Vector3.Distance(Proximity_Data_Structs[i].triggerOOITransform.position, tetheredAnimal.position) <= Proximity_Data_Structs[i].proximity)
                        {
                            proximity_Flags[i] = ProximityFlag.True;
                            proximity_Routines[i] = StartCoroutine(Trigger_Proximity(i));
                        }
                    }
                    else if (proximity_Flags[i] == ProximityFlag.True)
                    {
                        //if the distance between the tracked OOI and the tethered animal is below the allotted proximity value complete the trial
                        if (Vector3.Distance(Proximity_Data_Structs[i].triggerOOITransform.position, tetheredAnimal.position) <= Proximity_Data_Structs[i].proximity)
                        {
                            // Still in proximity so do nothing
                        }
                        else
                        {
                            //No longer in proximity so cancel coroutine
                            StopCoroutine(proximity_Routines[i]);
                            proximity_Flags[i] = ProximityFlag.False;
                        }
                    }
                    else if (proximity_Flags[i] == ProximityFlag.Triggered)
                    {
                        //Do nothing
                    }
                }
            }
        }

        IEnumerator Trigger_Proximity(int TrackingNo) 
        {
            yield return new WaitForSecondsRealtime(Proximity_Data_Structs[TrackingNo].proximityDuration);

            //Set to triggered to disable this proximity intervention.
            proximity_Flags[TrackingNo] = ProximityFlag.Triggered;

            //Get the intervention's position within the list, use that number to access and trigger the correct intervention.
            OnProximity(list_Of_Interventions[Proximity_Data_Structs[TrackingNo].Intervention_ID]);

            if(list_Of_Interventions[Proximity_Data_Structs[TrackingNo].Intervention_ID].Get_isRepeatable() == true)
            {
                proximity_Flags[TrackingNo] = ProximityFlag.False;
            }
        }

        //Get intervention and set it up based on its trigger type
        public void Setup_Intervention(Intervention intervention)
        {
            list_Of_Interventions.Add(intervention);

            if(intervention.Get_Action() == InterventionActions.ApproachingObject)
            {
                //set the variables required for approach beforehand so when triggered we are already good to go, regardless of whether it is a replay or not.
                OOIManager.Instance.Get_OOI_From_ID(intervention.Get_ApproachingOOI().Get_Object_Of_Interest_ID()).SetupApproachVariables(
                    intervention.Get_TargetOOI(), intervention.Get_isTrackingTarget(), intervention.Get_ApproachSpeed(), intervention.Get_ApproachDistance(), 
                    intervention.Get_ApproachDirection(), intervention.Get_ApproachOffset(), intervention.Get_ApproachProximity(), intervention.Get_ApproachProximityDuration());
            }

            switch (intervention.Get_Trigger())
            {
                case InterventionTriggers.Duration:
                    Coroutine temp = StartCoroutine(Wait_duration(intervention , intervention.Get_TriggerDelayDuration()));
                    duration_Routines.Add(temp);
                    //cancel routine on scene end/skip etc?
                    break;
                
                case InterventionTriggers.Proximity:
                    Track_Proximity_To_OOI(list_Of_Interventions.Count - 1, intervention.Get_TriggerOOI().Get_Object_Of_Interest_ID(), intervention.Get_TriggerProximity(),intervention.Get_TriggerProximityDuration());
                    break;

                //case Intervention_Triggers.LineofSight:
                //    Debug.LogError("Error: Line of Sight not implemented, use another trigger");
                //    break;

                default:
                    Debug.LogWarning("Warning: Code should not reach here");
                    break;
            }
        }

        #region Triggers
        //Wait the desired amount of time and then perform the Action specified by the intervention
        IEnumerator Wait_duration(Intervention intervention, float triggerdelay)
        {
            yield return new WaitForSecondsRealtime(triggerdelay);

            Trigger_Intervention(intervention);
            
            if(intervention.Get_isRepeatable() == true)
            {
                Coroutine temp = StartCoroutine(Wait_duration(intervention , intervention.Get_TriggerDelayDuration()));
                duration_Routines.Add(temp);
            }
        }

        private void OnLineOfSight(Intervention intervention)
        {
            Trigger_Intervention(intervention);
        }

        private void OnProximity(Intervention intervention)
        {
            Trigger_Intervention(intervention);
        }

        private void Trigger_Intervention(Intervention intervention) 
        {
            switch (intervention.Get_Action())
            {
                case InterventionActions.Edit_OOI_Transform:

                    if(intervention.Get_Intervention_Type() == InterventionType.Instant)
                    {
                        Edit_Transform(intervention);
                    }
                    else
                    {
                        Add_To_Transform_Struct(intervention);
                    }
                    break;

                case InterventionActions.Hide_OOI:
                    Hide_Object(intervention);
                    break;

                case InterventionActions.Show_OOI:
                    Show_Object(intervention);
                    break;

                case InterventionActions.ApproachingObject:
                    MakeOOIApproach(intervention);
                    break;

                default:
                    Debug.LogWarning("Warning: Code should not reach here");
                    break;
            }
        }
        #endregion

        private void Add_To_Transform_Struct(Intervention intervention)
        {
            Transform_Over_Frames_Struct tempStruct = new Transform_Over_Frames_Struct();

            //Create a new GameObject for its Transform, assign this Transfrom the desired values then add it to the struct
            GameObject tempGameObject = new GameObject();
            tempGameObject.transform.localPosition = intervention.Get_Position();
            tempGameObject.transform.localEulerAngles = intervention.Get_Rotation();
            tempGameObject.transform.localScale = intervention.Get_Scale();
            
            tempStruct.TriggerOOITransform = tempGameObject.transform;
            tempStruct.originalNumberofFrames = intervention.Get_NumberOfFrames();

            //Get object of interest from the Object of interest manager via ID, then add the transform of this object to the list.
            currentTransform.Add(OOIManager.Instance.Get_OOI_From_ID(intervention.Get_TargetOOI().Get_Object_Of_Interest_ID()).transform);
            framesLeft.Add(intervention.Get_NumberOfFrames());

            Transform_Over_Frames_Structs.Add(tempStruct);
        }

        #region Actions
        private void Edit_Transform(Intervention intervention)
        {
            
            Transform temp = OOIManager.Instance.Get_OOI_From_ID(intervention.Get_TargetOOI().Get_Object_Of_Interest_ID()).transform;
            //Call this so the OOI can log in its .transform CSV that it was forcibly moved
            OOIManager.Instance.Get_OOI_From_ID(intervention.Get_TargetOOI().Get_Object_Of_Interest_ID()).EditTransform();

            temp.localPosition += intervention.Get_Position();
            temp.localEulerAngles += intervention.Get_Rotation();
            temp.localScale += intervention.Get_Scale();
        }

        private void Hide_Object(Intervention intervention)
        {
            OOIManager.Instance.Get_OOI_From_ID(intervention.Get_TargetOOI().Get_Object_Of_Interest_ID()).HideInScene();
        }

        private void Show_Object(Intervention intervention)
        {
            OOIManager.Instance.Get_OOI_From_ID(intervention.Get_TargetOOI().Get_Object_Of_Interest_ID()).ShowInScene();
        }
        #endregion

        private void MakeOOIApproach(Intervention intervention) 
        {
            OOIManager.Instance.Get_OOI_From_ID(intervention.Get_ApproachingOOI().Get_Object_Of_Interest_ID()).BeginApproach();
        }

        private void Track_Proximity_To_OOI(int interventionID , int triggerOOI_ID, float proximity, float proximityduration)
        {
            ObjectOfInterest temp = OOIManager.Instance.Get_OOI_From_ID(triggerOOI_ID);

            if (temp != null)
            {
                //If target OOI is not the tethered animal
                if (temp.gameObject.transform != tetheredAnimal)
                {
                    Proximity_Data_Struct tempStruct = new Proximity_Data_Struct();

                    tempStruct.Intervention_ID = interventionID;
                    tempStruct.triggerOOITransform = temp.gameObject.transform;
                    tempStruct.proximity = proximity;
                    tempStruct.proximityDuration = proximityduration;
                    
                    proximity_Flags.Add(ProximityFlag.False);
                    proximity_Routines.Add(null);
                    Proximity_Data_Structs.Add(tempStruct);
                }                
                else
                {
                    Debug.LogWarning("Warning: Cannot Track the proximity of the Tethered Animal Avatar against itself.");
                }
            }
        }

        private void StopRoutines()
        {
            if (verbose) { print("Stopping All Interventions"); }
            foreach (Coroutine routine in duration_Routines)
            {
                StopCoroutine(routine);
            }

            foreach (Coroutine routine in proximity_Routines)
            {
                //routines are initialised as null in this list and may not be replaced by a valid routine
                if(routine != null)
                {
                    StopCoroutine(routine);
                }  
            }
        }

        private void Trial_Complete()
        {
            StopRoutines();
        }

        private void OnDestroy()
        {
            TrialManager.Instance.OpenLoop -= Trial_Complete;
            StopRoutines();
        }
    }
}