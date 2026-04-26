using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система локализации для симулятора вождения
/// Поддерживает русский, английский и испанский языки
/// </summary>
public class LocalizationManagr : MonoBehaviour
{
    public enum Language
    {
        Russian,
        English,
        Spanish
    }

    private static LocalizationManagr instance;
    public static LocalizationManagr Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LocalizationManager");
                instance = go.AddComponent<LocalizationManagr>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Настройки")]
    [SerializeField] private Language currentLanguage = Language.Russian;
    [SerializeField] private bool autoDetectLanguage = true;

    // Словари переводов
    private Dictionary<string, Dictionary<Language, string>> translations;

    // События для оповещения UI об изменении языка
    public static event Action OnLanguageChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeTranslations();
        LoadSavedLanguage();
    }

    /// <summary>
    /// Инициализация всех переводов
    /// </summary>
    private void InitializeTranslations()
    {
        translations = new Dictionary<string, Dictionary<Language, string>>();

        // ========== ГЛАВНОЕ МЕНЮ ==========
        AddTranslation("menu.singleplayer",
            "Одиночная игра",
            "Single Player",
            "Un jugador");

        AddTranslation("menu.multiplayer",
            "Сетевая игра",
            "Multiplayer",
            "Multijugador");

        AddTranslation("menu.about",
            "Об авторе",
            "About Author",
            "Sobre el autor");

        AddTranslation("menu.settings",
            "Настройки",
            "Settings",
            "Configuración");

        AddTranslation("menu.exit",
            "Выход",
            "Exit",
            "Salir");

        // ========== МУЛЬТИПЛЕЕР ==========
        AddTranslation("mp.createroom",
            "Создать комнату",
            "Create Room",
            "Crear sala");

        AddTranslation("mp.joinroom",
            "Присоединиться",
            "Join Room",
            "Unirse a sala");

        AddTranslation("mp.back",
            "Назад",
            "Back",
            "Atrás");

        AddTranslation("mp.roomname",
            "Название комнаты",
            "Room Name",
            "Nombre de sala");

        AddTranslation("mp.errorroomname",
            "Такой комнаты не существует",
            "This room doesnt exist!",
            "Esta habitacion no existe!");

        AddTranslation("mp.enterroomname",
            "Введите название комнаты...",
            "Enter room name...",
            "Ingrese nombre de sala...");

        AddTranslation("mp.startgame",
            "Запустить карту",
            "Start Game",
            "Iniciar juego");

        AddTranslation("mp.players",
            "Игроки",
            "Players",
            "Jugadores");


        // ========== ОБ АВТОРЕ ==========
        AddTranslation("about.label",
            "ОБ АВТОРЕ",
            "ABOUT AUTHOUR",
            "sobre el autor");
        AddTranslation("about.text1",
         "Мы — два студента, которые решили превратить курсовую работу в настоящий вызов. Эта игра — результат нашей страсти к разработке и любви к симуляторам. Авторы: Матвей и Евгений. Если есть вопросы или предложения, можете написать на одну из почт ниже. ",
         "We are two students who decided to turn their coursework into a real challenge. This game is the result of our passion for development and our love for simulators. The authors are Matvey and Evgeny. If you have any questions or suggestions, please feel free to contact us via the email addresses provided below.",
         "Somos dos estudiantes que decidimos convertir el trabajo del curso en un verdadero desafío. Este juego es el resultado de nuestra pasión por el desarrollo y el amor por los simuladores. Autores: Mateo y Eugenio. Si tiene preguntas o sugerencias, puede escribir a uno de los correos a continuación.");
        AddTranslation("about.text2",
            "korneichikwork@gmail.com, nelipovi4evgen@gmail.com",
            "korneichikwork@gmail.com, nelipovi4evgen@gmail.com",
            "korneichikwork@gmail.com, nelipovi4evgen@gmail.com");
        AddTranslation("about.contacts",
            "Контакты для связи",
            "Contacts for communication",
            "Contactos para la comunicación");

        // ========== ОШИБКИ ВАЛИДАЦИИ ==========
        AddTranslation("error.empty",
            "Название комнаты не может быть пустым",
            "Room name cannot be empty",
            "El nombre de sala no puede estar vacío");

        AddTranslation("error.tooshort",
            "Название слишком короткое (минимум 3 символов)",
            "Name too short (minimum 3 characters)",
            "Nombre muy corto (mínimo 3 caracteres)");

        AddTranslation("error.toolong",
            "Название слишком длинное (максимум 20 символов)",
            "Name too long (maximum 20 characters)",
            "Nombre muy largo (máximo 20 caracteres)");

        AddTranslation("error.latinonly",
            "Используйте только латинские буквы",
            "Use only Latin letters",
            "Use solo letras latinas");

        AddTranslation("error.specialchars",
            "Спецсимволы запрещены (!@#$%^&* и т.д.)",
            "Special characters forbidden (!@#$%^&* etc.)",
            "Caracteres especiales prohibidos (!@#$%^&* etc.)");

        AddTranslation("error.onlynumbers",
            "Название не может состоять только из цифр",
            "Name cannot consist only of numbers",
            "El nombre no puede ser solo números");

        AddTranslation("error.repeating",
            "Слишком много повторяющихся символов подряд",
            "Too many repeating characters in a row",
            "Demasiados caracteres repetidos seguidos");

        AddTranslation("error.bannedword",
            "Слово '{0}' запрещено",
            "Word '{0}' is forbidden",
            "Palabra '{0}' está prohibida");

        AddTranslation("error.suspicious",
            "Обнаружена подозрительная последовательность",
            "Suspicious sequence detected",
            "Secuencia sospechosa detectada");

        AddTranslation("error.createfailed",
            "Не удалось создать комнату: {0}",
            "Failed to create room: {0}",
            "No se pudo crear sala: {0}");

        AddTranslation("error.joinfailed",
            "Не удалось присоединиться: {0}",
            "Failed to join: {0}",
            "No se pudo unir: {0}");

        // ========== ПАУЗА ==========
        AddTranslation("pause.title",
            "Пауза",
            "Pause",
            "Pausa");

        AddTranslation("pause.resume",
            "Продолжить",
            "Resume",
            "Reanudar");

        AddTranslation("pause.menu",
            "Меню",
            "Menu",
            "Menú");

        AddTranslation("pause.quit",
            "Выход из игры",
            "Exit the game",
            "Salir del juego");

        AddTranslation("pause.quittomenu",
            "Выход в главное меню",
            "Quit to menu",
            "Salir del menú");

        AddTranslation("pause.help",
            "Руководство",
            "Help",
            "Ayuda");

        // ========== УПРАВЛЕНИЕ ==========
        AddTranslation("controls.wasd",
            "WASD - Управление",
            "WASD - Movement",
            "WASD - Movimiento");

        AddTranslation("controls.space",
            "Пробел - Ручной тормоз",
            "Space - Handbrake",
            "Espacio - Freno de mano");

        AddTranslation("controls.shift",
            "Shift - Ускорение",
            "Shift - Boost",
            "Shift - Aceleración");

        AddTranslation("controls.v",
            "V - Скрыть/Показать HUD",
            "V - Hide/Show HUD",
            "V - Ocultar/Mostrar HUD");

        AddTranslation("controls.q",
            "Q - Левый поворотник",
            "Q - Left Turn Signal",
            "Q - Intermitente izquierdo");

        AddTranslation("controls.e",
            "E - Правый поворотник",
            "E - Right Turn Signal",
            "E - Intermitente derecho");

        AddTranslation("controls.esc",
            "ESC - Меню/Назад",
            "ESC - Menu/Back",
            "ESC - Menú/Atrás");

        // ========== СТАТИСТИКА ==========
        AddTranslation("stats.speed",
            "Скорость",
            "Speed",
            "Velocidad");

        AddTranslation("stats.distance",
            "Пройдено",
            "Distance",
            "Distancia");

        AddTranslation("stats.time",
            "Время",
            "Time",
            "Tiempo");

        AddTranslation("stats.collisions",
            "Столкновений",
            "Collisions",
            "Colisiones");

        // ========== НАСТРОЙКИ ==========
        AddTranslation("settings.label",
            "Настройки",
            "SETTINGS",
            "AJUSTES");

        AddTranslation("settings.language",
            "Язык",
            "Language",
            "Idioma");

        AddTranslation("settings.graphics",
            "Графика",
            "Graphics",
            "Gráficos");

        AddTranslation("settings.audio",
            "Звук",
            "Audio",
            "Audio");

        AddTranslation("settings.controls",
            "Управление",
            "Controls",
            "Controles");

        AddTranslation("settings.apply",
            "Применить",
            "Apply",
            "Aplicar");

        AddTranslation("settings.cancel",
            "Отмена",
            "Cancel",
            "Cancelar");

        // ========== ГРАФИКА ==========
        AddTranslation("settings.graphics.quality",
            "Качество картинки",
            "Image Quality",
            "Calidad de imagen");

        AddTranslation("settings.graphics.fps",
            "Макс. fps",
            "Max FPS",
            "FPS máximo");

        AddTranslation("settings.graphics.vsync",
            "Верт. синхронизация",
            "V-Sync",
            "Sincronización vert.");

        AddTranslation("settings.graphics.gamma",
            "Гамма",
            "Gamma",
            "Gamma");

        AddTranslation("settings.graphics.enterfps",
            "Введите ФПС",
            "Enter FPS",
            "Intoduzca FPS");

        AddTranslation("settings.graphics.default",
            "По умолчанию",
            "Default",
            "Por defecto");

        AddTranslation("settings.graphics.save",
            "Сохранить",
            "Save",
            "Guardar");

        // ========== ЗВУК ==========
        AddTranslation("settings.sounds.volume",
            "Громкость звука",
            "Volume",
            "Volumen");

        AddTranslation("settings.sounds.musicvolume",
            "Громкость музыки в меню",
            "Music volume in the menu",
            "Volumen de la musica en el menu");

        // ========== ЯЗЫК ==========
        AddTranslation("settings.language.language",
            "Язык",
            "Language",
            "Idioma");
        AddTranslation("settings.language.russian",
            "Русский",
            "Russian",
            "Rusa");
        AddTranslation("settings.language.english",
            "Английский",
            "English",
            "Inglés");
        AddTranslation("settings.language.spanish",
            "Испанский",
            "Spanish",
            "Español");

        // ========== ОБЩИЕ ==========
        AddTranslation("common.yes",
            "Да",
            "Yes",
            "Sí");

        AddTranslation("common.no",
            "Нет",
            "No",
            "No");

        AddTranslation("common.ok",
            "ОК",
            "OK",
            "OK");

        AddTranslation("common.loading",
            "Загрузка...",
            "Loading...",
            "Cargando...");

        AddTranslation("common.connecting",
            "Подключение...",
            "Connecting...",
            "Conectando...");

        GameLogger.Log(GameLogger.LogCategory.System,
            $"Локализация инициализирована. Всего ключей: {translations.Count}");
    }

    /// <summary>
    /// Добавить перевод для одного ключа
    /// </summary>
    private void AddTranslation(string key, string russian, string english, string spanish)
    {
        if (!translations.ContainsKey(key))
        {
            translations[key] = new Dictionary<Language, string>();
        }

        translations[key][Language.Russian] = russian;
        translations[key][Language.English] = english;
        translations[key][Language.Spanish] = spanish;
    }

    /// <summary>
    /// Получить перевод по ключу
    /// </summary>
    public static string Get(string key, params object[] args)
    {
        return Instance.GetTranslation(key, args);
    }

    private string GetTranslation(string key, params object[] args)
    {
        if (translations.ContainsKey(key))
        {
            if (translations[key].ContainsKey(currentLanguage))
            {
                string translation = translations[key][currentLanguage];

                // Поддержка форматирования строк {0}, {1} и т.д.
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(translation, args);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Ошибка форматирования перевода '{key}': {e.Message}");
                        return translation;
                    }
                }

                return translation;
            }
        }

        // Если перевод не найден - возвращаем сам ключ
        Debug.LogWarning($"Перевод не найден для ключа: {key}");
        return $"[{key}]";
    }

    /// <summary>
    /// Установить язык
    /// </summary>
    public static void SetLanguage(Language language)
    {
        Instance.currentLanguage = language;
        PlayerPrefs.SetInt("GameLanguage", (int)language);
        PlayerPrefs.Save();

        GameLogger.Log(GameLogger.LogCategory.System,
            $"Язык изменен на: {language}");

        // Оповещаем всех слушателей об изменении языка
        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// Получить текущий язык
    /// </summary>
    public static Language GetCurrentLanguage()
    {
        return Instance.currentLanguage;
    }

    /// <summary>
    /// Загрузить сохраненный язык
    /// </summary>
    private void LoadSavedLanguage()
    {
        if (autoDetectLanguage && !PlayerPrefs.HasKey("GameLanguage"))
        {
            // Автоопределение языка по системным настройкам
            SystemLanguage sysLang = Application.systemLanguage;

            switch (sysLang)
            {
                case SystemLanguage.Russian:
                case SystemLanguage.Ukrainian:
                case SystemLanguage.Belarusian:
                    currentLanguage = Language.Russian;
                    break;

                case SystemLanguage.Spanish:
                    currentLanguage = Language.Spanish;
                    break;

                default:
                    currentLanguage = Language.English;
                    break;
            }

            GameLogger.Log(GameLogger.LogCategory.System,
                $"Язык автоопределен: {currentLanguage} (системный: {sysLang})");
        }
        else
        {
            int savedLang = PlayerPrefs.GetInt("GameLanguage", (int)currentLanguage);
            currentLanguage = (Language)savedLang;

            GameLogger.Log(GameLogger.LogCategory.System,
                $"Язык загружен из настроек: {currentLanguage}");
        }
    }

    /// <summary>
    /// Получить название языка на текущем языке
    /// </summary>
    public static string GetLanguageName(Language language)
    {
        switch (language)
        {
            case Language.Russian:
                return "Русский";
            case Language.English:
                return "English";
            case Language.Spanish:
                return "Español";
            default:
                return language.ToString();
        }
    }

    /// <summary>
    /// Получить все доступные языки
    /// </summary>
    public static Language[] GetAvailableLanguages()
    {
        return (Language[])Enum.GetValues(typeof(Language));
    }
}