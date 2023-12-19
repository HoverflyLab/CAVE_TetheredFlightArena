//Created By Raymond Aoukar 07/07/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //The Object of Interest (OOI) Manager allows for Scriptable objects and scripts to gain access to any Object of Interest within the current scene at runtime via the OOI's Unique ID.
    //On scene start, this script is used to store all of the Objects of Interest in a list.
    //It also provides the functionality for checking if the Tethered Animal Avatar's position is within proximity to an Object of Interest within the scene.
    public class OOIManager : MonoBehaviour
    {
        public static OOIManager Instance = null;
        
        [SerializeField, ReadOnly] private List<ObjectOfInterest> list_Of_OOI_In_Scene = new List<ObjectOfInterest>();

        private List<Proximity_Data_Struct> Proximity_Data_Structs = new List<Proximity_Data_Struct>();
        private List<ProximityFlag> flags = new List<ProximityFlag>();
        private List<Coroutine> proximity_Routines = new List<Coroutine>();
        private bool verbose = false;

        private Transform tetheredAnimal = null;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Error: There can only be one OOI Manager");
                Destroy(this.gameObject);
            }
        }

        private void Start()
        {
            if(TetheredAnimalAvatarController.Instance.gameObject.transform != null)
            {
                tetheredAnimal = TetheredAnimalAvatarController.Instance.gameObject.transform;
            }
            else
            {
                Debug.LogError("Error: Tethered Animal could not be located in scene");
            }
        }

        private void FixedUpdate()
        {
            Check_Proximities();
        }

        private void Check_Proximities()
        {
            //Check proximity between tethered animal and the targets (Objects of Interest)
            if (Proximity_Data_Structs.Count > 0 && tetheredAnimal != null)
            {
                for (int i = 0; i < Proximity_Data_Structs.Count; i++)
                {
                    //If false check to see if the tethered animal is now within the proximity
                    if (flags[i] == ProximityFlag.False)
                    {
                        //if the distance between the tracked OOI and the tethered animal is below the allotted proximity value complete the trial
                        if (Vector3.Distance(Proximity_Data_Structs[i].triggerOOITransform.position, tetheredAnimal.position) <= Proximity_Data_Structs[i].proximity)
                        {
                            flags[i] = ProximityFlag.True;
                            proximity_Routines[i] = StartCoroutine(Trigger_Proximity(i));
                        }
                    }
                    else if (flags[i] == ProximityFlag.True)
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
                            flags[i] = ProximityFlag.False;
                        }
                    }
                    else if (flags[i] == ProximityFlag.Triggered)
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
            flags[TrackingNo] = ProximityFlag.Triggered;

            TrialManager.Instance.ProximityComplete(Proximity_Data_Structs[TrackingNo].triggerOOITransform.gameObject.name, Proximity_Data_Structs[TrackingNo].triggerOOITransform.gameObject.GetComponent<ObjectOfInterest>().GetID());
        }

        //Populated on Awake by ObjectOfInterest scripts in scene
        public void Add_OOI_To_List(ObjectOfInterest OOI)
        {
            if(OOI == null) 
            {
                Debug.LogWarning("Warning: OOI is null, cannot be added to list");
            }

            if(list_Of_OOI_In_Scene.Contains(OOI) == false)
            {
                list_Of_OOI_In_Scene.Add(OOI);
            }
            else
            {
                Debug.LogWarning("Warning: OOI has already been added to the list, ID : " + OOI.GetID());
            }
        }

        public ObjectOfInterest Get_OOI_From_ID(int ID) 
        {
            foreach(ObjectOfInterest OOI in list_Of_OOI_In_Scene)
            {
                if(OOI == null)
                {
                    continue;
                }

                if(OOI.GetID() == ID)
                {
                    return OOI;
                }
            }

            Debug.LogWarning("Warning: OOI is not in list, ID : " + ID);
            return null;
        }

        public void Track_Proximity_To_OOI(int ID, float proximity, float duration)
        {
            ObjectOfInterest temp = Get_OOI_From_ID(ID);

            if(temp != null)
            {
                //If OOI is not the tethered animal
                if (temp.gameObject.transform != tetheredAnimal)
                {
                    Proximity_Data_Struct tempStruct = new Proximity_Data_Struct();

                    tempStruct.triggerOOITransform = temp.gameObject.transform;
                    tempStruct.proximity = proximity;
                    tempStruct.proximityDuration = duration;

                    flags.Add(ProximityFlag.False);
                    proximity_Routines.Add(null);
                    Proximity_Data_Structs.Add(tempStruct);
                }
                else
                {
                    Debug.LogWarning("Warning: Cannot Track Proximity of the Tethered Animal to the Tethered Animal");
                }
            }
        }

        public List<ObjectOfInterest> Get_List_Of_OOI()
        {
            return list_Of_OOI_In_Scene;
        }

        private void StopRoutines()
        {
            if (verbose) { print("Stopping All Coroutines"); }

            foreach (Coroutine routine in proximity_Routines)
            {
                //routines are initialised as null in this list and may not be replaced by a valid routine
                if(routine != null)
                {
                    StopCoroutine(routine);
                }  
            }
        }

        //Triggered at the end of each trial
        public void Create_Datafiles_For_Tracked_Objects()
        {
            foreach(ObjectOfInterest obj in list_Of_OOI_In_Scene)
            {
                obj.Create_Object_Datafile();
            }
        }

        private void OnDestroy()
        {
            StopRoutines();
        }
    }
}