//Created By Raymond Aoukar 
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace TetheredFlight
{
    //This Script is responsible for all of the logic behind the User Interface interactions
    public class UIManager : MonoBehaviour
    {
        static public UIManager Instance = null;
        [SerializeField] private bool isActivatingStimulusScreens = false;
        
        #region Menus
        [Header("Menus")]
        [SerializeField] private GameObject mainMenu = null;
        [SerializeField] private GameObject stimulusMenu = null;
        [SerializeField] private GameObject replayMenu = null;
        [SerializeField] private GameObject nextSequenceMenu = null;
        [SerializeField] private GameObject longitudinalAxisMenu = null;
        private LongitudinalAxisMenu longitudinalAxisScript = null;
        private GameObject currentMenu = null;
        private List<GameObject> previousMenus = new List<GameObject>();
        #endregion

        #region TextFields
        [Header("TextFields MainMenu")]
        [SerializeField] private TMP_Dropdown sexOfAnimalDropdown = null;
        [SerializeField] private GameObject animalFolderTextField = null;

        [Header("TextFields StimulusMenu")]
        [SerializeField] private TextMeshProUGUI sequenceNumber = null;
        [SerializeField] private TextMeshProUGUI trialNumber = null;
        [SerializeField] private TextMeshProUGUI preStimulusDuration = null;
        [SerializeField] private TextMeshProUGUI trialDuration = null;
        [SerializeField] private TextMeshProUGUI postStimulusDuration = null;
        [SerializeField] private TextMeshProUGUI currentlyShowing = null;
        #endregion

        #region Buttons
        [Header("Buttons")]
        [SerializeField] private GameObject longitudinalAxisButton = null;
        private bool isAutomaticLongitudinalAxis = true;
        #endregion

        private bool isManualControl = false;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one UI Manager");
                Destroy(this.gameObject);
            } 

            if(mainMenu != null)
            {
                currentMenu = mainMenu;
                previousMenus.Add(mainMenu);
            }
            else
            {
                Debug.LogError("Error: Main Menu GameObject is null");
            }

            longitudinalAxisScript = longitudinalAxisMenu.GetComponent<LongitudinalAxisMenu>();
        }

        //Called via Button
        public void Start_Sequence()
        {
            //If displaying stimulus (this is a real Trial)
            if(isActivatingStimulusScreens == true)
            {
                //If we have chosen to manually set the longitudinal axis and the values have not been set show popup
                if(isAutomaticLongitudinalAxis == false && longitudinalAxisScript.ArePointsSet() == false)
                {
                    #if UNITY_EDITOR
                        EditorUtility.DisplayDialog("Warning", "You have not manually set the longitudinal Axis", "Ok"); 
                        return;
                    #endif
                }
            }

            DirectoryManager.Instance.Initialise_Directory();
            
            #if UNITY_EDITOR
                CreateStimulusScreens.Focus_On_Stimulus();
            #endif
            Open_Menu(stimulusMenu);

            SequenceManager.Instance.Reset_Number_Of_Trials();
            SequenceManager.Instance.Start_First_Sequence();
        }
        
        //Called via Button
        public void Load_Replay()
        {
            ReplayManager.Instance.Activate_Replay_Manager();
            Open_Menu(replayMenu);
            ReplayManager.Instance.LocateTrialFolder();    
        }

        //Called via Button
        public void Set_LongitudinalAxis()
        {
            Open_Menu(longitudinalAxisMenu);  
        }

        //Called via Button
        public void Start_Replay()
        {
            //Interupts Default Stimulus
            StimulusManager.Instance.interupt_Stimulus();
            
            ReplayManager.Instance.Start_Replay();
        }

        //Called via Button
        public void Skip()
        {
            TrialManager.Instance.SkipTrial();
        }

        //Called via Button
        public void Stop()
        {
            TrialManager.Instance.StopTrial();
        }

        //Called via Button
        public void Show_LongitudinalAxisButton_Toggle()
        {
            isAutomaticLongitudinalAxis = !isAutomaticLongitudinalAxis;
            longitudinalAxisButton.SetActive(!isAutomaticLongitudinalAxis);
        }

        //Called via Button
        public void Set_ManualControl_Toggle()
        {
            isManualControl = !isManualControl;
        }

        public void Previous_Menu()
        {
            currentMenu.SetActive(false);
            currentMenu = previousMenus[previousMenus.Count-2];
            previousMenus.RemoveAt(previousMenus.Count-1);
            currentMenu.SetActive(true);
        }

        private void Open_Menu(GameObject menu_ToOpen)
        {
            previousMenus.Add(currentMenu);
            currentMenu.SetActive(false);
            currentMenu = menu_ToOpen;
            currentMenu.SetActive(true);
        }

        public void Open_Next_Sequence_Menu()
        {
            Open_Menu(nextSequenceMenu);
        }

        public void Update_Stimulus_Menu(float seqNo, float totalSeq, float trialNo, float totalTrials, float preStimDuration, float trialDuration, float postStimDuration)
        {
            sequenceNumber.text = seqNo.ToString() + "/" + totalSeq.ToString();
            trialNumber.text = trialNo.ToString() + "/" + totalTrials.ToString();
            preStimulusDuration.text = preStimDuration.ToString();
            this.trialDuration.text = trialDuration.ToString();
            postStimulusDuration.text = postStimDuration.ToString();
        }

        public void Update_Showing(string newtext)
        {
            currentlyShowing.text = newtext;
        }

        //Assumes scene 0 is Main Menu (loads managers)
        public bool Is_Menu_Scene()
        {
            if(SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(0))
            {
                return true;
            }
            return false;
        }

        public string Get_Animal_Folder_Name()
        {
            return animalFolderTextField.GetComponent<InputField>().text + "_" + sexOfAnimalDropdown.options[sexOfAnimalDropdown.value].text;
        }

        public string Get_Sex_Of_Animal()
        {
            return sexOfAnimalDropdown.options[sexOfAnimalDropdown.value].text;
        }

        public bool Get_isActivating_Stimulus_Screens()
        {
            return isActivatingStimulusScreens;
        }

        public bool Get_isLongitudinalAxisCalc_Manual()
        {
            return isAutomaticLongitudinalAxis;
        }

        public void Set_isLongitudinalAxisCalc_Manual(bool val)
        {
            isAutomaticLongitudinalAxis = val;
        }

        public bool Get_isManualControl()
        {
            return isManualControl;
        }
    }
}
