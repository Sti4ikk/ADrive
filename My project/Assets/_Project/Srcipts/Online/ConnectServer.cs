using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
public class ConnectServer : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        SceneManager.LoadScene("menu");
    }
}