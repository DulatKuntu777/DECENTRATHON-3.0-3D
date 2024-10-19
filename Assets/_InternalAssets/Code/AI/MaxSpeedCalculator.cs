using UnityEngine;

public class MaxSpeedCalculator : MonoBehaviour
{
    public float maxSpeedKmh;
 public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;
    public Rigidbody rb;

    void Update()
    {
        // Массив колёс
        WheelCollider[] wheels = new WheelCollider[] { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };

        float wheelBase = Vector3.Distance(frontLeftWheel.transform.position, rearLeftWheel.transform.position);

        float steeringAngleRad = frontLeftWheel.steerAngle * Mathf.Deg2Rad;

        if (Mathf.Abs(steeringAngleRad) < 0.001f)
        {
            Debug.Log("Автомобиль движется прямо, ограничений по скорости нет.");
            return;
        }

        float turnRadius = wheelBase / Mathf.Tan(steeringAngleRad);

        if (turnRadius <= 0 || float.IsInfinity(turnRadius) || float.IsNaN(turnRadius))
        {
            Debug.LogWarning("Некорректный радиус поворота: " + turnRadius);
            return;
        }

        float totalMaxSideForce = 0f;

        foreach (WheelCollider wheel in wheels)
        {
            WheelHit hit;
            if (wheel.GetGroundHit(out hit))
            {
                float normalForce = hit.force;
                float frictionCoefficient = wheel.sidewaysFriction.extremumValue;
                float maxSideForce = frictionCoefficient * normalForce;

                totalMaxSideForce += maxSideForce;
            }
            else
            {
                Debug.LogWarning("Колесо не касается земли: " + wheel.name);
            }
        }
        if (totalMaxSideForce <= 0)
        {
            maxSpeedKmh = 400;
            return;
        }

        float mass = rb.mass;

        float maxSpeed = Mathf.Sqrt((totalMaxSideForce * turnRadius) / mass);

        if (float.IsNaN(maxSpeed) || maxSpeed <= 0)
        {
            maxSpeedKmh = 400;
            return;
        }

        // Преобразование в км/ч
        maxSpeedKmh = maxSpeed * 3.6f;

    }
}
