using System;

// 아이템 관련 이벤트
public static class ItemEvents
{
    // TODO: Item, Weapon 클래스 생성 후 타입 변경
    public static event Action<object> OnItemPickedUp;
    public static event Action<object> OnWeaponChanged;

    public static void ItemPickedUp(object item) => OnItemPickedUp?.Invoke(item);
    public static void WeaponChanged(object weapon) => OnWeaponChanged?.Invoke(weapon);
}
