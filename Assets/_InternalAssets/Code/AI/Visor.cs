using UnityEngine;    
public class Visor : MonoBehaviour
    {
        public CastVehicleVisor leftCast;
        public CastVehicleVisor rightCast;

        public float multiplicator = 4;
        public float steerValue = 0;

        public bool condition = false;

        void Update()
        {
            if(!condition) return;

            leftCast.HitBoxRay();
            rightCast.HitBoxRay();
            
            float r = rightCast.hitValue;
            float l = leftCast.hitValue;

            float leftSide = (l);
            float rightSide = (r);
            steerValue = Mathf.Clamp( ((leftSide - rightSide))  * multiplicator, -1f, 1f);
        }
    }
