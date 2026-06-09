using UnityEngine;

/// <summary>
/// Сохранение лучших результатов забега между сессиями (PlayerPrefs).
/// Считает итоговый счёт и определяет, какие рекорды побиты.
/// </summary>
public static class RunRecords
{
    private const string K_Time  = "vind_best_time";
    private const string K_Kills = "vind_best_kills";
    private const string K_Level = "vind_best_level";
    private const string K_Wave  = "vind_best_wave";
    private const string K_Score = "vind_best_score";

    public static float BestTime  => PlayerPrefs.GetFloat(K_Time, 0f);
    public static int   BestKills => PlayerPrefs.GetInt(K_Kills, 0);
    public static int   BestLevel => PlayerPrefs.GetInt(K_Level, 0);
    public static int   BestWave  => PlayerPrefs.GetInt(K_Wave, 0);
    public static int   BestScore => PlayerPrefs.GetInt(K_Score, 0);

    /// <summary>Итоговый счёт забега.</summary>
    public static int ComputeScore(float time, int kills, int level, int wave)
        => Mathf.RoundToInt(time) + kills * 10 + level * 100 + wave * 75;

    public struct Result
    {
        public int  Score;
        public bool NewScore;
        public bool NewTime;
        public bool NewKills;
        public bool NewLevel;
        public bool NewWave;
        public bool AnyRecord => NewScore || NewTime || NewKills || NewLevel || NewWave;
    }

    /// <summary>Подаёт результат забега, обновляет рекорды и возвращает, что было побито.</summary>
    public static Result Submit(float time, int kills, int level, int wave, int score)
    {
        var r = new Result { Score = score };

        if (r.Score > BestScore) { PlayerPrefs.SetInt(K_Score, r.Score); r.NewScore = true; }
        if (time   > BestTime)   { PlayerPrefs.SetFloat(K_Time, time);   r.NewTime  = true; }
        if (kills  > BestKills)  { PlayerPrefs.SetInt(K_Kills, kills);   r.NewKills = true; }
        if (level  > BestLevel)  { PlayerPrefs.SetInt(K_Level, level);   r.NewLevel = true; }
        if (wave   > BestWave)   { PlayerPrefs.SetInt(K_Wave, wave);     r.NewWave  = true; }

        PlayerPrefs.Save();
        return r;
    }

    public static string FormatTime(float seconds)
    {
        int min = (int)(seconds / 60);
        int sec = (int)(seconds % 60);
        return $"{min:00}:{sec:00}";
    }
}
