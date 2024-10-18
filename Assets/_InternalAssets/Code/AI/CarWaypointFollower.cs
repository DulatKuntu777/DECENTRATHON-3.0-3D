using System.Collections.Generic;
using UnityEngine;
using BansheeGz.BGSpline.Components;
using UnityEngine.Serialization;

public class CarWaypointFollower : MonoBehaviour
{
    public List<Vector3> waypoints = new ();          // Массив точек пути
    public float speed = 5f;             // Скорость движения автомобиля
    public float reachThreshold = 0.5f;  // Расстояние, при котором считается, что точка достигнута

    public int currentWaypointIndex;    // Текущий индекс точки пути
    [FormerlySerializedAs("Direction")] public Vector3 direction; 
    
    [Header("Links")] 
    [SerializeField] private GameObject bgCurveObj;

    private BGCcSplitterPolyline polylineSplitter;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if(currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count)
            Gizmos.DrawSphere(waypoints[currentWaypointIndex], 2f);
    }

    private void Awake()
    {
        polylineSplitter = bgCurveObj.GetComponent<BGCcSplitterPolyline>();
        waypoints = polylineSplitter.Positions;
    }

    void Start()
    {
        // Находим ближайшую точку пути при старте
        currentWaypointIndex = FindClosestWaypointIndex();
    }
    
    void Update()
    {
        UpdateWaypointIndex();
        UpdateDirection();
    }

    // Метод для нахождения ближайшей точки пути при старте
    int FindClosestWaypointIndex()
    {
        int closestIndex = 0;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = new Vector3(transform.position.x, 0, transform.position.z);

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 waypointPosition = new Vector3(waypoints[i].x, 0, waypoints[i].z);
            float distanceSqr = (waypointPosition - currentPosition).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    // Метод для обновления индекса текущей точки пути
    void UpdateWaypointIndex()
    {
        // Получаем текущую и следующую точки пути с проекцией на плоскость XZ
        Vector3 currentWaypoint = new Vector3(waypoints[currentWaypointIndex].x, 0, waypoints[currentWaypointIndex].z);
        int nextWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        Vector3 nextWaypoint = new Vector3(waypoints[nextWaypointIndex].x, 0, waypoints[nextWaypointIndex].z);

        // Вектор направления между текущей и следующей точками пути
        Vector3 pathDirection = (nextWaypoint - currentWaypoint).normalized;

        // Вектор от текущей точки пути до позиции автомобиля
        Vector3 carDirection = new Vector3(transform.position.x, 0, transform.position.z) - currentWaypoint;

        // Проекция вектора автомобиля на направление пути
        float dotProduct = Vector3.Dot(carDirection, pathDirection);

        // Длина сегмента между текущей и следующей точками пути
        float segmentLength = Vector3.Distance(currentWaypoint, nextWaypoint);

        // Проверяем, прошел ли автомобиль следующую точку пути
        if (dotProduct > segmentLength)
        {
            // Увеличиваем индекс и закольцовываем при необходимости
            currentWaypointIndex = nextWaypointIndex;
        }
    }

    // Метод для обновления направления на следующую точку пути
    void UpdateDirection()
    {
        int nextWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;

        Vector3 carPosition = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 nextWaypointPosition = new Vector3(waypoints[nextWaypointIndex].x, 0, waypoints[nextWaypointIndex].z);

        // Вычисляем направление на следующую точку
        direction = (nextWaypointPosition - carPosition).normalized;
    }

    // Опционально: Метод для визуализации текущего прогресса (например, в GUI)
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), "Текущий индекс точки пути: " + currentWaypointIndex);
        GUI.Label(new Rect(10, 30, 300, 20), "Направление на следующую точку: " + direction);
    }
}