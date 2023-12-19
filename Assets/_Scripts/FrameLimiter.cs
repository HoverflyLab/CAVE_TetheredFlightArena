//Found at https://stackoverflow.com/questions/54148708/programming-an-fps-limiter-for-unity
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using NaughtyAttributes;

namespace TetheredFlight
{
    //Did not perform as desired and was equally as effective or worse than Unity's own framerate function.
    //If you are having issues with stabilising framerate at an exact value, this script may help get you started.
    public class FrameLimiter : MonoBehaviour
    {
        public static FrameLimiter Instance = null;

        [SerializeField, ReadOnly] private float FPSLimit = 0;

        private DateTime epochStart = default;
        private double lastTime = 0;
        private double step = 0;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Error: There can only be one FrameLimiter");
                Destroy(this.gameObject);
            }
        }

        void Start()
        {
            epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            lastTime = GetNow();
        }

        void OnDestroy()
        {
            Instance = null;
        }

        void Update()
        {
            if (FPSLimit <= 0) return;

            step = (double)(1.0f / FPSLimit);
            lastTime += step;
            //print("lastframe = " + lastTime.ToString());
            //print("step = " + step.ToString());
            double now = GetNow();
            //print("now = " + now.ToString());

            if (now >= lastTime)
            {
                //print("lateframe");
                lastTime = now;
                return;
            }
            else
            {
                SpinWait.SpinUntil(() => {return (GetNow() >= lastTime);});
            }
        }

        public void UpdateFPSLimit(float newLimit)
        {
            FPSLimit = newLimit;
        }

        //Alternative GetNow funtion using ticks
        // public long GetNow()
        // {
        //     return (DateTime.UtcNow - epochStart).Ticks*100;
        // }   

        private double GetNow()
        {
            double currentTime = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            return currentTime / 1000;
        }
    }
}
