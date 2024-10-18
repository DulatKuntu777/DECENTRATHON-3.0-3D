using UnityEngine;    
public class Visor : MonoBehaviour
    {
        public CastVehicleVisor forwarsCast;
        public CastVehicleVisor leftCast;
        public CastVehicleVisor rightCast;
        public CastVehicleVisor wLeftCast;
        public CastVehicleVisor wRightCast;

        public float multiplicator = 4;
        public float steerValue = 0;
        public float speedValue = 0;

        public bool condition = false;

        void Update()
        {
            if(!condition) return;

            forwarsCast.HitBoxRay();
            leftCast.HitBoxRay();
            rightCast.HitBoxRay();
            wLeftCast.HitBoxRay();
            wRightCast.HitBoxRay();
            
            float f = forwarsCast.hitValue;
            float r = rightCast.hitValue;
            float l = leftCast.hitValue;
            float wr = wRightCast.hitValue;
            float wl = wLeftCast.hitValue;

            float difference = Mathf.Abs(1 - Mathf.Abs((l + wl) - (r+wr)));

            float forwardSide = (f + difference);
            float leftSide = (l + wl);
            float rightSide = (r+wr);
            steerValue = Mathf.Clamp( ((leftSide - rightSide))*forwardSide  * multiplicator, -1f, 1f);
            speedValue = f;
        }
    }
