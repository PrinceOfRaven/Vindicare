using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon Data", order = 0)]
public class WeaponData : ScriptableObject
{
    [Header("Отображение в меню")]
    public string displayName = "Weapon";
    public Sprite icon;

    [Header("Префаб оружия")]
    public GameObject weaponPrefab;
}
