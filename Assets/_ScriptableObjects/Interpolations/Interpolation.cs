//Created By Raymond Aoukar 05/07/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This Script is used to create the Interpolation Scriptable Objects, which contain the data required to setup an Interpolation.
    //Interpolations are used to alter the starting position, rotation, scale or speed of an ObjectOfInterest across an entire sequence.
    //Changes made via interpolation occur at the start of each trial in the sequence.
    [CreateAssetMenu(fileName = "new_Intepolation", menuName = "ScriptableObjects/Interpolation", order = 7)]
    public class Interpolation : ScriptableObject
    {
        #region Interpolation Variables
        [SerializeField, DisableIf(nameof(isLocked))] bool InterpolateObject = true;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(InterpolateObject))] Object_Of_Interest objectToInterpolate = null;
        [SerializeField, DisableIf(nameof(isLocked)), ShowIf(nameof(InterpolateObject))] OOI_InterpolationOptions objectInterpolationOption = OOI_InterpolationOptions.Position_X;
        [SerializeField, DisableIf(nameof(isLocked)), HideIf(nameof(InterpolateObject))] Settings_InterpolationOptions settingsInterpolationOption = Settings_InterpolationOptions.Trial_Duration;
        [SerializeField, DisableIf(nameof(isLocked)), MinMaxSlider(-100, 100)] Vector2 rangeOfInterpolation = new Vector2(0, 10);
        [SerializeField, DisableIf(nameof(isLocked))] InterpolationMethod interpolationMethod = InterpolationMethod.Linear;
        #endregion

        #region Getters
        public string Get_Interpolation_Name() { return this.name; }
        public bool Is_Interpolating_Object() { return InterpolateObject; }
        public Object_Of_Interest Get_Object_To_Interpolate() { return objectToInterpolate; }
        public OOI_InterpolationOptions Get_Object_Interpolation_Option() { return objectInterpolationOption; }
        public Settings_InterpolationOptions Get_Settings_Interpolation_Option() { return settingsInterpolationOption; }
        public Vector2 Get_Range_Of_Interpolation() { return rangeOfInterpolation; }
        public InterpolationMethod Get_Interpolation_Method() { return interpolationMethod; }

        public string Get_Interpolation_Printout() { if(InterpolateObject == true && objectToInterpolate == null) { return "Error, No OOI to Interpolate"; } else { return this.name + "," + InterpolateObject.ToString() 
                                                    + "," + objectToInterpolate.Get_Object_Of_Interest_ID().ToString() + "," + objectInterpolationOption.ToString() + "," + settingsInterpolationOption.ToString()
                                                    + "," + rangeOfInterpolation.x + "," + rangeOfInterpolation.y + "," + interpolationMethod.ToString();} }
        #endregion
        
        #region Setters
        //
        //The following methods should only be used by the replay manager when it instantiates a new scriptable,
        //it uses these methods to recreate the Interpolation saved in the data text files and allows it to be added to the new replay Sequence.
        //
        public void Set_Interpolation_Name(string name) { this.name = name; }
        public void Set_Is_Interpolation_Object(bool value) { InterpolateObject = value; }
        public void Set_Object_To_Interpolate(Object_Of_Interest OOI) { objectToInterpolate = OOI; }
        public void Set_Object_Interpolation_Option(OOI_InterpolationOptions interpOption) { objectInterpolationOption = interpOption; }
        public void Set_Settings_Interpolation_Option(Settings_InterpolationOptions interpOption) { settingsInterpolationOption = interpOption; }
        public void Set_Range_Of_Interpolation(Vector2 range) { rangeOfInterpolation = range; }
        public void Set_Interpolation_Method(InterpolationMethod interpMethod) { interpolationMethod = interpMethod; }
        #endregion

        //Ensures Interpolations cant be used to set Error prone values
        public void OnValidate()
        {
            if(InterpolateObject == true)
            {
                //Scale cant be set to less than or equal to 0
                if(objectInterpolationOption == OOI_InterpolationOptions.Scale_X && rangeOfInterpolation.x <= 0f)
                {
                    rangeOfInterpolation.x = 1f;
                }

                if(objectInterpolationOption == OOI_InterpolationOptions.Scale_Y && rangeOfInterpolation.x <= 0f)
                {
                    rangeOfInterpolation.x = 1f;
                }

                if(objectInterpolationOption == OOI_InterpolationOptions.Scale_Z && rangeOfInterpolation.x <= 0f)
                {
                    rangeOfInterpolation.x = 1f;
                }
            }
            else
            {
                //Trial duration cant be set to less than 1 second
                if(settingsInterpolationOption == Settings_InterpolationOptions.Trial_Duration && rangeOfInterpolation.x < 1f)
                {
                    rangeOfInterpolation.x = 1f;
                }
            }
        }

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
