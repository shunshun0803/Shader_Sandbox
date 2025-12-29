using UnityEngine;

public class PlayerCombatEvents : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Collider weaponCollider; // 剣のコライダー
    [SerializeField] private TrailRenderer weaponTrail;

    // アニメーションの「振り始め」で呼ぶ
    public void HitboxOn()
    {
        if (weaponCollider != null)
        {
            weaponCollider.enabled = true;
        }
        if (weaponTrail != null)
        {
            weaponTrail.emitting = true;
        }
    }

    // アニメーションの「振り終わり」で呼ぶ
    public void HitboxOff()
    {
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }
        if (weaponTrail != null)
        {
            weaponTrail.emitting = false;
        }
    }
}