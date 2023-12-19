//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This Script is used to create the Intervention Scriptable Objects, which contain the data required to setup an Intervention.
    //Interventions are used to Move, Hide or Show Objects of Interest during a trial.
    //These actions can be triggered by the proximity of two Objects of Interest or simply after a certain duration of time has passed.
    [CreateAssetMenu(fileName = "new_Intervention", menuName = "ScriptableObjects/Intervention", order = 6)]
    public class Intervention : ScriptableObject
    {
        #region Settings Variables
        [SerializeField, DisableIf(nameof(isLocked))] private InterventionActions Action = InterventionActions.Edit_OOI_Transform;
        [SerializeField, DisableIf(nameof(isLocked))] private Object_Of_Interest TargetOOI = null;

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action),InterventionActions.Edit_OOI_Transform), Tooltip("Additive, value of 0 will provide no change")] private Vector3 Position = new Vector3(0,0,0);
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action),InterventionActions.Edit_OOI_Transform), Tooltip("Additive, value of 0 will provide no change, rotation is always clockwise")] private Vector3 Rotation = new Vector3(0,0,0);
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action),InterventionActions.Edit_OOI_Transform), Tooltip("Additive, value of 0 will provide no change")] private Vector3 Scale = new Vector3(0,0,0);
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action),InterventionActions.Edit_OOI_Transform)] private InterventionType interventionType = InterventionType.Instant;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(showNumberOfFrames)), MinValue(2f)] private int numberOfFrames = 10;

        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Object that will approach the OOI")] private Object_Of_Interest ApproachingOOI = null;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Updates Target Location everyframe so the Object always approaches the OOI instead of approaching the OOI's original location")] private bool isTrackingTarget = false;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Speed of the Object approaching the OOI in meters per second")] private float ApproachSpeed = 2.5f;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Distance the Object approaches the OOI from in meters")] private float ApproachDistance = 5f;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Approach direction relative to the OOI, e.g. 0,0,0 is directly infront")] private Vector3 ApproachDirection = new Vector3();
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Use this to adjust the Target Location, e.g. Object approaches the point 10cm above the OOI")] private Vector3 ApproachOffset = new Vector3();
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Once the Object is within this distance (meters) to the Target Location the Intervention ends")] private float ApproachProximity = 0.1f;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(Action), InterventionActions.ApproachingObject), Tooltip("Duration (seconds) before the Object is removed from view")] private float ApproachProximityDuration = 1f;

        [Space]
        [SerializeField, DisableIf(nameof(isLocked))] private InterventionTriggers interventionTrigger = InterventionTriggers.Duration;
        [SerializeField, DisableIf(nameof(isLocked)), MinValue(0f), ShowIf(nameof(interventionTrigger), InterventionTriggers.Duration)] private float triggerDelayDuration = 15f;
        [SerializeField, DisableIf(nameof(isLocked)), HideIf(nameof(interventionTrigger),InterventionTriggers.Duration)] private Object_Of_Interest triggerOOI = null;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(interventionTrigger),InterventionTriggers.Proximity), MinValue(0f)] private float triggerProximity = 3f;
        [SerializeField, DisableIf(nameof(isLocked)), MinValue(0f), ShowIf(nameof(interventionTrigger), InterventionTriggers.Proximity)] private float triggerProximityDuration = 0.5f;
        [SerializeField, DisableIf(nameof(isLocked))] private bool isRepeatable = false;
        #endregion

        #region Getters
        public string Get_Intervention_Name() { return this.name; }
        public InterventionActions Get_Action() { return Action; }
        public Object_Of_Interest Get_TargetOOI() { return TargetOOI; }
        public Vector3 Get_Position() { return Position; }
        public Vector3 Get_Rotation() { return Rotation; }
        public Vector3 Get_Scale() { return Scale; }
        public Object_Of_Interest Get_ApproachingOOI() { return ApproachingOOI; }
        public bool Get_isTrackingTarget() { return isTrackingTarget; }
        public float Get_ApproachSpeed() { return ApproachSpeed; }
        public float Get_ApproachDistance() { return ApproachDistance; }
        public Vector3 Get_ApproachDirection() { return ApproachDirection; }
        public Vector3 Get_ApproachOffset() { return ApproachOffset; }
        public float Get_ApproachProximity() { return ApproachProximity; }
        public float Get_ApproachProximityDuration() { return ApproachProximityDuration; }
        public Object_Of_Interest Get_TriggerOOI() { return triggerOOI; }
        public InterventionType Get_Intervention_Type() { return interventionType; }
        public int Get_NumberOfFrames() { return numberOfFrames; }
        public InterventionTriggers Get_Trigger() { return interventionTrigger; }
        public float Get_TriggerDelayDuration() { return triggerDelayDuration; }
        public float Get_TriggerProximity() { return triggerProximity; }
        public float Get_TriggerProximityDuration() { return triggerProximityDuration; }
        public bool Get_isRepeatable() {return isRepeatable; }

        public string Get_Intervention_Printout() 
        { 
            string targetID = "";
            string approachingOOI = "";
            
            if(triggerOOI == null) 
            { 
                targetID = "-1";
            } 
            else 
            { 
                targetID = triggerOOI.Get_Object_Of_Interest_ID().ToString();
            }

            if(ApproachingOOI == null) 
            {
                approachingOOI = "-1";
            }
            else 
            {
                approachingOOI = ApproachingOOI.Get_Object_Of_Interest_ID().ToString();
            }
                    
            return this.name + "," + Action.ToString() + "," + TargetOOI.Get_Object_Of_Interest_ID().ToString() + "," + Position.x.ToString() 
            + "," + Position.y.ToString() + "," + Position.z.ToString() + "," + Rotation.x.ToString() + "," + Rotation.y.ToString() + "," + Rotation.z.ToString() 
            + "," + Scale.x.ToString() + "," + Scale.y.ToString() + "," + Scale.z.ToString() + "," +  approachingOOI + "," + isTrackingTarget.ToString() 
            + "," + ApproachSpeed.ToString() + "," + ApproachDistance.ToString() + "," + ApproachDirection.x.ToString() + "," + ApproachDirection.y.ToString() 
            + "," + ApproachDirection.z.ToString() + "," + ApproachOffset.x.ToString() + "," + ApproachOffset.y.ToString() + "," + ApproachOffset.z.ToString() 
            + "," + ApproachProximity.ToString() + "," + ApproachProximityDuration.ToString() + "," + targetID + "," + interventionType.ToString() + "," + numberOfFrames.ToString() 
            + "," + interventionTrigger.ToString() + "," + triggerDelayDuration.ToString()  + "," + triggerProximity.ToString() + "," + triggerProximityDuration.ToString() + "," + isRepeatable.ToString(); 
        }
        #endregion

        #region Setters
        //
        //The following methods should only be used by the replay manager when it instantiates a new scriptable,
        //it uses these methods to recreate the Intervention saved in the data text files and allows it to be added to the new replay Trial.
        //
        public void Set_Intervention_Name(string name) { this.name = name; }
        public void Set_Action(InterventionActions action) { Action = action; }
        public void Set_TargetOOI(Object_Of_Interest OOI) { TargetOOI = OOI; }
        public void Set_Position(Vector3 pos) { Position = pos; }
        public void Set_Rotation(Vector3 rot) { Rotation = rot; }
        public void Set_Scale(Vector3 scale) { Scale = scale; }
        public void Set_ApproachingOOI(Object_Of_Interest OOI) { ApproachingOOI = OOI; }
        public void Set_isTrackingTarget(bool value) { isTrackingTarget = value; }
        public void Set_ApproachSpeed(float value) { ApproachSpeed = value; }
        public void Set_ApproachDistance(float value) { ApproachDistance = value; }
        public void Set_ApproachDirection(Vector3 rot) { ApproachDirection = rot; }
        public void Set_ApproachOffset(Vector3 pos) { ApproachOffset = pos; }
        public void Set_ApproachProximity(float value) { ApproachProximity = value; }
        public void Set_ApproachProximityDuration(float value) { ApproachProximityDuration = value; }
        public void Set_TriggerOOI(Object_Of_Interest OOI) { triggerOOI = OOI; }
        public void Set_InterventionType(InterventionType type) { interventionType = type; }
        public void Set_NumberOfFrames(int value) { numberOfFrames = value; }
        public void Set_Trigger(InterventionTriggers value) { interventionTrigger = value; }
        public void Set_TriggerDelayDuration(float value) { triggerDelayDuration = value; }
        public void Set_TriggerProximity(float value) { triggerProximity = value; }
        public void Set_TriggerProximityDuration(float value) { triggerProximityDuration = value; }
        public void Set_isRepeatable(bool isrepeat) { isRepeatable = isrepeat; }
        #endregion

        public void OnValidate()
        {
            if(Action == InterventionActions.Edit_OOI_Transform && interventionType == InterventionType.Over_Frames)
            {
                showNumberOfFrames = true;
            }
            else if (showNumberOfFrames == true)
            {
                showNumberOfFrames = false;
            }
        }

        private bool showNumberOfFrames = false;

        [Button]
        public virtual void Lock_Variables()
        {
            isLocked = !isLocked;
        }

        #region Editor Logic Variables
        [HideInInspector] public bool isLocked = false;
        #endregion
    }
}
