using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class AI : SerializedMonoBehaviour
{
    public Visor visor;
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
    public float maxSteerAngle = 30f; // Максимальный угол поворота колес в градусах
    public float maxDrag = 9.81f;     // Ускорение свободного падения
    public float minDrag = 9.81f;     // Ускорение свободного падения
    
    [Header("Getters")] 
    public float brakeInput = 0;
    public float inputSteer = 0;
    public float throttleInput = 0;

    public List<Vector3> upcomingWaypoints = new List<Vector3>();

    [Header("Links")] 
    [SerializeField] private TrackAnalyzer trackAnalyzer;

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


    private void Awake()
    {
        OnValidate();
        carControl = GetComponent<SCC_Drivetrain>();
        inputs = GetComponent<SCC_InputProcessor>();
        waipointCalc = GetComponent<CarWaypointFollower>();
        maxSpeedCalculator = GetComponent<MaxSpeedCalculator>();
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
        rb = GetComponent<Rigidbody>();
        
    }

    void Update()
    {
        throttleInput = 0;
        brakeInput = 0;
        lookAheadPoints = (int)Mathf.Clamp(carControl.speed / 25f, 2f, 10f);
        ThortleBrakeControl();
        
        inputs.inputs.steerInput = inputSteer + (Mathf.Clamp(visor.steerValue, -1, 1) * castMultiplicator);
        inputs.inputs.throttleInput = throttleInput;
        inputs.inputs.brakeInput = brakeInput;
    }

    [SerializeField] float angleToTarget;

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

    float UpdateCarState()
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

     Vector3 FindFurthestAccessiblePoint(out float angleToTarget)
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
    void MoveTowardsTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;

        Vector3 forward = transform.forward;
        float angle = Vector3.SignedAngle(forward, directionToTarget, Vector3.up);

        float steerInput = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f); // Предполагаем, что максимальный угол поворота 45 градусов
        inputSteer = (Mathf.Lerp(inputSteer, steerInput, 15 * Time.deltaTime));
    }
    void OnDrawGizmos()
    {
        // Визуализация BoxCast
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Полупрозрачный красный
        if (boxCastDirection != Vector3.zero)
        {
            // Центр коробки
            Vector3 boxCenter = transform.position + boxCastDirection * boxCastDistance;

            // Размеры коробки
            Vector3 boxSize = new Vector3(boxCastHalfWidth * 2, boxCastHalfHeight * 2, boxCastHalfWidth * 2);

            // Рисуем коробку, представляющую конечное положение BoxCast
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, boxCastRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);

            // Рисуем линию от начальной позиции до конечной позиции BoxCast
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawLine(transform.position, boxCenter);
        }

        // Визуализация направления к targetPoint
        if (targetPoint != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPoint);
            Gizmos.DrawSphere(targetPoint, 0.5f);
        }

        // Визуализация upcomingWaypoints
        Gizmos.color = Color.cyan;
        foreach (Vector3 waypoint in upcomingWaypoints)
        {
            Gizmos.DrawSphere(waypoint, 0.3f);
        }
    }
}
