using UnityEngine;

public class FanBladeRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Скорость вращения в градусах в секунду")]
    public float rotationSpeed = 360f; // 360°/секунда = 1 оборот в секунду

    [Tooltip("Направление вращения (true = по часовой стрелке)")]
    public bool clockwise = true;

    void Update()
    {
        // Определяем направление
        float direction = clockwise ? -1f : 1f;

        // Вращаем лопасть вокруг локальной оси Z
        transform.Rotate(0f, 0f, rotationSpeed * direction * Time.deltaTime);
    }
}
