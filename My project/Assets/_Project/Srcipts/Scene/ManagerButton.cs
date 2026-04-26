using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerButton : MonoBehaviour
{
    public void butPlay()
    {
        SceneManager.LoadScene(2);
    }
}
