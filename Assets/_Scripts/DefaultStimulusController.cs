//Created By Raymond Aoukar 30/09/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //Provides the desired movement (rotation) for the duration of the Default Stimulus
    public class DefaultStimulusController : MonoBehaviour
    {
        public static DefaultStimulusController Instance = null;

        private bool StimulusRotation = false;
        private float rotationValue = 0;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Error: There can only be one DefaultStimulusController");
                Destroy(this.gameObject);
            }
        }

        void FixedUpdate()
        {
            if(StimulusRotation == true)
            {
                //Rotation (Yaw)
                this.gameObject.transform.eulerAngles += new Vector3(0, rotationValue * Time.deltaTime, 0);
            }
        }

        public void StartRotation(float secondsPerRevolution)
        {
            rotationValue = 360f * (1f / secondsPerRevolution);
            StimulusRotation = true;
        }
    }
}
