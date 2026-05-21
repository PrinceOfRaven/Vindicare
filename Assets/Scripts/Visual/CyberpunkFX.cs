using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Единая точка визуальных эффектов: пост-обработка, глобальный 2D-свет,
/// процедурные партиклы, шейк, hit-stop, damage popup'ы.
/// Запускается автоматически после загрузки сцены — никаких ручных компонентов в сцене не нужно.
/// </summary>
public static class CyberpunkFX
{
    // ─── Cyberpunk-палитра ───
    public static readonly Color Cyan    = new Color(0.0f, 1.0f, 0.88f, 1f);
    public static readonly Color Magenta = new Color(1.0f, 0.18f, 0.80f, 1f);
    public static readonly Color Amber   = new Color(1.0f, 0.70f, 0.16f, 1f);
    public static readonly Color Lime    = new Color(0.45f, 1.0f, 0.30f, 1f);
    public static readonly Color HotRed  = new Color(1.0f, 0.20f, 0.30f, 1f);

    static Material _glowMat;
    static Material _trailMat;
    static CameraShake _shake;
    static MonoBehaviour _runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        TryBootstrapCurrent();
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        TryBootstrapCurrent();
    }

    static void TryBootstrapCurrent()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name == "MainMenu" || scene.name == "PlayerHub") return;

        EnsureRunner();
        EnsurePostProcessing();
        EnsureGlobalLight();
        EnsureCamera();
    }

    static void EnsureRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("[FX Runner]");
        Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<FXRunner>();
    }

    static void EnsurePostProcessing()
    {
        if (Object.FindAnyObjectByType<Volume>() != null) return;

        var go = new GameObject("[PostFX Volume]");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 100f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "CyberpunkProfile";
        vol.sharedProfile = profile;

        // Bloom: яркое неоновое свечение
        var bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.overrideState = true;   bloom.intensity.value = 1.4f;
        bloom.threshold.overrideState = true;   bloom.threshold.value = 0.85f;
        bloom.scatter.overrideState   = true;   bloom.scatter.value   = 0.75f;
        bloom.tint.overrideState      = true;   bloom.tint.value      = new Color(0.85f, 0.92f, 1f);
        bloom.highQualityFiltering.overrideState = true; bloom.highQualityFiltering.value = true;

        // Vignette: затемнение по краям
        var vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.color.overrideState     = true; vignette.color.value     = new Color(0.05f, 0.0f, 0.12f);
        vignette.intensity.overrideState = true; vignette.intensity.value = 0.42f;
        vignette.smoothness.overrideState= true; vignette.smoothness.value= 0.5f;
        vignette.rounded.overrideState   = true; vignette.rounded.value   = false;

        // Лёгкая хроматическая аберрация
        var ca = profile.Add<ChromaticAberration>(true);
        ca.active = true;
        ca.intensity.overrideState = true; ca.intensity.value = 0.18f;

        // Цветокор: чуть контраста, мягкий сине-фиолетовый сдвиг
        var color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.overrideState = true; color.postExposure.value = 0.15f;
        color.contrast.overrideState     = true; color.contrast.value     = 18f;
        color.saturation.overrideState   = true; color.saturation.value   = 12f;
        color.colorFilter.overrideState  = true; color.colorFilter.value  = new Color(0.92f, 0.90f, 1.05f);

        // Film grain
        var grain = profile.Add<FilmGrain>(true);
        grain.active = true;
        grain.intensity.overrideState = true; grain.intensity.value = 0.18f;
        grain.response.overrideState = true; grain.response.value = 0.8f;

        Object.DontDestroyOnLoad(go);
    }

    static void EnsureGlobalLight()
    {
        // Глобальный 2D-свет: тёмный фон с холодным синим оттенком
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
            if (l.lightType == Light2D.LightType.Global) return;

        var go = new GameObject("[Global Light2D]");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(0.32f, 0.38f, 0.55f);
        light.intensity = 0.55f;
    }

    static void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.allowHDR = true;
        cam.backgroundColor = new Color(0.025f, 0.015f, 0.05f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null) data.renderPostProcessing = true;

        if (cam.GetComponent<CameraShake>() == null)
            _shake = cam.gameObject.AddComponent<CameraShake>();
        else
            _shake = cam.GetComponent<CameraShake>();
    }

    // ─── Публичный API ───

    public static void Shake(float amplitude = 0.18f, float duration = 0.15f, float freq = 28f)
    {
        if (_shake == null && Camera.main != null)
        {
            _shake = Camera.main.GetComponent<CameraShake>() ?? Camera.main.gameObject.AddComponent<CameraShake>();
        }
        if (_shake != null) _shake.Shake(amplitude, duration, freq);
    }

    public static void HitStop(float seconds)
    {
        if (_runner == null) EnsureRunner();
        ((FXRunner)_runner).DoHitStop(seconds);
    }

    public static void DamagePopup(Vector3 worldPos, float amount, Color? color = null)
    {
        if (_runner == null) EnsureRunner();
        ((FXRunner)_runner).SpawnDamagePopup(worldPos, amount, color ?? Color.white);
    }

    public static void SpawnHitSpark(Vector3 pos, Color color)
    {
        var ps = BuildBurst(pos, color, count: 10, lifetime: 0.35f, sizeMin: 0.06f, sizeMax: 0.16f, speed: 5f, scatter: 360f);
        ps.gameObject.AddComponent<DestroyAfter>().lifetime = 0.6f;
        AddLightFlash(ps.gameObject, color, intensity: 1.8f, radius: 1.6f, duration: 0.18f);
    }

    public static void SpawnDeathBurst(Vector3 pos, Color color)
    {
        var ps = BuildBurst(pos, color, count: 28, lifetime: 0.6f, sizeMin: 0.1f, sizeMax: 0.24f, speed: 7f, scatter: 360f);
        ps.gameObject.AddComponent<DestroyAfter>().lifetime = 1.0f;
        AddLightFlash(ps.gameObject, color, intensity: 2.2f, radius: 2.2f, duration: 0.22f);
    }

    public static void SpawnPickupPop(Vector3 pos, Color color)
    {
        var ps = BuildBurst(pos, color, count: 8, lifetime: 0.3f, sizeMin: 0.06f, sizeMax: 0.12f, speed: 3f, scatter: 360f);
        ps.gameObject.AddComponent<DestroyAfter>().lifetime = 0.5f;
    }

    public static void SpawnExplosion(Vector3 pos, float radius, Color color)
    {
        var ps = BuildBurst(pos, color, count: 60, lifetime: 0.9f, sizeMin: 0.2f, sizeMax: 0.55f, speed: radius * 3.5f, scatter: 360f);
        ps.gameObject.AddComponent<DestroyAfter>().lifetime = 1.5f;
        AddLightFlash(ps.gameObject, color, intensity: 4f, radius: radius * 1.8f, duration: 0.4f);

        // Кольцо
        var ring = BuildRing(pos, color, radius);
        ring.gameObject.AddComponent<DestroyAfter>().lifetime = 0.8f;
    }

    // ─── Внутренности: построение партиклов и материалов ───

    static Material GlowMat()
    {
        if (_glowMat != null) return _glowMat;
        // URP/2D совместимый аддитивный шейдер для партиклов
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        _glowMat = new Material(sh) { name = "FXGlow" };
        if (_glowMat.HasProperty("_Surface")) _glowMat.SetFloat("_Surface", 1f);   // Transparent
        if (_glowMat.HasProperty("_Blend"))   _glowMat.SetFloat("_Blend", 1f);     // Additive
        if (_glowMat.HasProperty("_BaseColor"))_glowMat.SetColor("_BaseColor", Color.white);
        _glowMat.renderQueue = 3100;
        return _glowMat;
    }

    public static Material TrailMat()
    {
        if (_trailMat != null) return _trailMat;
        var sh = Shader.Find("Sprites/Default");
        _trailMat = new Material(sh) { name = "FXTrail" };
        return _trailMat;
    }

    static ParticleSystem BuildBurst(Vector3 pos, Color color, int count, float lifetime,
                                     float sizeMin, float sizeMax, float speed, float scatter)
    {
        var go = new GameObject("FX_Burst");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GlowMat();
        renderer.sortingOrder = 50;

        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(color * 2.5f); // HDR для bloom
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count + 4;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.arc = scatter;

        var sot = ps.sizeOverLifetime;
        sot.enabled = true;
        var size = new AnimationCurve();
        size.AddKey(0f, 1f);
        size.AddKey(1f, 0f);
        sot.size = new ParticleSystem.MinMaxCurve(1f, size);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.6f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.radial = new ParticleSystem.MinMaxCurve(0f, -speed * 0.5f);

        return ps;
    }

    static ParticleSystem BuildRing(Vector3 pos, Color color, float targetRadius)
    {
        var go = new GameObject("FX_Ring");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GlowMat();
        renderer.sortingOrder = 50;

        var main = ps.main;
        main.startLifetime = 0.6f;
        main.startSpeed = 0f;
        main.startSize = 0.45f;
        main.startColor = color * 3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)32) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = targetRadius;
        shape.radiusThickness = 0f;
        shape.arc = 360f;

        var sot = ps.sizeOverLifetime;
        sot.enabled = true;
        var size = new AnimationCurve();
        size.AddKey(0f, 1f);
        size.AddKey(1f, 0f);
        sot.size = new ParticleSystem.MinMaxCurve(1f, size);

        return ps;
    }

    static void AddLightFlash(GameObject host, Color color, float intensity, float radius, float duration)
    {
        var lightGo = new GameObject("FlashLight");
        lightGo.transform.SetParent(host.transform, false);
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.05f;
        lightGo.AddComponent<LightFade>().Setup(intensity, duration);
    }

    /// <summary>Прицепить point-light как ребёнок к сущности. Возвращает Light2D.</summary>
    public static Light2D AttachLight(Transform target, Color color, float intensity, float outerRadius, float innerRadius = 0f)
    {
        var go = new GameObject("Light2D");
        go.transform.SetParent(target, false);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = outerRadius;
        light.pointLightInnerRadius = innerRadius;
        light.falloffIntensity = 0.6f;
        return light;
    }
}

/// <summary>Постоянно живущий "runner" для корутин и hit-stop / popup.</summary>
public class FXRunner : MonoBehaviour
{
    Coroutine _hitStop;

    public void DoHitStop(float seconds)
    {
        if (_hitStop != null) StopCoroutine(_hitStop);
        _hitStop = StartCoroutine(HitStopCR(seconds));
    }

    IEnumerator HitStopCR(float seconds)
    {
        // Если игра уже на паузе (timeScale=0), не трогаем
        if (Time.timeScale <= 0.01f) yield break;
        float prev = Time.timeScale;
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(seconds);
        if (Time.timeScale < 0.01f) Time.timeScale = prev;
    }

    public void SpawnDamagePopup(Vector3 worldPos, float amount, Color color)
    {
        var go = new GameObject("DmgPopup");
        go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0.4f, 0f);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = Mathf.RoundToInt(amount).ToString();
        tmp.fontSize = 5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.18f;
        tmp.sortingOrder = 60;
        go.AddComponent<DamagePopupAnim>();
    }
}

public class DamagePopupAnim : MonoBehaviour
{
    public float lifetime = 0.7f;
    public float floatSpeed = 1.6f;
    float _t;
    Vector3 _start;
    TextMeshPro _tmp;
    Color _baseColor;

    void Start()
    {
        _start = transform.position;
        _tmp = GetComponent<TextMeshPro>();
        if (_tmp != null) _baseColor = _tmp.color;
        transform.localScale = Vector3.one * 0.1f;
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(_t / lifetime);
        transform.position = _start + Vector3.up * (floatSpeed * p);
        // pop-scale: 0 → 1.2 → 1.0
        float s = p < 0.15f ? Mathf.Lerp(0.1f, 1.2f, p / 0.15f)
                            : Mathf.Lerp(1.2f, 0.9f, (p - 0.15f) / 0.85f);
        transform.localScale = Vector3.one * s;
        if (_tmp != null)
        {
            var c = _baseColor; c.a = 1f - p * p;
            _tmp.color = c;
        }
        if (p >= 1f) Destroy(gameObject);
    }
}

public class LightFade : MonoBehaviour
{
    Light2D _light;
    float _start;
    float _t;
    float _duration;

    public void Setup(float startIntensity, float duration)
    {
        _light = GetComponent<Light2D>();
        _start = startIntensity;
        _duration = Mathf.Max(0.001f, duration);
    }

    void Update()
    {
        if (_light == null) return;
        _t += Time.deltaTime;
        float p = Mathf.Clamp01(_t / _duration);
        _light.intensity = Mathf.Lerp(_start, 0f, p);
        if (p >= 1f) Destroy(gameObject);
    }
}

public class DestroyAfter : MonoBehaviour
{
    public float lifetime = 1f;
    float _t;
    void Update()
    {
        _t += Time.deltaTime;
        if (_t >= lifetime) Destroy(gameObject);
    }
}
