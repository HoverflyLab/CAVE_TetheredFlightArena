//Created By Raymond Aoukar
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetheredFlight
{
    public class EnumStorage : MonoBehaviour
    {
        void Start()
        {

        }
    }

    //This Script contains all of the public enums within this project
    #region Settings

    public enum StimulusType
    {
        Stimulus2D = 0, Scene = 1, RotatingScene = 2
    }

    public enum TrialCompletionType
    {
        Duration = 0, Duration_Or_Proximity = 1
    }

    public enum TrialCompletedBy
    {
        Unknown = 0, Duration = 1, Proximity = 2, Skip = 3, Stop = 4
    }

    public enum ObjectOfInterestType
    {
        StaticOOI = 0, DynamicOOI = 1
    }

    public enum Dynamic_OOI_Behaviours
    {
        Hover = 0, Patrol = 1, Chase = 2, Flee = 3, None = 4, StayRelative = 5
    }

    public enum EncounterType
    {
        Duration = 0, Proximity = 1
    }

    public enum StimulusRole
    {
        Default_Stimulus = 0, Pre_Stimulus = 1, Post_Stimulus = 2
    }

    public enum Stimulus
    {
        Blank = 0, SineGrating = 1
    }
    #endregion

    #region Flight
    public enum Lean
    {
        Right = 0, Left = 1
    }

    public enum WingPosition
    {
        Forward = 0, Backward = 1
    }

    public enum YawMethod
    {
        Constant = 0, Linear = 1, Variable = 2//, Sigmoid = 3
    }

    public enum ThrustMethod
    {
        Constant = 0, Linear = 1, Variable = 2//, Sigmoid = 3
    }
    #endregion

    #region Data (saving, loading)
    public enum commands
    {
        Start = 0, Skip = 1, Stop = 2, Print_Data = 3
    }

    public enum ReplayActions
    {
        Edit_OOI_Transform = 0, Hide_OOI = 1, Show_OOI = 2, ApproachingObject = 3, OOI_Encounter_Triggered = 4,
    }

    public enum LoopStatus
    {
        BetweenTrials = 0, PreStim = 1, LoopClosed = 2, PostStim = 3
    }
    #endregion

    #region Interpolation
    public enum OOI_InterpolationOptions
    {
        //Tethered Animal Avatar Transform
        Position_X = 0, Position_Y = 1, Position_Z = 2,
        Rotation_X = 3, Rotation_Y = 4, Rotation_Z = 5, 
        Scale_X = 6, Scale_Y = 7, Scale_Z = 8, Dynamic_OOI_Speed = 9
    }

    public enum Settings_InterpolationOptions
    {
        Trial_Duration = 0
    }

    public enum InterpolationMethod
    {
        Linear = 0, Log10 = 1
    }
    #endregion

    #region Interventions
    public enum InterventionActions
    {
        Edit_OOI_Transform = 0, Hide_OOI = 1, Show_OOI = 2, ApproachingObject = 3,
    }

    public enum InterventionTriggers
    {
        Duration = 0, Proximity = 1 //, LineofSight = 2
    }

    public enum ProximityFlag 
    {
        False = 0, True = 1, Triggered = 2
    }

    public enum InterventionType
    {
        Instant = 0, Over_Frames = 1 //, Over_Time = 2
    }
    #endregion

    public enum ComparisonType
    {
        Equals = 1, NotEqual = 2, GreaterThan = 3, SmallerThan = 4, SmallerOrEqual = 5, GreaterOrEqual = 6
    }

    public enum DisablingType
    {
        ReadOnly = 2, DontDraw = 3
    }
}
