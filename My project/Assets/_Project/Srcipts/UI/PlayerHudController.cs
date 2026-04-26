//using Photon.Pun;
//using UnityEngine;

//public class PlayerHUDController : MonoBehaviourPun
//{
//    [Header("HUD Elements")]
//    public Canvas hudCanvas;
//    public GameObject speedometer;

//    void Start()
//    {
//        // Проверяем, это локальный игрок или нет
//        if (photonView.IsMine)
//        {
//            // Это наш игрок - показываем HUD
//            if (hudCanvas != null)
//                hudCanvas.enabled = true;

//            if (speedometer != null)
//                speedometer.SetActive(true);
//        }
//        else
//        {
//            // Это другой игрок - скрываем HUD
//            if (hudCanvas != null)
//                hudCanvas.enabled = false;

//            if (speedometer != null)
//                speedometer.SetActive(false);
//        }
//    }
//}