using UnityEngine;
using System.IO;

[System.Serializable]
public class CarData
{
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
}

// Сисетма сохранения данных о последнем местоположении машины игрока в одиночном режиме
public class CarSaveSystem : MonoBehaviour
{
    [SerializeField] private Transform carTransform;

    private string customSavePath = null;
    private string savePath;

    private void Awake()
    {
        // Если путь задан извне - используем его, иначе дефолтный
        if (string.IsNullOrEmpty(customSavePath))
        {
            savePath = Path.Combine(Application.dataPath, "_Project\\Car\\CarSaveSystem\\carSave.json");
        }
        else
        {
            savePath = customSavePath;
        }

        Debug.Log($"Путь сохранения: {savePath}");
    }


    private void Start()
    {
        LoadCarData();
    }

    private void OnApplicationQuit()
    {
        SaveCarData();
    }

    public void SaveCarData()
    {
        try
        {
            CarData data = new CarData
            {
                posX = carTransform.position.x,
                posY = carTransform.position.y,
                posZ = carTransform.position.z,
                rotX = carTransform.rotation.eulerAngles.x,
                rotY = carTransform.rotation.eulerAngles.y,
                rotZ = carTransform.rotation.eulerAngles.z
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);

            // ЛОГИРОВАНИЕ СОХРАНЕНИЯ
            GameLogger.Log(GameLogger.LogCategory.System,
                $"Данные машины сохранены: Pos({data.posX:F2}, {data.posY:F2}, {data.posZ:F2})");
            GameLogger.LogEvent("CarDataSaved", $"Position: {carTransform.position}");
        }
        catch (System.Exception e)
        {
            // ЛОГИРОВАНИЕ ОШИБКИ
            GameLogger.LogError(GameLogger.LogCategory.Error,
                $"Ошибка сохранения данных машины: {e.Message}");
        }
    }

    // Загрузить данные ииз файла при загрузке игры
    public void LoadCarData()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                CarData data = JsonUtility.FromJson<CarData>(json);
                carTransform.position = new Vector3(data.posX, data.posY, data.posZ);
                carTransform.rotation = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);

                // ЛОГИРОВАНИЕ ЗАГРУЗКИ
                GameLogger.Log(GameLogger.LogCategory.System,
                    $"Данные машины загружены: Pos({data.posX:F2}, {data.posY:F2}, {data.posZ:F2})");
                GameLogger.LogEvent("CarDataLoaded", $"Position: {carTransform.position}");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError(GameLogger.LogCategory.Error,
                    $"Ошибка загрузки данных машины: {e.Message}");
            }
        }
        else
        {
            GameLogger.LogWarning(GameLogger.LogCategory.System,
                "Файл сохранения не найден, используются параметры по умолчанию");
        }
    }

    // Удалить сохранение
    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            GameLogger.Log(GameLogger.LogCategory.System, "Сохранение удалено");
        }
    }

    // ДОБАВЬ ЭТОТ МЕТОД для тестов
    public void SetSavePath(string path)
    {
        customSavePath = path;
        savePath = path;
    }
}