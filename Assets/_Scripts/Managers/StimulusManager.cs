//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TetheredFlight
{
    //This script is handles all of the Stimulus logic including the activation and deactivation of the Stimulus UI used to display 2D stimulus such as Sine Grating.
    public class StimulusManager : MonoBehaviour
    {
        public static StimulusManager Instance = null;

        [SerializeField] private GameObject Stimulus_2D_Screen = null;
        [SerializeField] private List<RawImage> List_of_2D_Displays = new List<RawImage>();
        [SerializeField] private List<Material> List_of_Materials = new List<Material>(); // SineGrating, 
        [SerializeField] private bool verbose = false;
        
        private Coroutine currentCoroutine = null;
        bool rotateSceneOnLoad = false;
        DefaultStimulus defaultStimulus = null;
        
        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one Stimulus Manager");
                Destroy(this.gameObject);
            }

            SceneManager.sceneLoaded += Default_Stimulus_Loaded;
        }

        #region Activate Stimulus
        public void Activate_Stimulus(StimulusType stimType, StimulusRole stimRole, Stimulus stimulus, Color32 newColor, int revolutions, float duration)
        {
            switch(stimType)
            {
                case StimulusType.Stimulus2D:
                    Activate_2D_Stimulus(stimRole, stimulus, newColor, duration);
                    break;
                case StimulusType.Scene:
                    Activate_Scene_Stimulus(stimRole, duration);
                    break;
                case StimulusType.RotatingScene:
                    Activate_And_Rotate_Scene_Stimulus(stimRole, duration, revolutions);
                    break;
                default:
                Debug.LogError("Error: Code should not reach here");
                    break;
            }
        }

        private void Activate_2D_Stimulus(StimulusRole stimRole, Stimulus stimulus, Color32 newColor, float duration = -1f)
        {
            if (verbose) { print("Stim Canvas Active"); }

            foreach(RawImage image in List_of_2D_Displays)
            {
                image.color = newColor;
                
                //Assign material based on stimulus
                switch(stimulus)
                {
                    case Stimulus.Blank:
                        image.material = null;
                        break;
                    
                    case Stimulus.SineGrating:
                        image.material = List_of_Materials[0];
                        break;
                    
                    default: 
                        Debug.LogError("Error: Code should not reach here, a stimulus must be chosen");
                        break;
                }
            }

            if(stimRole == StimulusRole.Default_Stimulus)
            {
                Stimulus_2D_Screen.SetActive(true);
            }
            else
            {
                Stimulus_2D_Screen.SetActive(true);
                currentCoroutine = StartCoroutine(Disable_Stimulus(stimRole, duration));
            }
        }

        private void Activate_Scene_Stimulus(StimulusRole stimRole, float duration)
        {
            currentCoroutine = StartCoroutine(Disable_Stimulus(stimRole, duration));
        }

        private void Activate_And_Rotate_Scene_Stimulus(StimulusRole stimRole, float duration, int revolutions)
        {
            TetheredAnimalAvatarController.Instance.StartRotation(revolutions, duration);
            currentCoroutine = StartCoroutine(Disable_Stimulus(stimRole, duration));
        }
        #endregion

        //Interupts the Coroutine so completion is never triggered
        public void interupt_Stimulus()
        {
            if (verbose) { print("Interupt Stimulus"); }

            if(currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }

            Stimulus_2D_Screen.SetActive(false);
        }

        IEnumerator Disable_Stimulus(StimulusRole stimRole, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);

            if(stimRole == StimulusRole.Pre_Stimulus)
            {
                if (verbose) { print("Pre Stimulus Complete"); }

                Stimulus_2D_Screen.SetActive(false);
                TrialManager.Instance.Start_Trial();
            }

            if(stimRole == StimulusRole.Post_Stimulus)
            {
                if (verbose) { print("Post Stimulus Complete"); }

                bool isNextPreStim2D = SequenceManager.Instance.Is_Next_Trial_Pre_Stimulus_2D();

                //If next trial has a 2D Pre Stimulus leave the screen active (this stops the flickering which occurs when the screen is deactivated and then reactivated)
                if(isNextPreStim2D == false)
                {
                    Stimulus_2D_Screen.SetActive(false);
                }
                
                TrialManager.Instance.Post_Stim_Complete();
                SequenceManager.Instance.Next_Trial();
            }
        }

        //Plays before a sequence is started as well as between sequences
        public void Activate_Default_Stimulus()
        {
            defaultStimulus = SequenceManager.Instance.Get_Default_Stimulus();

            switch(defaultStimulus.Get_Stimulus_Type())
            {
                case StimulusType.Stimulus2D:
                    Activate_2D_Stimulus(StimulusRole.Default_Stimulus, defaultStimulus.Get_Stimulus(), defaultStimulus.Get_Stimulus_Color());
                    defaultStimulus = null;
                    break;

                case StimulusType.Scene:
                    rotateSceneOnLoad = false;
                    SceneManager.LoadScene(defaultStimulus.Get_Scene_Name());
                    break;

                case StimulusType.RotatingScene:
                    rotateSceneOnLoad = true;
                    SceneManager.LoadScene(defaultStimulus.Get_Scene_Name());
                    break;

                default:
                    Debug.LogError("Error: Code should not reach here");
                    break;
            }
        }

        private void Default_Stimulus_Loaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            //Only run if this is the specific scene loaded by Activate_Default_Stimulus()
            if(defaultStimulus != null && scene.name == defaultStimulus.Get_Scene_Name())
            {
                if(rotateSceneOnLoad == true)
                {
                    if(DefaultStimulusController.Instance != null)
                    {
                        DefaultStimulusController.Instance.StartRotation(defaultStimulus.Get_Seconds_Per_Revolution());
                    }
                    else
                    {
                        Debug.LogError("Error: Could not find the DefaultStimulusController script, The Scene you are trying to use as a Default Stimulus must contain this script");
                    }
                    
                    rotateSceneOnLoad = false;
                }

                defaultStimulus = null;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= Default_Stimulus_Loaded;
        }
    }
}
