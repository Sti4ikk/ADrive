using UnityEngine;

public class CarSpawner
{
    [Header("Точки спавна")]
    public Transform[] spawnPoints;

    [Header("Имена префабов в Resources/Cars")]
    public string[] carPrefabs;

    void Start()
    {
        SpawnCarForPlayer();
    }

    void SpawnCarForPlayer()
    {
        if (carPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogError("CarSpawner: Не назначены префабы или точки спавна!");
            return;
        }

        // Определяем точку спавна для текущего игрока
        //int spawnIndex = Mathf.Clamp(PhotonNetwork.LocalPlayer.ActorNumber - 1, 0, spawnPoints.Length - 1);
        //Transform spawnPoint = spawnPoints[spawnIndex];

        // Выбираем случайный префаб машины
        int carIndex = Random.Range(0, carPrefabs.Length);
        string carPrefabName = "Cars/" + carPrefabs[carIndex]; // префабы должны быть в Resources/Cars

        // Создаём машину через Photon
        //GameObject playerCar = PhotonNetwork.Instantiate(
        //    carPrefabName,
        //    spawnPoint.position,
        //    spawnPoint.rotation
        //);

        // Можно настроить камеру и другие компоненты здесь
        // Camera camera = playerCar.GetComponentInChildren<Camera>();
        // camera.enabled = true;
    }
}
