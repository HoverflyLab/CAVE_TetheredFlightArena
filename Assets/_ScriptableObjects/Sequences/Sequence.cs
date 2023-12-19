//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This Script is used to create the Sequence Scriptable Objects, which are a container for all of the data required to start a sequence.
    //Scripts within the scene are given this data at runtime to help them function.
    //Sequences are used to play 1 trial after another without input from the experimenter.
    [CreateAssetMenu(fileName = "new_Sequence", menuName = "ScriptableObjects/Sequence", order = 2)]
    public class Sequence : ScriptableObject
    {
        #region Sequence Variables
        [SerializeField, DisableIf(nameof(isLocked)), Expandable] 
        private List<Trial> listOfTrials = new List<Trial>();

        //This variable is set to the correct value on Start_Sequence (this is to avoid a bug during replay)
        private int numberOfTrials = 0;

        [SerializeField, DisableIf(nameof(isLocked)), Expandable, Tooltip("Stimulus shown between Sequences")] private DefaultStimulus defaultStimulus = null;
        #endregion

        #region Getters
        public string Get_Sequence_Name() { return this.name; }
        public List<Trial> Get_List_Of_Trials() { return listOfTrials; }
        public int Get_Number_Of_Trials() { return numberOfTrials; }
        public DefaultStimulus Get_Default_Stimulus() { return defaultStimulus; }
        public List<Interpolation> Get_List_Of_Interpolations() { return list_Of_Interpolations; }

        public string Get_Sequence_Printout() { return this.name + "," + numberOfTrials.ToString();}
        #endregion

        #region Setters
        //
        //The following methods should only be used by the replay manager when it instantiates a new scriptable,
        //it uses these methods to recreate the Sequence saved in the data text files and allows the instantiated scriptable to be run through the Sequence / Trial manager.
        //
        public void Set_Sequence_Name(string name) { this.name = name; }
        public void Add_Replay_Trial(Trial replayTrial) { listOfTrials.Add(replayTrial); }
        public void Set_Number_Of_Trials(int num) { numberOfTrials = num; }
        public void Set_Default_Stimulus(DefaultStimulus defaultStim) { defaultStimulus = defaultStim; }
        public void Add_New_Interpolation(Interpolation interp) { list_Of_Interpolations.Add(interp); }
        #endregion

        [Button]
        private void Lock_Variables()
        {
            isLocked = !isLocked;
        }

        [Header("Interpolation"), Tooltip("Overrides the selected value for all trials in this sequence")]
        [SerializeField, DisableIf(nameof(isLocked)), Expandable] private List<Interpolation> list_Of_Interpolations = new List<Interpolation>();

        #region Editor Logic Variables
        private bool isLocked = false;
        #endregion
    }
}
