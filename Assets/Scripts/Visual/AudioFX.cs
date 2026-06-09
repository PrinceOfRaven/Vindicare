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

    static AudioClip _shoot, _shootRifle, _shootShotgun, _shootSniper,
                     _enemyHit, _enemyDeath, _playerHit, _playerDeath,
                     _pickup, _levelUp, _explosion, _uiClick,
                     _dash, _shield, _overdrive, _turret;

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

    public static void ShootRifle()
    {
        if (!_ready) return;
        if (Time.unscaledTime - _lastShootTime < 0.04f) return;
        _lastShootTime = Time.unscaledTime;
        Play(_shootRifle, 0.34f, 0.95f, 1.06f);
    }

    public static void ShootShotgun()
    {
        if (!_ready) return;
        // Дробовик: два слоя — мясистый бас + резкий верх.
        Play(_shootShotgun, 0.75f, 0.93f, 1.0f);
        Play(_shootRifle, 0.25f, 0.8f, 0.9f);
    }

    public static void ShootSniper()
    {
        if (!_ready) return;
        Play(_shootSniper, 0.7f, 0.97f, 1.03f);
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

    public static void Dash() => Play(_dash, 0.5f, 0.97f, 1.06f);
    public static void Shield() => Play(_shield, 0.55f);
    public static void Overdrive() => Play(_overdrive, 0.6f);
    public static void TurretDeploy() => Play(_turret, 0.5f, 0.82f, 0.9f);
    public static void TurretShot() => Play(_turret, 0.22f, 1.08f, 1.22f);

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
        _shootRifle  = BuildShootRifle();
        _shootShotgun= BuildShootShotgun();
        _shootSniper = BuildShootSniper();
        _enemyHit    = BuildEnemyHit();
        _enemyDeath  = BuildEnemyDeath();
        _playerHit   = BuildPlayerHit();
        _playerDeath = BuildPlayerDeath();
        _pickup      = BuildPickup();
        _levelUp     = BuildLevelUp();
        _explosion   = BuildExplosion();
        _uiClick     = BuildUIClick();
        _dash        = BuildDash();
        _shield      = BuildShield();
        _overdrive   = BuildOverdrive();
        _turret      = BuildTurret();
    }

    // Рывок: восходяще-нисходящий «вжух» — тональный свип вниз, замешанный с шумом, конверт-свелл.
    static AudioClip BuildDash()
    {
        const float dur = 0.22f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(720f, 170f, p);
            phase += freq / SampleRate;
            float sine = Mathf.Sin((phase - Mathf.Floor(phase)) * TwoPi);
            float noise = Random.Range(-1f, 1f);
            float body = Mathf.Lerp(noise, sine, 0.4f);
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(p));
            d[i] = body * env * 0.6f;
        }
        return Finish("sfx_dash", d);
    }

    // Щит: восходящий мерцающий аккорд (основной тон + квинта), мягкая атака и долгий спад.
    static AudioClip BuildShield()
    {
        const float dur = 0.5f;
        var d = Buffer(dur);
        float ph1 = 0f, ph2 = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float f1 = Mathf.Lerp(240f, 520f, p);
            ph1 += f1 / SampleRate;
            ph2 += (f1 * 1.5f) / SampleRate;
            float wave = Mathf.Sin((ph1 - Mathf.Floor(ph1)) * TwoPi)
                       + 0.5f * Mathf.Sin((ph2 - Mathf.Floor(ph2)) * TwoPi);
            float attack = 1f - Mathf.Exp(-t * 18f);
            float decay = Mathf.Exp(-t * 2.6f);
            d[i] = wave * attack * decay * 0.4f;
        }
        return Finish("sfx_shield", d);
    }

    // Овердрайв: нарастающий пилообразный «разгон» с суб-октавой и лёгким вибрато.
    static AudioClip BuildOverdrive()
    {
        const float dur = 0.55f;
        var d = Buffer(dur);
        float ph = 0f, phSub = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float vib = 1f + 0.02f * Mathf.Sin(t * 38f);
            float freq = Mathf.Lerp(120f, 300f, p) * vib;
            ph += freq / SampleRate;
            phSub += (freq * 0.5f) / SampleRate;
            float saw = 2f * (ph - Mathf.Floor(ph)) - 1f;
            float sub = 2f * (phSub - Mathf.Floor(phSub)) - 1f;
            float mix = saw * 0.6f + sub * 0.4f;
            mix = Mathf.Clamp(mix * 1.4f, -1f, 1f); // лёгкий перегруз
            float env = (1f - Mathf.Exp(-t * 10f)) * Mathf.Lerp(1f, 0.7f, p);
            d[i] = mix * env * 0.45f;
        }
        return Finish("sfx_overdrive", d);
    }

    // Турель/лазер: металлический «зэп» — высокий свип вниз + шумовой щелчок, быстрый спад.
    static AudioClip BuildTurret()
    {
        const float dur = 0.12f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(1900f, 560f, p * p);
            phase += freq / SampleRate;
            float cyc = phase - Mathf.Floor(phase);
            float sq = cyc < 0.5f ? 0.5f : -0.5f;
            float click = Random.Range(-1f, 1f) * 0.4f * Mathf.Exp(-t * 80f);
            float env = Mathf.Exp(-t * 30f);
            d[i] = (sq + click) * env;
        }
        return Finish("sfx_turret", d);
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

    // Винтовка: резкий короткий «крак» — быстрый свип вниз + шумовой щелчок, очень быстрый спад.
    static AudioClip BuildShootRifle()
    {
        const float dur = 0.09f;
        var d = Buffer(dur);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(1500f, 420f, p * p);
            phase += freq / SampleRate;
            float cyc = phase - Mathf.Floor(phase);
            float sq = cyc < 0.5f ? 0.6f : -0.6f;
            float noise = Random.Range(-1f, 1f) * 0.5f * Mathf.Exp(-t * 90f);
            float env = Mathf.Exp(-t * 42f);
            d[i] = Mathf.Clamp((sq * env) + noise, -1f, 1f);
        }
        return Finish("sfx_shoot_rifle", d);
    }

    // Дробовик: низкий жирный «БУХ» — фильтрованный нойз-взрыв + суб-бас + начальный щелчок.
    static AudioClip BuildShootShotgun()
    {
        const float dur = 0.34f;
        var d = Buffer(dur);
        float phase = 0f;
        float lp = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float noise = Random.Range(-1f, 1f);
            lp += (noise - lp) * 0.18f;                 // низкочастотный нойз = «мясо»
            float boom = lp * 0.9f * Mathf.Exp(-t * 11f);
            float subFreq = Mathf.Lerp(85f, 38f, p);
            phase += subFreq / SampleRate;
            float sub = Mathf.Sin(phase * TwoPi) * 0.8f * Mathf.Exp(-t * 6f);
            float crack = Random.Range(-1f, 1f) * Mathf.Exp(-t * 120f) * 0.6f;
            d[i] = Mathf.Clamp(boom + sub + crack, -1f, 1f);
        }
        return Finish("sfx_shoot_shotgun", d);
    }

    // Снайперка: railgun — яркий электро-разряд (свип сверху вниз) + бас-удар + лёгкий «звон».
    static AudioClip BuildShootSniper()
    {
        const float dur = 0.36f;
        var d = Buffer(dur);
        float zapPhase = 0f, bassPhase = 0f, ringPhase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SampleRate;
            float p = t / dur;
            float zapFreq = Mathf.Lerp(2600f, 240f, Mathf.Sqrt(p));
            zapPhase += zapFreq / SampleRate;
            float zcyc = zapPhase - Mathf.Floor(zapPhase);
            float zap = (zcyc < 0.5f ? 0.5f : -0.5f) * Mathf.Exp(-t * 13f);
            float bassFreq = Mathf.Lerp(140f, 46f, p);
            bassPhase += bassFreq / SampleRate;
            float bass = Mathf.Sin(bassPhase * TwoPi) * 0.7f * Mathf.Exp(-t * 5f);
            ringPhase += 1800f / SampleRate;
            float ring = Mathf.Sin(ringPhase * TwoPi) * 0.18f * Mathf.Exp(-t * 22f);
            float crack = Random.Range(-1f, 1f) * Mathf.Exp(-t * 70f) * 0.4f;
            d[i] = Mathf.Clamp(zap + bass + ring + crack, -1f, 1f);
        }
        return Finish("sfx_shoot_sniper", d);
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
