using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class windowLoading : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private int singlePlayerSceneIndex = 1;

    [Header("Настройки")]
    [SerializeField] private float delayAfterVideo = 1f;   // ← Здесь настраивай задержку (0.3–0.8 сек обычно идеально)
    [SerializeField] private bool skipWithAnyKey = true;     // Пропуск по нажатию
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        // Автоподхват VideoPlayer, если забыли перетащить
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Скрываем канвас ещё до первого кадра
        if (mainCanvas != null)
            mainCanvas.gameObject.SetActive(false);

        mainCamera.backgroundColor = Color.black;
    }

    private void Start()
    {
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished; // главное событие — конец видео

        videoPlayer.Prepare();                                      // начинаем загрузку
        videoPlayer.prepareCompleted += OnVideoReady;               // подписываемся на событие «видео готово»

        // Пока видео не готово — показываем чёрный экран
        if (mainCamera != null)
            mainCamera.backgroundColor = Color.black;
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        // Запускаем корутину с задержкой
        StartCoroutine(ShowMainSceneAfterDelay());
    }

    private IEnumerator ShowMainSceneAfterDelay()
    {
        // Ждём небольшую паузу после последнего кадра видео (по желанию)
        yield return new WaitForSeconds(delayAfterVideo);

        SceneManager.LoadScene(singlePlayerSceneIndex);

        Destroy(gameObject);
    }

    // Пропуск видео по нажатию любой клавиши / клику
    private void Update()
    {

    }



    private void OnVideoReady(VideoPlayer vp)
    {
        // Отписываемся, чтобы не сработало дважды
        videoPlayer.prepareCompleted -= OnVideoReady;

        //ЗАДЕРЖКА ПЕРЕД ПОЯВЛЕНИЕМ МЕНЮ
        StartCoroutine(DelayBeforePlay(5)); // 0.2 сек — идеально, можно 0f или 0.5f
    }

    private IEnumerator DelayBeforePlay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // ТЕПЕРЬ ТОЧНО ВСЁ ГОТОВО — запускаем видео
        videoPlayer.Play();
    }
}

