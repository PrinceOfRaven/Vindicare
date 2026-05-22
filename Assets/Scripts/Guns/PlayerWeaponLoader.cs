using UnityEngine;

[DisallowMultipleComponent]
public class PlayerWeaponLoader : MonoBehaviour
{
    [Header("Куда прицепить оружие")]
    [Tooltip("Точка-хэндл, к которой будет привязан префаб оружия. Если пусто — привяжет к этому же transform.")]
    [SerializeField] private Transform _weaponMount;

    [Header("Фолбэк")]
    [Tooltip("Оружие по умолчанию, если игрок попал в сцену в обход меню (например, при тесте напрямую из SampleScene).")]
    [SerializeField] private WeaponData _fallbackWeapon;

    private GameObject _spawnedWeapon;

    public void Configure(Transform mount, WeaponData fallback)
    {
        _weaponMount = mount;
        _fallbackWeapon = fallback;
    }

    private void Start()
    {
        WeaponData data = SelectedWeapon.Current != null ? SelectedWeapon.Current : _fallbackWeapon;
        if (data == null)
        {
            return;
        }
        if (data.weaponPrefab == null)
        {
            return;
        }

        Transform mount = _weaponMount != null ? _weaponMount : transform;

        var existing = mount.GetComponentsInChildren<GunsBase>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            Destroy(existing[i].gameObject);
        }

        _spawnedWeapon = Instantiate(data.weaponPrefab, mount.position, mount.rotation, mount);
        _spawnedWeapon.name = data.weaponPrefab.name;
    }
}
