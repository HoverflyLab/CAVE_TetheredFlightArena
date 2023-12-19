//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

namespace TetheredFlight
{
    [CreateAssetMenu(fileName = "new_OOI_StaticObject", menuName = "ScriptableObjects/Objects of interest/Static Object", order = 1)]
    public class OOI_StaticObject : Object_Of_Interest
    {
        #region OOI_StaticObject Variables
        //[SerializeField, ReadOnly] 
        private ObjectOfInterestType type = ObjectOfInterestType.StaticOOI;
        #endregion

        #region Getters
        public override ObjectOfInterestType Get_Object_Of_Interest_Type() { return type; }
        public override void Set_Object_Of_Interest_Type(ObjectOfInterestType _type) { type = _type; EditorUtility.SetDirty(this); }
        #endregion
    }
}
