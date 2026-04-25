//using Photon.Pun;
//using UnityEngine;
//using UnityEngine.UI;

//public class MinimapController : MonoBehaviourPun
//{
//    [Header("Minimap References")]
//    public Camera minimapCamera;           // Minimap Camera из префаба
//    public RawImage minimapDisplay;        // Minimap Display (RawImage)

//    [Header("Minimap Settings")]
//    public int textureResolution = 512;
//    public float cameraHeight = 50f;

//    private RenderTexture playerMinimapTexture;

//    void Start()
//    {
//        if (photonView.IsMine)
//        {
//            // Локальный игрок - настроить миникарту
//            SetupMinimap();
//        }
//        else
//        {
//            // Другой игрок - отключить камеру
//            if (minimapCamera != null)
//                minimapCamera.enabled = false;
//        }
//    }

//    void SetupMinimap()
//    {
//        if (minimapCamera == null || minimapDisplay == null)
//        {
//            Debug.LogError("Minimap Camera or Display not assigned!");
//            return;
//        }

//        // Создать новый уникальный RenderTexture для этого игрока
//        playerMinimapTexture = new RenderTexture(textureResolution, textureResolution, 16);
//        playerMinimapTexture.name = "PlayerMinimap_" + photonView.ViewID;

//        // Назначить RenderTexture на камеру
//        minimapCamera.targetTexture = playerMinimapTexture;
//        minimapCamera.enabled = true;

//        // Показать на RawImage
//        minimapDisplay.texture = playerMinimapTexture;

//        Debug.Log("Minimap setup for player: " + photonView.ViewID);
//    }

//    void LateUpdate()
//    {
//        if (photonView.IsMine && minimapCamera != null)
//        {
//            // Камера миникарты следует за игроком сверху
//            Vector3 cameraPos = transform.position;
//            cameraPos.y += cameraHeight;
//            minimapCamera.transform.position = cameraPos;

//            // Опционально: вращать вместе с машиной
//            // minimapCamera.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
//        }
//    }

//    void OnDestroy()
//    {
//        // Очистить RenderTexture при удалении игрока
//        if (playerMinimapTexture != null)
//        {
//            playerMinimapTexture.Release();
//            Destroy(playerMinimapTexture);
//        }
//    }
//}