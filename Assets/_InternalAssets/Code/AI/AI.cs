using Sirenix.OdinInspector;
using UnityEngine;
    
public class AI : SerializedMonoBehaviour
    {
        public Visor visor;

        public float inputbrake = 0;
        public float inputSteer = 0;
        public float inputThrottle = 0;
        public float inputBoost = 0;

        [SerializeField] private SCC_Drivetrain carControl;
        [SerializeField] private SCC_InputProcessor inputs;
        public bool brake = true;
        
        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            carControl ??= GetComponent<SCC_Drivetrain>();
            inputs ??= GetComponent<SCC_InputProcessor>();
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

            inputSteer = visor.steerValue;
            inputThrottle = 1;

            inputs.inputs.steerInput = inputSteer;
            inputs.inputs.throttleInput = inputThrottle;
        }
    }
