//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace TetheredFlight
{
    //This Script is used to create the Objects_Of_Interest Scriptable Objects, which each store an integer corresponding to an ObjectOfInterest's unique ID.
    //By using this Scriptable Object to store the appropriate ID then naming it after the same ObjectOfInterest allows you to quickly assign 
    //the ObjectOfInterest you wish to manipulate by name instead of remembering the unique ID for each ObjectOfInterest.

    //one of these Scriptable Objects should exist for every ObjectOfInterest in the project.
    public class Object_Of_Interest : ScriptableObject
    {
        #region OOI Variables
        [SerializeField, DisableIf(nameof(isLocked)), Header("The Unique ID of the Object of Interest you wish to manipulate"), MinValue(-1)]
        private int OOI_ID = -1;
        #endregion

        #region Getters
        public string Get_Object_Of_Interest_Name() { return this.name; }
        public virtual ObjectOfInterestType Get_Object_Of_Interest_Type() { return default; }
        public int Get_Object_Of_Interest_ID() { return OOI_ID; }
        #endregion

        #region Setters
        //
        //The following methods should only be used by the replay manager when it instantiates a new scriptable,
        //it uses these methods to recreate the Object of Interest saved in the data text files and allows the instantiated scriptable to be added to other scriptables such as interpolations.
        //
        public void Set_Object_Of_Interest_Name(string name) { this.name = name; }
        public virtual void Set_Object_Of_Interest_Type(ObjectOfInterestType _type) {}
        public void Set_Object_Of_Interest_ID(int id) {OOI_ID = id; }
        #endregion

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
