using UnityEngine;
using UnityEngine.SceneManagement;

public static class AudioFX
{
    const int SampleRate = 44100;
    const float TwoPi = Mathf.PI * 2f;

    static GameObject _root;
    static AudioSource _music;
    static AudioSource[] _sfxPool;
    static int _sfxIndex;
    static bool _ready;

    static AudioClip _shoot, _enemyHit, _enemyDeath, _playerHit, _playerDeath,
                     _pickup, _levelUp, _explosion, _uiClick;

    static float _lastShootTime = -10f;
    static float _lastEnemyHitTime = -10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInit();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryInit();

    static void TryInit()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name == "MainMenu" || scene.name == "PlayerHub") return;
        EnsureSetup();
    }

    static void EnsureSetup()
    {
        if (_ready) return;

        _root = new GameObject("[Audio]");
        Object.DontDestroyOnLoad(_root);

        if (Object.FindAnyObjectByType<AudioListener>() == null)
            _root.AddComponent<AudioListener>();

        _music = _root.AddComponent<AudioSource>();
        _music.loop = true;
        _music.playOnAwake = false;
        _music.spatialBlend = 0f;
        _music.volume = 0.16f;
        var musicClip = Resources.Load<AudioClip>("Music/death-squad");
        if (musicClip != null)
        {
            _music.clip = musicClip;
            _music.Play();
        }

        _sfxPool = new AudioSource[6];
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            var src = _root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            _sfxPool[i] = src;
        }

        BuildClips();
        _ready = true;
    }

    public static void Shoot()
    {
        if (!_ready) return;
        if (Time.unscaledTime - _lastShootTime < 0.04f) return;
        _lastShootTime = Time.unscaledTime;
        Play(_shoot, 0.30f, 0.92f, 1.09f);
    }

    public static void EnemyHit()
    {
        if (!_ready) return;
        if (Time.unscaledTime - _lastEnemyHitTime < 0.035f) return;
        _lastEnemyHitTime = Time.unscaledTime;
        Play(_enemyHit, 0.32f, 0.88f, 1.16f);
    }

    public static void EnemyDeath() => Play(_enemyDeath, 0.42f, 0.94f, 1.08f);
    public static void PlayerHit() => Play(_playerHit, 0.6f, 0.96f, 1.04f);
    public static void PlayerDeath() => Play(_playerDeath, 0.8f);
    public static void Pickup() => Play(_pickup, 0.28f, 0.96f, 1.07f);
    public static void LevelUp() => Play(_levelUp, 0.5f);
    public static void Explosion() => Play(_explosion, 0.85f, 0.95f, 1.05f);
    public static void UIClick() => Play(_uiClick, 0.4f, 0.97f, 1.03f);

    static void Play(AudioClip clip, float volume, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (!_ready || clip == null) return;
        var src = _sfxPool[_sfxIndex];
        _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.PlayOneShot(clip, volume);
    }

    static void BuildClips()
    {
        _shoot       = BuildShoot();
        _enemyHit    = BuildEnemyHit();
        _enemyDeath  = BuildEnemyDeath();
        _playerHit   = BuildPlayerHit();
        _playerDeath = BuildPlayerDeath();
        _pickup      = BuildPickup();
        _levelUp     = BuildLevelUp();
        _explosion   = BuildExplosion();
        _uiClick     = BuildUIClick();
    }

    static float[] Buffer(float duration) =>
        new float[Mathf.Max(1, (int)(duration * SampleRate))];

    static AudioClip Finish(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip BuildShoot()
    {
        const float dur = 0.11f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(900f, 280f, p);
            phase += freq / SampleRate;
            float cyc = phase - Mathf.Floor(phase);
            float wave = cyc < 0.5f ? 0.55f : -0.55f;
            float env = Mathf.Exp(-t * 26f);
            d[i] = wave * env;
        }
        return Finish("sfx_shoot", d);
    }

    static AudioClip BuildEnemyHit()
    {
        const float dur = 0.055f;
        var d = Buffer(dur);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float env = Mathf.Exp(-t * 80f);
            d[i] = Random.Range(-1f, 1f) * 0.45f * env;
        }
        return Finish("sfx_enemy_hit", d);
    }

    static AudioClip BuildEnemyDeath()
    {
        const float dur = 0.30f;
        var d = Buffer(dur);
        float phase = 0f;
        float lp = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(440f, 80f, p);
            phase += freq / SampleRate;
            float cyc = phase - Mathf.Floor(phase);
            float tone = (cyc < 0.5f ? 0.5f : -0.5f) * Mathf.Exp(-t * 9f);
            float noise = Random.Range(-1f, 1f);
            lp += (noise - lp) * 0.4f;
            float burst = lp * 0.6f * Mathf.Exp(-t * 17f);
            d[i] = Mathf.Clamp(tone + burst, -1f, 1f) * Mathf.Exp(-t * 5f);
        }
        return Finish("sfx_enemy_death", d);
    }

    static AudioClip BuildPlayerHit()
    {
        const float dur = 0.20f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            phase += 108f / SampleRate;
            float cyc = phase - Mathf.Floor(phase);
            float sq = cyc < 0.5f ? 0.5f : -0.5f;
            float env = Mathf.Exp(-t * 9f);
            d[i] = (sq + Random.Range(-1f, 1f) * 0.18f) * env;
        }
        return Finish("sfx_player_hit", d);
    }

    static AudioClip BuildPlayerDeath()
    {
        const float dur = 0.7f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(220f, 48f, p);
            phase += freq / SampleRate;
            float cyc = phase - Mathf.Floor(phase);
            float wave = cyc < 0.5f ? 0.5f : -0.5f;
            float env = Mathf.Exp(-t * 3.4f);
            d[i] = wave * env;
        }
        return Finish("sfx_player_death", d);
    }

    static AudioClip BuildPickup()
    {
        const float dur = 0.13f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(560f, 1080f, p);
            phase += freq / SampleRate;
            float s = Mathf.Sin(phase * TwoPi);
            float env = Mathf.Sin(p * Mathf.PI);
            d[i] = s * 0.4f * env;
        }
        return Finish("sfx_pickup", d);
    }

    static AudioClip BuildLevelUp()
    {
        const float noteDur = 0.14f;
        const float dur = noteDur * 3f;
        float[] notes = { 523.25f, 659.25f, 783.99f };
        var d = Buffer(dur);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            int n = Mathf.Min(2, (int)(t / noteDur));
            float lt = t - n * noteDur;
            float freq = notes[n];
            float sine = Mathf.Sin(TwoPi * freq * lt);
            float cyc = (freq * lt) - Mathf.Floor(freq * lt);
            float sq = cyc < 0.5f ? 1f : -1f;
            float env = Mathf.Exp(-lt * 8f);
            d[i] = (sine * 0.32f + sq * 0.12f) * env;
        }
        return Finish("sfx_levelup", d);
    }

    static AudioClip BuildExplosion()
    {
        const float dur = 0.55f;
        var d = Buffer(dur);
        float phase = 0f;
        float lp = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float noise = Random.Range(-1f, 1f);
            lp += (noise - lp) * 0.25f;
            float boom = lp * 0.7f * Mathf.Exp(-t * 7f);
            float rumbleFreq = Mathf.Lerp(95f, 32f, p);
            phase += rumbleFreq / SampleRate;
            float rumble = Mathf.Sin(phase * TwoPi) * 0.7f * Mathf.Exp(-t * 4f);
            d[i] = Mathf.Clamp(boom + rumble, -1f, 1f);
        }
        return Finish("sfx_explosion", d);
    }

    static AudioClip BuildUIClick()
    {
        const float dur = 0.045f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            phase += 1250f / SampleRate;
            float cyc = phase - Mathf.Floor(phase);
            float sq = cyc < 0.5f ? 0.4f : -0.4f;
            float env = Mathf.Exp(-t * 110f);
            d[i] = sq * env;
        }
        return Finish("sfx_uiclick", d);
    }
}
