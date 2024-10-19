using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Components;
using Sirenix.OdinInspector;

[System.Serializable]
public class WaypointThrottle
{
    public int waypointIndex;
    public float throttleValue; // От 0 до 1

    public WaypointThrottle(int index, float value)
    {
        waypointIndex = index;
        throttleValue = Mathf.Clamp01(value);
    }
}
public class TrackAnalyzer : SerializedMonoBehaviour
{
    public List<Vector3> waypoints = new List<Vector3>();

    [Header("Links")] [SerializeField] private GameObject bgCurveObj;
    private BGCcSplitterPolyline polylineSplitter;
    
    [Header("Параметры радиуса кривизны")]
    public float minRadius = 20f; 
    public float maxRadius = 100f;  

    [Header("Параметры дросселя")]
    public float minThrottle = 0f;  
    public float maxThrottle = 1f;  

    [Header("Параметры кривой дросселя")]
    public int pointsBeforeCurve = 5; 
    public int pointsAfterCurve = 2;  

    public List<WaypointThrottle> throttleValues = new List<WaypointThrottle>();

    [Button(25)]
    void Start()
    {
        polylineSplitter = bgCurveObj.GetComponent<BGCcSplitterPolyline>();
        waypoints = polylineSplitter.Positions;
        AnalyzeTrack();
    }

   void AnalyzeTrack()
    {
        int waypointCount = waypoints.Count;

        throttleValues.Clear();
        for (int i = 0; i < waypointCount; i++)
        {
            throttleValues.Add(new WaypointThrottle(i, maxThrottle));
        }

        for (int i = 0; i < waypointCount; i++)
        {
            Vector3 prevPoint = waypoints[(i - 1 + waypointCount) % waypointCount];
            Vector3 currentPoint = waypoints[i];
            Vector3 nextPoint = waypoints[(i + 1) % waypointCount];

            float radius = CalculateCurvatureRadius(prevPoint, currentPoint, nextPoint);
            float normalizedRadius = Mathf.InverseLerp(minRadius, maxRadius, radius);

            float throttle = Mathf.Lerp(minThrottle, maxThrottle, normalizedRadius);

            for (int j = pointsBeforeCurve; j >= 0; j--)
            {
                int index = (i - j + waypointCount) % waypointCount;
                if (index >= 0 && index < waypointCount)
                {
                    float factor = (float)(pointsBeforeCurve - j) / Mathf.Max(pointsBeforeCurve, 1);
                    float adjustedThrottle = Mathf.Lerp(maxThrottle, throttle, factor);
                    throttleValues[index].throttleValue = Mathf.Min(throttleValues[index].throttleValue, adjustedThrottle);
                }
            }

            for (int j = 1; j <= pointsAfterCurve; j++)
            {
                int index = (i + j) % waypointCount;
                if (index >= 0 && index < waypointCount)
                {
                    float factor = (float)j / Mathf.Max(pointsAfterCurve, 1);
                    float adjustedThrottle = Mathf.Lerp(throttle, maxThrottle, factor);
                    throttleValues[index].throttleValue = Mathf.Max(throttleValues[index].throttleValue, adjustedThrottle);
                }
            }

            throttleValues[i].throttleValue = Mathf.Min(throttleValues[i].throttleValue, throttle);
        }
    }

    float CalculateCurvatureRadius(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        float a = Vector3.Distance(pointB, pointC);
        float b = Vector3.Distance(pointA, pointC);
        float c = Vector3.Distance(pointA, pointB);

        float s = (a + b + c) / 2f;

        float area = Mathf.Sqrt(Mathf.Max(s * (s - a) * (s - b) * (s - c), 0f));

        if (area == 0)
        {
            return float.MaxValue;
        }

        float radius = (a * b * c) / (4f * area);

        return radius;
    }


    void OnDrawGizmos()
    {
        if (throttleValues == null || throttleValues.Count == 0)
            return;

        for (int i = 0; i < throttleValues.Count; i++)
        {
            int index = throttleValues[i].waypointIndex;
            float throttleValue = throttleValues[i].throttleValue;
            Vector3 position = waypoints[index];

            Color throttleColor = Color.Lerp(Color.red, Color.green, throttleValue);

            Gizmos.color = throttleColor;
            Gizmos.DrawSphere(position, 1f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * 1.5f, throttleValue.ToString("F2"));
#endif
        }
    }


}
