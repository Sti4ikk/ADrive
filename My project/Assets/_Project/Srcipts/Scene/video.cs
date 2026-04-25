using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class VideoIntroController : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Canvas mainCanvas;
    [Header("Настройки")]
    [SerializeField] private float delayAfterVideo = 1f;
    [SerializeField] private bool skipWithAnyKey = true;
    [SerializeField] private Camera mainCamera;

    // Статическая переменная — живёт только во время одной сессии
    private static bool hasShownIntroThisSession = false;

    private void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCanvas != null)
            mainCanvas.gameObject.SetActive(false);

        mainCamera.backgroundColor = Color.black;
    }

    private void Start()
    {
        // Если уже показывали в этой сессии — сразу в меню
        if (hasShownIntroThisSession)
        {
            SkipIntroAndShowMenu();
            return;
        }

        // Помечаем, что в этой сессии интро уже будет показано
        hasShownIntroThisSession = true;

        SetupAndPlayIntro();
    }

    private void SetupAndPlayIntro()
    {
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.prepareCompleted += OnVideoReady;
        videoPlayer.Prepare();
    }

    private void OnVideoReady(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= OnVideoReady;
        StartCoroutine(DelayBeforePlay(0.2f));
    }

    private IEnumerator DelayBeforePlay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        StartCoroutine(ShowMenuAfterDelay());
    }

    private IEnumerator ShowMenuAfterDelay()
    {
        yield return new WaitForSeconds(delayAfterVideo);

        if (mainCamera != null)
            mainCamera.backgroundColor = Color.clear;

        if (mainCanvas != null)
        {
            mainCanvas.gameObject.SetActive(true);
            CanvasGroup cg = mainCanvas.GetComponent<CanvasGroup>();
            if (cg == null) cg = mainCanvas.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            float fadeTime = 0.8f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }

            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }

        Destroy(gameObject);
    }

    private void SkipIntroAndShowMenu()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (mainCamera != null)
            mainCamera.backgroundColor = Color.clear;

        if (mainCanvas != null)
        {
            mainCanvas.gameObject.SetActive(true);
        }

        Destroy(gameObject);
    }

    private void Update()
    {
        if (skipWithAnyKey && !hasShownIntroThisSession)
        {
            if (Input.anyKeyDown)
            {
                hasShownIntroThisSession = true; // чтобы больше не предлагалось
                StopAllCoroutines();
                if (videoPlayer != null) videoPlayer.Stop();
                SkipIntroAndShowMenu();
            }
        }
    }

    // Больше НЕ НУЖНО ничего удалять — флаг и так умрёт при закрытии приложения
}