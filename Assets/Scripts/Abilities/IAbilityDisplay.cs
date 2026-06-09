using UnityEngine;

/// <summary>
/// То, что HUD умеет показать как слот способности (иконка, клавиша, перезарядка).
/// Реализуется и бомбой, и активными способностями.
/// </summary>
public interface IAbilityDisplay
{
    string DisplayName { get; }
    string KeyLabel { get; }
    Color ThemeColor { get; }
    Sprite Icon { get; }

    /// <summary>1 — только что использована, 0 — готова.</summary>
    float CooldownRemaining01 { get; }
    bool IsReady { get; }
}
