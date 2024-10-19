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

        // Расчёт колесной базы
        float wheelBase = Vector3.Distance(frontLeftWheel.transform.position, rearLeftWheel.transform.position);

        // Угол поворота колёс в радианах
        float steeringAngleRad = frontLeftWheel.steerAngle * Mathf.Deg2Rad;

        // Проверка на нулевой или очень малый угол поворота
        if (Mathf.Abs(steeringAngleRad) < 0.001f)
        {
            // Автомобиль движется почти прямо
            Debug.Log("Автомобиль движется прямо, ограничений по скорости нет.");
            return;
        }

        // Радиус поворота
        float turnRadius = wheelBase / Mathf.Tan(steeringAngleRad);

        // Проверка на корректность радиуса поворота
        if (turnRadius <= 0 || float.IsInfinity(turnRadius) || float.IsNaN(turnRadius))
        {
            Debug.LogWarning("Некорректный радиус поворота: " + turnRadius);
            return;
        }

        // Получение общей максимальной боковой силы сцепления
        float totalMaxSideForce = 0f;

        foreach (WheelCollider wheel in wheels)
        {
            WheelHit hit;
            if (wheel.GetGroundHit(out hit))
            {
                // Нагрузка на колесо (нормальная сила)
                float normalForce = hit.force;

                // Коэффициент бокового трения (extremumValue)
                float frictionCoefficient = wheel.sidewaysFriction.extremumValue;

                // Максимальная боковая сила для этого колеса
                float maxSideForce = frictionCoefficient * normalForce;

                totalMaxSideForce += maxSideForce;
            }
            else
            {
                // Колесо не касается земли
                Debug.LogWarning("Колесо не касается земли: " + wheel.name);
            }
        }

        // Проверка на корректность общей боковой силы
        if (totalMaxSideForce <= 0)
        {
            maxSpeedKmh = 400;
            return;
        }

        // Масса автомобиля
        float mass = rb.mass;

        // Максимальная допустимая скорость (в м/с)
        float maxSpeed = Mathf.Sqrt((totalMaxSideForce * turnRadius) / mass);

        // Проверка на корректность максимальной скорости
        if (float.IsNaN(maxSpeed) || maxSpeed <= 0)
        {
            maxSpeedKmh = 400;
            return;
        }

        // Преобразование в км/ч
        maxSpeedKmh = maxSpeed * 3.6f;

    }
}
