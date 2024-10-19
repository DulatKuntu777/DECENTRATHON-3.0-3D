using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public enum Complexity
{
    Hard,
    Medium,
    Easy
}
public class AI : SerializedMonoBehaviour
{
    [Header("Settings")] 
     public int lookAheadPoints = 5; // Количество точек перед автомобилем
     
    [SerializeField] private float maxBrakeAngle = 45f;
    [SerializeField] private float brakeWeight = 0.5f;
    [SerializeField] private float boxCastHalfWidth = 1f;
    [SerializeField] private float boxCastHalfHeight = 1f;
    [SerializeField] private float castMultiplicator = 0.075f;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private LayerMask enemyLayerMask;
    
    [Header("Vehicle Dynamics Settings")]
    public float maxSteerAngle = 30f;
    public float maxDrag = 9.81f; 
    public float minDrag = 9.81f;
    
    [Header("Getters")] 
    public float brakeInput = 0;
    public float inputSteer = 0;
    public float throttleInput = 0;

    [Header("Links")] 
    [SerializeField] private TrackAnalyzer trackAnalyzer;
    [SerializeField] private Visor visor;

    public List<Vector3> upcomingWaypoints = new List<Vector3>();
    
    private SCC_Drivetrain carControl;
    private SCC_InputProcessor inputs;
    private MaxSpeedCalculator maxSpeedCalculator;
    private CarWaypointFollower waipointCalc;
    private Rigidbody rb;
    private Vector3 boxCastDirection;
    private Vector3 targetPoint;
    private float previousBrakeIntensity = 0f;
    private float boxCastDistance;
    private float previousSlowDownIntensity = 0f;
    private Quaternion boxCastRotation = Quaternion.identity;
    private float angleToTarget;
    private float lowSpeedTime = 3;

    private Complexity complexity = Complexity.Hard;
    
    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        carControl ??= GetComponentInChildren<SCC_Drivetrain>();
        inputs ??= GetComponentInChildren<SCC_InputProcessor>();
        waipointCalc ??= GetComponent<CarWaypointFollower>();
        visor ??= GetComponentInChildren<Visor>();
    }

    private void OnEnable()
    {
        visor.condition = true;
        inputs.receiveInputsFromInputManager = false;
        
    }
   
    private void OnDisable()
    {
        visor.condition = false;
        inputs.receiveInputsFromInputManager = true;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        VehicleAIManager.instance.AddVehicle(this);
                SetComplexity(VehicleAIManager.instance.currentComplexity);
    }

    private void Update()
    {
        throttleInput = 0;
        brakeInput = 0;
        if(complexity == Complexity.Hard)
            lookAheadPoints = (int)Mathf.Clamp(carControl.speed / 20f, 2f, 10f);
        else if (complexity == Complexity.Medium)
            lookAheadPoints = (int)Mathf.Clamp(carControl.speed / 25f, 2f, 6);
        else if (complexity == Complexity.Easy)
            lookAheadPoints = (int)Mathf.Clamp(carControl.speed / 30f, 1f, 4);
        
        ThortleBrakeControl();
        ReturnToRoad();
        
        inputs.inputs.steerInput = inputSteer + (Mathf.Clamp(visor.steerValue, -1, 1) * castMultiplicator);
        inputs.inputs.throttleInput = throttleInput;
        inputs.inputs.brakeInput = brakeInput;
    }

    private void ThortleBrakeControl()
    {
        targetPoint = FindFurthestAccessiblePoint(out angleToTarget);
        MoveTowardsTarget(targetPoint);
        
        float throttleValue = UpdateCarState();
        float maxDrag = this.maxDrag * (carControl.speed / carControl.maximumSpeed);
        rb.drag = Mathf.Clamp((1 - throttleValue) * 2, minDrag, maxDrag);
        
        throttleInput = throttleValue;
        brakeInput = Mathf.Clamp01( brakeWeight - throttleValue);
    }
    private float UpdateCarState()
    {
        int currentWaypointIndex = waipointCalc.currentWaypointIndex;
        List<WaypointThrottle> throttleValues = trackAnalyzer.throttleValues;

        if (currentWaypointIndex >= 0 && currentWaypointIndex < throttleValues.Count)
        {
            float throttleValue = throttleValues[currentWaypointIndex].throttleValue;
            return throttleValue;
        }

        return 0;
    }
    private Vector3 FindFurthestAccessiblePoint(out float angleToTarget)
    {
        angleToTarget = 0f; // Инициализируем выходной параметр

        Vector3 furthestPoint = upcomingWaypoints[0];
        bool boxCastPerformed = false;

        for (int i = upcomingWaypoints.Count - 1; i >= 0; i--)
        {
            Vector3 point = upcomingWaypoints[i];

            Vector3 direction = point - transform.position;
            float distance = direction.magnitude;
            direction.Normalize();

            // Сохраняем данные для визуализации
            boxCastDirection = direction;
            boxCastDistance = distance;

            // Вычисляем вращение коробки в направлении движения
            boxCastRotation = Quaternion.LookRotation(direction);

            // Выполняем BoxCast
            if (!Physics.BoxCast(transform.position, new Vector3(boxCastHalfWidth, boxCastHalfHeight, boxCastHalfWidth), direction, boxCastRotation, distance, wallLayerMask))
            {
                // Если нет столкновений, сохраняем эту точку
                furthestPoint = point;

                // Вычисляем угол между направлением автомобиля и направлением на эту точку
                Vector3 carForward = transform.forward;
                angleToTarget = Vector3.Angle(carForward, direction);

                // Выходим из цикла
                boxCastPerformed = true;
                break;
            }
        }

        // Если ни одна точка недоступна, используем ближайшую точку и вычисляем угол до нее
        if (!boxCastPerformed)
        {
            Vector3 direction = upcomingWaypoints[0] - transform.position;
            float distance = direction.magnitude;
            direction.Normalize();

            // Сохраняем данные для визуализации
            boxCastDirection = direction;
            boxCastDistance = distance;

            // Вычисляем вращение коробки в направлении движения
            boxCastRotation = Quaternion.LookRotation(direction);

            Vector3 carForward = transform.forward;
            angleToTarget = Vector3.Angle(carForward, direction);
        }

        return furthestPoint;
    }
    private void MoveTowardsTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;

        Vector3 forward = transform.forward;
        float angle = Vector3.SignedAngle(forward, directionToTarget, Vector3.up);

        float steerInput = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f); 
        inputSteer = (Mathf.Lerp(inputSteer, steerInput, 15 * Time.deltaTime));
    }

    private void ReturnToRoad()
    {
        if (carControl.speed < 5)
        {
            lowSpeedTime -= Time.deltaTime;
            if (lowSpeedTime <= 0)
            {
                int currentWaypointIndex = waipointCalc.currentWaypointIndex;
                Quaternion targetRotation = Quaternion.LookRotation(boxCastDirection);
                rb.position = waipointCalc.waypoints[currentWaypointIndex] + new Vector3(0, 1, 0);
                rb.rotation = targetRotation;
                lowSpeedTime = Random.Range(1f, 3f);
            }
        }
    }
    
    public void SetComplexity(Complexity complexity)
    {
        this.complexity = complexity;
        switch (complexity)
        {
            case Complexity.Hard:
                carControl.maximumSpeed = 150 + Random.Range(5, 25);
                carControl.engineTorque = 2000 + Random.Range(100, 300);
                carControl.brakeTorque = 3000;
                maxDrag = 0.7f;
                castMultiplicator = 0.25f;
                break;
            case Complexity.Medium:
                carControl.maximumSpeed = 120+ Random.Range(5, 25);
                carControl.engineTorque = 1900 + Random.Range(100, 300);
                carControl.brakeTorque = 2000;
                maxDrag = 0.55f;
                castMultiplicator = 0.2f;
                break;
            case Complexity.Easy:
                carControl.maximumSpeed = 95 + Random.Range(5, 25);
                carControl.engineTorque = 1400 + Random.Range(100, 300);
                carControl.brakeTorque = 2000;
                maxDrag = 0.45f;
                castMultiplicator = 0.15f;
                break;
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); 
        if (boxCastDirection != Vector3.zero)
        {
            Vector3 boxCenter = transform.position + boxCastDirection * boxCastDistance;

            Vector3 boxSize = new Vector3(boxCastHalfWidth * 2, boxCastHalfHeight * 2, boxCastHalfWidth * 2);

            Gizmos.matrix = Matrix4x4.TRS(boxCenter, boxCastRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);

            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawLine(transform.position, boxCenter);
        }

        if (targetPoint != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPoint);
            Gizmos.DrawSphere(targetPoint, 0.5f);
        }

        Gizmos.color = Color.cyan;
        foreach (Vector3 waypoint in upcomingWaypoints)
        {
            Gizmos.DrawSphere(waypoint, 0.3f);
        }
    }
}
