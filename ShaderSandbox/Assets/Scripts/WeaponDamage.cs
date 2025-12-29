using UnityEngine;
using Unity.Cinemachine;

public class WeaponDamage : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private GameObject hitVfxPrefab; // ▼ 追加: エフェクトのプレハブ

    private CinemachineImpulseSource _impulseSource;

    private void Start()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                // 1. ダメージ
                enemy.TakeDamage(damageAmount);

                // 2. ヒットストップ
                if (HitStop.Instance != null) HitStop.Instance.Stop(0.1f);

                // 3. カメラシェイク
                if (_impulseSource != null) _impulseSource.GenerateImpulse(1.0f);

                // 4. ▼ 追加: エフェクト発生
                if (hitVfxPrefab != null)
                {
                    // 衝突位置を計算（相手のコライダーの、自分に一番近い場所）
                    Vector3 hitPos = other.ClosestPointOnBounds(transform.position);
                    
                    // エフェクトを生成
                    GameObject vfx = Instantiate(hitVfxPrefab, hitPos, Quaternion.identity);
                    
                    // 2秒後に消す（ゴミ掃除）
                    Destroy(vfx, 2.0f);
                }
            }
        }
    }
}