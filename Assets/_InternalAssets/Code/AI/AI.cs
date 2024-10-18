using Sirenix.OdinInspector;
using UnityEngine;
    
public class AI : SerializedMonoBehaviour
    {
        public Visor visor;
        [Header("Settings")] 
        public float angleWeight = 0.5f;

        [Header("Getters")]
        public float inputbrake = 0;
        public float inputSteer = 0;
        public float inputThrottle = 0;
        public float inputBoost = 0;
        

        [SerializeField] private SCC_Drivetrain carControl;
        [SerializeField] private SCC_InputProcessor inputs;
        [SerializeField] private CarWaypointFollower waipointCalc;
        public bool brake = true;
        
        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            carControl ??= GetComponent<SCC_Drivetrain>();
            inputs ??= GetComponent<SCC_InputProcessor>();
            waipointCalc ??= GetComponent<CarWaypointFollower>();
            visor ??= GetComponentInChildren<Visor>();
        }
        private void OnEnable()
        {
            visor.condition = true;
        }
        private void Start()
        {
            inputs.receiveInputsFromInputManager = false;
        }

        private bool useNos = false;    
        void Update()
        {
            if (brake)
            {
                inputs.inputs.brakeInput = 1;
                return;
            }

            Vector3 rotdirection = waipointCalc.direction;
            Quaternion targetRotation = Quaternion.LookRotation(rotdirection);
            float angleRot = HR_CalculateAngle.CalculateAngle(transform.rotation, targetRotation) * (angleWeight / 310);
            
            inputSteer = visor.steerValue + angleRot;
            inputThrottle = 1;

            inputs.inputs.steerInput = inputSteer;
            inputs.inputs.throttleInput = inputThrottle;
        }
    }
