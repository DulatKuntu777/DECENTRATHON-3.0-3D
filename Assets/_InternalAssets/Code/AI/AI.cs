using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
    
public class AI : SerializedMonoBehaviour
    {
        public Visor visor;
        [Header("Settings")] 
        public float brakeWeight = 0.5f;
        public int lookAheadPoints = 5;        // Количество точек перед автомобилем

        [Header("Getters")]
        public float inputbrake = 0;
        public float inputSteer = 0;
        public float inputThrottle = 0;
        
        public LayerMask obstacleLayerMask; // Слой препятствий
        public float boxCastHalfWidth = 1f; // Половина ширины автомобиля
        public float boxCastHalfHeight = 1f; // Половина высоты автомобиля
        
        public List<Vector3> upcomingWaypoints = new List<Vector3>();

        [SerializeField] private SCC_Drivetrain carControl;
        [SerializeField] private SCC_InputProcessor inputs;
        [SerializeField] private CarWaypointFollower waipointCalc;
        [SerializeField] private float DirectionMagnitude;
        public bool brake = true;
        private Vector3 targetPoint;

        Vector3 FindFurthestAccessiblePoint(out float angleToTarget)
        {
            angleToTarget = 0f; // Инициализируем выходной параметр

            Vector3 furthestPoint = upcomingWaypoints[0];

            // Проходимся по точкам в обратном порядке (от дальней к ближней)
            for (int i = upcomingWaypoints.Count - 1; i >= 0; i--)
            {
                Vector3 point = upcomingWaypoints[i];

                // Направление от автомобиля к точке
                Vector3 direction = point - transform.position;
                float distance = direction.magnitude;
                direction.Normalize();

                // Выполняем BoxCast
                if (!Physics.BoxCast(transform.position, new Vector3(boxCastHalfWidth, boxCastHalfHeight, boxCastHalfWidth), direction, Quaternion.identity, distance, obstacleLayerMask))
                {
                    // Если нет столкновений, сохраняем эту точку
                    furthestPoint = point;

                    // Вычисляем угол между направлением автомобиля и направлением на эту точку
                    Vector3 carForward = transform.forward;
                    angleToTarget = Vector3.Angle(carForward, direction);

                    // Выходим из цикла, так как нашли самую дальнюю доступную точку
                    break;
                }
            }

            // Если ни одна точка недоступна, используем ближайшую точку и вычисляем угол до нее
            if (angleToTarget == 0f)
            {
                Vector3 direction = upcomingWaypoints[0] - transform.position;
                direction.Normalize();
                Vector3 carForward = transform.forward;
                angleToTarget = Vector3.Angle(carForward, direction);
            }

            // Возвращаем выбранную точку
            return furthestPoint;
        }
        
        void MoveTowardsTarget(Vector3 target)
        {
            // Ваш код для перемещения автомобиля к target
            // Например, вычисление steerInput на основе направления к target

            Vector3 directionToTarget = (target - transform.position).normalized;

            // Вычисляем угол между направлением автомобиля и направлением на цель
            Vector3 forward = transform.forward;
            float angle = Vector3.SignedAngle(forward, directionToTarget, Vector3.up);

            // Нормализуем угол в диапазон от -1 до 1
            float steerInput = Mathf.Clamp(angle / 45f, -1f, 1f); // Предполагаем, что максимальный угол поворота 45 градусов
            inputSteer = steerInput;
            inputThrottle = 1;
            // Управляем автомобилем
            // Пример:
            // ApplySteering(steerInput);
            // ApplyThrottle(throttleInput);
        }
        
        void OnDrawGizmos()
        {
            // Визуализация BoxCast
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(boxCastHalfWidth * 2, boxCastHalfHeight * 2, boxCastHalfWidth * 2));

            // Визуализация направления к targetPoint
            if (targetPoint != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPoint);
            }
        }
        
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
            
            
            float angleToTarget;
            // Находим самую дальнюю доступную точку и получаем угол до нее
            targetPoint = FindFurthestAccessiblePoint(out angleToTarget);

            // Движемся к targetPoint
            MoveTowardsTarget(targetPoint);

            // Чем больше угол, тем сильнее торможение
            float maxAngle = 90f; // Угол, при котором торможение будет максимальным
            float brakeIntensity = Mathf.Clamp01(angleToTarget / maxAngle) * brakeWeight * carControl.speed;

            // Применяем торможение и регулируем газ
            inputbrake = brakeIntensity;

            /*Vector3 rotdirection = waipointCalc.direction;
            Quaternion targetRotation = Quaternion.LookRotation(rotdirection);
            float angleRot = HR_CalculateAngle.CalculateAngle(transform.rotation, targetRotation) * (angleWeight / 310);*/
            
            
            
            inputs.inputs.steerInput = inputSteer;
            inputs.inputs.throttleInput = inputThrottle;
            inputs.inputs.brakeInput = inputbrake;

            /*inputSteer = visor.steerValue + angleRot;
            inputThrottle = 1;

            inputs.inputs.steerInput = inputSteer;
            inputs.inputs.throttleInput = inputThrottle;*/
        }
    }
