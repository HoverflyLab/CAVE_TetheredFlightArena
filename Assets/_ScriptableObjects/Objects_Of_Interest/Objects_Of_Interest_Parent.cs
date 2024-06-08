//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

namespace TetheredFlight
{
    //This Script is used to create the Objects_Of_Interest_Parent Scriptable Object, which maintains a list of all Objects of Interest being used within the Project.
    //Order in list = Unique ID, where the newest Object of Interest's Unique ID will equal the current uniqueIDCounter.
    //List and UniqueIDCounter can be reset by removing the ", ReadOnly" text from line 16 and 17, then clicking on the Objects_Of Interest_Parent Scriptable Object.
    public class Objects_Of_Interest_Parent : ScriptableObject
    {
        #region OOI_Parent Variables
        [SerializeField, ReadOnly] private List<ObjectOfInterest> List_of_OOI = new List<ObjectOfInterest>();
        [SerializeField, ReadOnly] private int uniqueIDCounter = 0;
        #endregion

        public void Add_OOI_To_List(ObjectOfInterest _object)
        {
            if(List_of_OOI.Contains(_object) == false)
            {
                List_of_OOI.Add(_object);
                AssignID(_object);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void Remove_OOI_From_List(ObjectOfInterest _object)
        {
            if (List_of_OOI.Contains(_object) == true)
            {
                List_of_OOI.Remove(_object);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public int Get_Current_ID_From_List(ObjectOfInterest _object)
        {
            if (List_of_OOI.Contains(_object) == true)
            {
                return List_of_OOI.IndexOf(_object);
            }
            return -1;
        }

        private void AssignID(ObjectOfInterest _object)
        {
            if (_object.GetID() == -1)
            {
                _object.SetID(uniqueIDCounter);
                uniqueIDCounter++;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        //[Button]
        //private void OverrideAllID()
        //{
        //    for(int i = 0; i < List_of_OOI.Count; i++)
        //    {
        //        List_of_OOI[i].SetID(i);
        //    }
        //}
    }
}
