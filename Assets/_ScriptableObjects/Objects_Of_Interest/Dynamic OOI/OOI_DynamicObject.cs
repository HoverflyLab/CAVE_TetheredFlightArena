//Created By Raymond Aoukar 2/12/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

namespace TetheredFlight
{
    [CreateAssetMenu(fileName = "new_OOI_DynamicObject", menuName = "ScriptableObjects/Objects of interest/DynamicObject", order = 2)]
    public class OOI_DynamicObject : Object_Of_Interest
    {
        #region OOI_Hoverfly Variables
        //[SerializeField, ReadOnly] 
        private ObjectOfInterestType type = ObjectOfInterestType.DynamicOOI;
        #endregion

        #region Getters
        public override ObjectOfInterestType Get_Object_Of_Interest_Type() { return type; }
        public override void Set_Object_Of_Interest_Type(ObjectOfInterestType _type) 
        { 
            type = _type;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
        #endregion
    }
}
