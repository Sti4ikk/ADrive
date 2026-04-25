using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Валидатор для названий комнат и никнеймов с использованием регулярных выражений
/// </summary>
public class RoomValidator : MonoBehaviour
{
    [System.Serializable]
    public class ValidationResult
    {
        public bool isValid;
        public string errorMessage;
        public string sanitizedValue; // Очищенное значение

        public ValidationResult(bool valid, string error = "", string sanitized = "")
        {
            isValid = valid;
            errorMessage = error;
            sanitizedValue = sanitized;
        }
    }

    // Настройки валидации
    [Header("Правила для названий комнат")]
    [SerializeField] private int roomNameMinLength = 3;
    [SerializeField] private int roomNameMaxLength = 20;
    [SerializeField] private bool allowSpacesInRoomName = true;
    [SerializeField] private bool allowNumbersInRoomName = true;

    [Header("Правила для никнеймов")]
    [SerializeField] private int nicknameMinLength = 3;
    [SerializeField] private int nicknameMaxLength = 15;
    [SerializeField] private bool allowSpacesInNickname = false;

    [Header("Запрещенные слова")]
    [SerializeField]
    private string[] bannedWords = new string[]
    {
        "admin", "moderator", "test", "debug"
    };

    // Регулярные выражения для различных проверок
    private static class Patterns
    {
        // Только латиница, цифры, дефис и подчеркивание
        public const string AlphanumericOnly = @"^[a-zA-Z0-9_-]+$";

        // Латиница, цифры, пробелы, дефис и подчеркивание
        public const string AlphanumericWithSpaces = @"^[a-zA-Z0-9\s_-]+$";

        // Только латиница (без цифр)
        public const string LettersOnly = @"^[a-zA-Z_-]+$";

        // Проверка на несколько пробелов подряд
        public const string MultipleSpaces = @"\s{2,}";

        // Проверка на спецсимволы
        public const string SpecialChars = @"[!@#$%^&*()+=\[\]{};:'"",.<>?/\\|`~]";

        // Проверка на эмодзи и нелатинские символы
        public const string NonLatinChars = @"[^\x00-\x7F]";

        // Проверка начала/конца на пробел или спецсимвол
        public const string StartsOrEndsWithSpecial = @"(^[\s_-]|[\s_-]$)";

        // Только цифры (недопустимое имя)
        public const string OnlyDigits = @"^\d+$";

        // Повторяющиеся символы (больше 3 подряд)
        public const string RepeatingChars = @"(.)\1{3,}";
    }

    /// <summary>
    /// Главный метод валидации названия комнаты
    /// </summary>
    public ValidationResult ValidateRoomName(string roomName)
    {
        // 1. Проверка на пустоту
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return new ValidationResult(false, "Название комнаты не может быть пустым");
        }

        // 2. Очистка от лишних пробелов
        string cleaned = roomName.Trim();
        cleaned = Regex.Replace(cleaned, Patterns.MultipleSpaces, " ");

        // 3. Проверка длины
        if (cleaned.Length < roomNameMinLength)
        {
            return new ValidationResult(false,
                $"Название слишком короткое (минимум {roomNameMinLength} символов)");
        }

        if (cleaned.Length > roomNameMaxLength)
        {
            return new ValidationResult(false,
                $"Название слишком длинное (максимум {roomNameMaxLength} символов)");
        }

        // 4. Проверка на нелатинские символы и эмодзи
        if (Regex.IsMatch(cleaned, Patterns.NonLatinChars))
        {
            return new ValidationResult(false, "Используйте только латинские буквы");
        }

        // 5. Проверка на спецсимволы
        if (Regex.IsMatch(cleaned, Patterns.SpecialChars))
        {
            return new ValidationResult(false, "Спецсимволы запрещены (!@#$%^&* и т.д.)");
        }

        // 6. Проверка начала/конца
        if (Regex.IsMatch(cleaned, Patterns.StartsOrEndsWithSpecial))
        {
            return new ValidationResult(false, "Название не может начинаться или заканчиваться пробелом/спецсимволом");
        }

        // 7. Проверка на только цифры
        if (Regex.IsMatch(cleaned, Patterns.OnlyDigits))
        {
            return new ValidationResult(false, "Название не может состоять только из цифр");
        }

        // 8. Проверка на повторяющиеся символы
        if (Regex.IsMatch(cleaned, Patterns.RepeatingChars))
        {
            return new ValidationResult(false, "Слишком много повторяющихся символов подряд");
        }

        // 9. Проверка разрешенных символов
        string pattern = allowSpacesInRoomName
            ? Patterns.AlphanumericWithSpaces
            : Patterns.AlphanumericOnly;

        if (!allowNumbersInRoomName)
        {
            pattern = Patterns.LettersOnly;
        }

        if (!Regex.IsMatch(cleaned, pattern))
        {
            return new ValidationResult(false, "Недопустимые символы в названии");
        }

        // 10. Проверка на запрещенные слова
        foreach (string banned in bannedWords)
        {
            // Регистронезависимая проверка
            string banPattern = $@"\b{Regex.Escape(banned)}\b";
            if (Regex.IsMatch(cleaned, banPattern, RegexOptions.IgnoreCase))
            {
                return new ValidationResult(false, $"Слово '{banned}' запрещено");
            }
        }

        // 11. Проверка на "безопасность" - нет SQL инъекций
        if (ContainsSQLInjection(cleaned))
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network,
                $"Попытка SQL инъекции в названии комнаты: {cleaned}");
            return new ValidationResult(false, "Обнаружена подозрительная последовательность");
        }

        // ВСЁ ОК!
        GameLogger.Log(GameLogger.LogCategory.Network,
            $"Название комнаты валидировано успешно: '{cleaned}'");

        return new ValidationResult(true, "", cleaned);
    }

    /// <summary>
    /// Валидация никнейма игрока
    /// </summary>
    public ValidationResult ValidateNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return new ValidationResult(false, "Никнейм не может быть пустым");
        }

        string cleaned = nickname.Trim();
        cleaned = Regex.Replace(cleaned, Patterns.MultipleSpaces, " ");

        if (cleaned.Length < nicknameMinLength)
        {
            return new ValidationResult(false,
                $"Никнейм слишком короткий (минимум {nicknameMinLength} символов)");
        }

        if (cleaned.Length > nicknameMaxLength)
        {
            return new ValidationResult(false,
                $"Никнейм слишком длинный (максимум {nicknameMaxLength} символов)");
        }

        if (Regex.IsMatch(cleaned, Patterns.NonLatinChars))
        {
            return new ValidationResult(false, "Используйте только латинские буквы");
        }

        if (Regex.IsMatch(cleaned, Patterns.SpecialChars))
        {
            return new ValidationResult(false, "Спецсимволы запрещены");
        }

        if (Regex.IsMatch(cleaned, Patterns.StartsOrEndsWithSpecial))
        {
            return new ValidationResult(false, "Никнейм не может начинаться или заканчиваться пробелом");
        }

        if (Regex.IsMatch(cleaned, Patterns.OnlyDigits))
        {
            return new ValidationResult(false, "Никнейм не может состоять только из цифр");
        }

        string pattern = allowSpacesInNickname
            ? Patterns.AlphanumericWithSpaces
            : Patterns.AlphanumericOnly;

        if (!Regex.IsMatch(cleaned, pattern))
        {
            return new ValidationResult(false, "Недопустимые символы в никнейме");
        }

        // Проверка на запрещенные слова
        foreach (string banned in bannedWords)
        {
            string banPattern = $@"\b{Regex.Escape(banned)}\b";
            if (Regex.IsMatch(cleaned, banPattern, RegexOptions.IgnoreCase))
            {
                return new ValidationResult(false, $"Слово '{banned}' запрещено");
            }
        }

        GameLogger.Log(GameLogger.LogCategory.Network,
            $"Никнейм валидирован успешно: '{cleaned}'");

        return new ValidationResult(true, "", cleaned);
    }

    /// <summary>
    /// Проверка на SQL инъекции и другие опасные паттерны
    /// </summary>
    private bool ContainsSQLInjection(string input)
    {
        string[] dangerousPatterns = new string[]
        {
            @"(\bSELECT\b|\bINSERT\b|\bUPDATE\b|\bDELETE\b|\bDROP\b)",
            @"(--|;|\/\*|\*\/)",
            @"(\bOR\b\s+\d+\s*=\s*\d+)",
            @"(\bAND\b\s+\d+\s*=\s*\d+)",
            @"('|"")(\s*OR\s*|\s*AND\s*)('|"")",
        };

        foreach (string pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Автоматическая очистка и исправление названия
    /// </summary>
    public string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        // Убираем все нелатинские символы
        string sanitized = Regex.Replace(input, Patterns.NonLatinChars, "");

        // Убираем спецсимволы
        sanitized = Regex.Replace(sanitized, Patterns.SpecialChars, "");

        // Убираем множественные пробелы
        sanitized = Regex.Replace(sanitized, Patterns.MultipleSpaces, " ");

        // Trim
        sanitized = sanitized.Trim();

        GameLogger.Log(GameLogger.LogCategory.System,
            $"Санитизация: '{input}' -> '{sanitized}'");

        return sanitized;
    }

    /// <summary>
    /// Генерация случайного валидного названия комнаты
    /// </summary>
    public string GenerateRandomRoomName()
    {
        string[] prefixes = { "Game", "Room", "Arena", "Battle", "Race", "Drive" };
        string[] suffixes = { "Pro", "Elite", "Master", "King", "Champion" };

        int randomNum = UnityEngine.Random.Range(100, 999);

        string prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
        string suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Length)];

        return $"{prefix}_{suffix}_{randomNum}";
    }

    /// <summary>
    /// Проверка уникальности названия комнаты (с учетом регистра)
    /// </summary>
    public bool IsRoomNameUnique(string roomName, string[] existingRooms)
    {
        string normalizedName = roomName.ToLower().Trim();

        foreach (string existing in existingRooms)
        {
            if (existing.ToLower().Trim() == normalizedName)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Извлечение тега из названия комнаты (например, [RU] MyRoom)
    /// </summary>
    public (string tag, string roomName) ExtractRoomTag(string input)
    {
        // Паттерн для тега в квадратных скобках в начале
        string pattern = @"^\[([A-Z]{2,3})\]\s*(.+)$";
        var match = Regex.Match(input, pattern);

        if (match.Success)
        {
            string tag = match.Groups[1].Value;
            string name = match.Groups[2].Value.Trim();
            return (tag, name);
        }

        return ("", input);
    }
}