public static class SelectedWeapon
{
    public static WeaponData Current { get; private set; }

    public static void Set(WeaponData data)
    {
        Current = data;
    }
}
