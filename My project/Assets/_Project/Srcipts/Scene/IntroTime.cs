using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class IntroTime : MonoBehaviour
{
    public int time;

    private void Start()
    {
        StartCoroutine(NextLevel());
    }

    IEnumerator NextLevel()
    {
        yield return new WaitForSeconds(time);
        SceneManager.LoadScene(1);
    }
}
