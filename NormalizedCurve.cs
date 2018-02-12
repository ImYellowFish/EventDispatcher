using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImYellowFish.Utility
{
    [System.Serializable]
    public class NormalizedCurve
    {
        public AnimationCurve curve;
        public float unitX = 1;
        public float unitY = 1;

        public float Evaluate(float t)
        {
            return curve.Evaluate(t / unitX) * unitY;
        }
    }
}