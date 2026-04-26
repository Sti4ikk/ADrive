using System;
using UnityEngine;

/// <summary>
/// Данные одной ошибки вождения
/// </summary>
[Serializable]
public class DrivingError
{
    public enum ErrorType
    {
        Speeding,           // Превышение скорости
        RedLight,           // Проезд на красный
        WrongLane,          // Нарушение разметки
        WrongManeuver,      // Неправильный манёвр
        Custom              // Кастомная ошибка
    }

    public ErrorType type;
    public string    description;       // Читаемое описание
    public float     speedAtMoment;     // Скорость в момент ошибки (км/ч)
    public float     speedLimit;        // Разрешённая скорость (км/ч)
    public Vector3   worldPosition;     // Где на карте произошла ошибка
    public DateTime  timestamp;

    public DrivingError(ErrorType type, string description, float speed, float speedLimit, Vector3 position)
    {
        this.type          = type;
        this.description   = description;
        this.speedAtMoment = Mathf.Round(speed);
        this.speedLimit    = speedLimit;
        this.worldPosition = position;
        this.timestamp     = DateTime.Now;
    }

    /// <summary>Локализованное название типа ошибки</summary>
    public string TypeName => type switch
    {
        ErrorType.Speeding      => "Превышение скорости",
        ErrorType.RedLight      => "Проезд на красный свет",
        ErrorType.WrongLane     => "Нарушение разметки",
        ErrorType.WrongManeuver => "Неправильный манёвр",
        _                       => "Нарушение ПДД"
    };
}
