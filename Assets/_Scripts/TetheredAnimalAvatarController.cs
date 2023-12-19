//Created By Raymond Aoukar
using UnityEngine;

namespace TetheredFlight
{
    //This script is responsible for moving the Tethered Animal Avatar based on user settings and the output from the DataProcessor script.
    //The user can can use the profile settings to manipulate how this data is processed. E.g. Constant, Linear or Variable yaw and thrust.
    //The script calculates Wing Beat Amplitude Difference (WBAD) and Sum (WBAS) in order to update the Tethered Animal Avatar's rotation and position within the scene.
    public class TetheredAnimalAvatarController : MonoBehaviour
    {
        public static TetheredAnimalAvatarController Instance = null;

        [SerializeField] private bool invert_Turn = false;
        private float YPos = 0f;
        private bool isClosedLoop = false;
        private bool isManualControl = false;
        private float manualSpeed = 2f;

        #region Yaw and Thrust Variables
        private float yaw_dps = 0f;
        private float thrust_mps = 0f;
        private float rightWBA = 0f;
        private float leftWBA = 0f;

        private float WBAD = 0f;
        private float min_WBAD = 0f;
        private float max_WBAD = 180f;

        private float WBAS = 0f;
        private float min_WBAS = 0f;
        private float max_WBAS = 360f;

        private YawMethod yaw_Method = YawMethod.Constant;
        private float minYaw = 0f;
        private float yawAtMidpoint = 0f;
        private float maxYaw = 0f;
        private float midpoint_WBAD = 0f;
        private Sigmoid_Values yaw_Sigmoid_Values = default;

        private ThrustMethod thrust_Method = ThrustMethod.Constant;
        private float minThrust = 0f;
        private float thrustAtMidpoint = 0f;
        private float maxThrust = 0f;
        private float midpoint_WBAS = 0f;
        #endregion

        private int uniquePacket = 0;
        private int duplicatePacket = 0;
        private int frameCount = 0;

        private float rotationValue = 0;
        private float rotationFinishTime = 0;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Error: There can only be one TetheredAnimalAvatarController");
                Destroy(this.gameObject);
            }

            TrialManager.Instance.CloseLoop += CloseLoop;
            TrialManager.Instance.OpenLoop += OpenLoop;

            YPos = transform.position.y;
            isManualControl = UIManager.Instance.Get_isManualControl();
        }

        // Start is called before the first frame update
        void Start()
        {
            Get_Sigmoid_Values();
        }
        
        //Currently these values are not used, but they will be necessary in the future for implementing the Sigmoid functions to get Yaw / Thrust values from Wing Beat Amplitude.
        private void Get_Sigmoid_Values()
        {
            SettingsManager.Instance.Update_Tethered_Animal_Controller_YawThrust_Values();

            if(UIManager.Instance.Get_Sex_Of_Animal().Equals("Male"))
            {
                yaw_Sigmoid_Values = SettingsManager.Instance.Get_Male_Yaw_SigmoidValues();
            }
            else if(UIManager.Instance.Get_Sex_Of_Animal().Equals("Female"))
            {
                yaw_Sigmoid_Values = SettingsManager.Instance.Get_Female_Yaw_SigmoidValues();
            }
            else //default, when there is no selection
            {
                yaw_Sigmoid_Values = SettingsManager.Instance.Get_Male_Yaw_SigmoidValues();
            }
        }

        void FixedUpdate()
        {
            if(rotationFinishTime > Time.time)
            {
                //rotation (Yaw). Runs during pre and post stimulus if the world has been set to rotate around the Tethered Animal
                //Instead of rotating the world, the Tethered Animal is rotated instead, achieves the same effect.
                this.gameObject.transform.eulerAngles += new Vector3(0, rotationValue * Time.deltaTime, 0);
            }
        }

        void Update()
        {
            if(isClosedLoop == true)
            {
                frameCount++;

                if(isManualControl == false) 
                {
                    //Rotation (Yaw)
                    this.gameObject.transform.eulerAngles += new Vector3(0,(yaw_dps * Time.deltaTime),0);

                    //Position (Thrust)
                    this.gameObject.transform.position += this.gameObject.transform.forward * thrust_mps * Time.deltaTime;

                    //lock height to Ypos
                    this.gameObject.transform.position = new Vector3(transform.position.x, YPos, transform.position.z);
                }
                else //Accept keyboard commands to control the Tethered Animal Avatar 
                {
                    if (Input.GetKey(KeyCode.W)) 
                    {
                        this.gameObject.transform.position += this.gameObject.transform.forward * manualSpeed * Time.deltaTime;
                    }

                    if (Input.GetKey(KeyCode.S))
                    {
                        this.gameObject.transform.position -= this.gameObject.transform.forward * manualSpeed * Time.deltaTime;
                    }

                    if (Input.GetKey(KeyCode.A))
                    {
                        this.gameObject.transform.eulerAngles -= new Vector3(0,(manualSpeed * Time.deltaTime * 30),0);
                    }

                    if (Input.GetKey(KeyCode.D))
                    {
                        this.gameObject.transform.eulerAngles += new Vector3(0,(manualSpeed * Time.deltaTime * 30),0);
                    }
                }
            }
        }

        //Called during Pre and Post Stimulus in order to set how long and how fast the Tethered Animal Avatar should rotate
        public void StartRotation(int numberOfRevolutions, float duration)
        {
            if(duration > 0f)
            {
                rotationValue = (360f * numberOfRevolutions) / duration;
                rotationFinishTime = Time.time + duration;
            }
        }

        public void Update_Angles(float right_WBA, float left_WBA)
        {
            if(rightWBA == right_WBA && leftWBA == left_WBA)
            {
                duplicatePacket++;
            }
            else
            {
                rightWBA = right_WBA;
                leftWBA = left_WBA;
                uniquePacket++;
            }

            WBAS = rightWBA + leftWBA;

            if(invert_Turn == false)
            {
                WBAD = leftWBA - rightWBA;
            }
            else
            {
                WBAD = rightWBA - leftWBA;
            }

            if(yaw_Method != YawMethod.Constant)
            {
                Calculate_Yaw();
            }

            if(thrust_Method != ThrustMethod.Constant)
            {
                Calculate_Thrust();
            }
        }

        private void Calculate_Yaw()
        {
            switch(yaw_Method)
            {                
                //Constant case can be ignored as yaw_dps is initially set to the constant value and
                //We don't run Calculate_Yaw() if YawMethod == constant
                case YawMethod.Linear:

                    //yaw deg/s = minimum Yaw + ((current Wing Beat Amplitude Difference - minimum WBAD) * (amount of yaw per WBAD)
                    yaw_dps = minYaw + ((Mathf.Abs(WBAD) - min_WBAD) * ((maxYaw - minYaw)/(max_WBAD - min_WBAD)));
                    break;
                
                case YawMethod.Variable:

                    if(Mathf.Abs(WBAD) >= midpoint_WBAD)
                    {
                        //yaw deg/s = midpoint yaw + ((current Wing Beat Amplitude Difference - mid point WBAD) * (amount of yaw per WBAD))
                        yaw_dps = yawAtMidpoint + ((Mathf.Abs(WBAD) - midpoint_WBAD) * ((maxYaw - yawAtMidpoint)/(max_WBAD - midpoint_WBAD))); 
                    }
                    else
                    {
                        //yaw deg/s = minimum Yaw + ((current Wing Beat Amplitude Difference - minimum WBAD) * (amount of yaw per WBAD))
                        yaw_dps = minYaw + ((Mathf.Abs(WBAD) - min_WBAD) * ((yawAtMidpoint - minYaw)/(midpoint_WBAD - min_WBAD)));  
                    }
                    break;
                
                default: 
                    Debug.LogWarning("Warning: If using Sigmoid as a Yaw_Method that functionality has not been implemented");
                    Debug.LogWarning("Warning: Code should not reach here");
                    break;
            }

            //override yaw if WBAD is below the minimum
            if(Mathf.Abs(WBAD) < min_WBAD)
            {
                yaw_dps = minYaw;
            }

            //override yaw if WBAD is above the maximum
            if(Mathf.Abs(WBAD) > max_WBAD)
            {
                yaw_dps = maxYaw;
            }

            //Since we used the absolute value during calculations we need to give yaw_dps a negative value if WBAD was negative.
            if(WBAD < 0)
            {
                yaw_dps = -yaw_dps;
            }
        }

        private void Calculate_Thrust()
        {
            switch(thrust_Method)
            {   
                //Constant case can be ignored as thrust_mps is initially set to the constant value and
                //We don't run Calculate_Thrust() if ThrustMethod == constant
                case ThrustMethod.Linear:

                    //thrust m/s = minimum thrust + ((current Wing Beat Amplitude Sum - minimum WBAS) * (amount of thrust per WBAS))
                    thrust_mps = minThrust + ((WBAS - min_WBAS) * ((maxThrust - minThrust)/(max_WBAS - min_WBAS)));
                    break;
                
                case ThrustMethod.Variable:

                    if(WBAS >= midpoint_WBAS)
                    {
                        //thrust m/s = midpoint thrust + ((current Wing Beat Amplitude Sum - mid point WBAS) * (amount of thust per WBAS))
                        thrust_mps = thrustAtMidpoint + ((WBAS - midpoint_WBAS) * ((maxThrust - thrustAtMidpoint)/(max_WBAS - midpoint_WBAS)));
                    }
                    else
                    {
                        //thrust m/s = minimum thrust + ((current Wing Beat Amplitude Sum - minimum WBAS) * (amount of thust per WBAS))
                        thrust_mps = minThrust + ((WBAS - min_WBAS) * ((thrustAtMidpoint - minThrust)/(midpoint_WBAS - min_WBAS))); 
                    }
                    break;
                
                default: 
                    Debug.LogWarning("Warning: If using Sigmoid as a Thrust_Method that functionality has not been implemented");
                    Debug.LogWarning("Warning: Code should not reach here");
                    break;
            }

            //override thrust if WBAS is below the minimum
            if(WBAS < min_WBAS)
            {
                thrust_mps = minThrust;
            }

            //override thrust if WBAS is above the maximum
            if(WBAS > max_WBAS)
            {
                thrust_mps = maxThrust;
            }
        }

        //Used by the trial manger to interpolate Y position of the tethered animal
        public void Set_Y_Pos(float interpValue)
        {
            YPos += interpValue;
        }

        //Settings Manager uses this function to load in the proper values inputted by the experimenter
        public void Set_Yaw_Values(YawMethod method, float yaw, float minyaw, float yawatmidpoint, float maxyaw, float midpoint_wbad, float minWBAD, float maxWBAD)
        {
            yaw_Method = method;
            yaw_dps = 0;

            if(yaw_Method == YawMethod.Constant)
            {
                yaw_dps = yaw;
            }

            minYaw = minyaw;
            yawAtMidpoint = yawatmidpoint;
            maxYaw = maxyaw;
            min_WBAD = minWBAD;
            midpoint_WBAD = midpoint_wbad;
            max_WBAD = maxWBAD;
        }

        //Settings Manager uses this function to load in the proper values inputted by the experimenter 
        public void Set_Thrust_Values(ThrustMethod method, float thrust, float minthrust, float thrustatmidpoint, float maxthrust, float midpoint_wbas, float minWBAS, float maxWBAS)
        {
            thrust_Method = method;
            thrust_mps = 0;

            if(thrust_Method == ThrustMethod.Constant)
            {
                thrust_mps = thrust;
            }

            minThrust = minthrust;
            thrustAtMidpoint = thrustatmidpoint;
            maxThrust = maxthrust;
            min_WBAS = minWBAS;
            midpoint_WBAS = midpoint_wbas;
            max_WBAS = maxWBAS;
        }

        //Called via action
        private void CloseLoop()
        {
            isClosedLoop = true;
        }

        //Called via action
        private void OpenLoop()
        {
            isClosedLoop = false;
        }

        public void Print_Stats()
        {
            print("Frames during closed loop = " + frameCount);
            print("Unique Packets during closed loop = " + uniquePacket);
            print("duplicate Packets during closed loop = " + duplicatePacket);
        }

        private void OnDestroy()
        {
            TrialManager.Instance.CloseLoop -= CloseLoop;
            TrialManager.Instance.OpenLoop -= OpenLoop;            
        }
    }
}