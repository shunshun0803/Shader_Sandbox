using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float knockbackForce = 5.0f;

    [SerializeField] private GameObject _owner;

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーに当たったかチェック
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player == null) return;

            // 1. パリィ判定
            if (player.IsParryActive)
            {
                OnParried();
                return; // ダメージ処理をスキップ
            }

            // 2. ガード判定
            if (player.IsGuarding)
            {
                OnGuarded(player);
                return; // ダメージを軽減またはスキップ
            }

            // 3. 直撃（ダメージ）
            OnHit(player);
        }
    }

    private void OnParried()
    {
        Debug.Log("★ パリィ成功！敵がのけぞる ★");

        // 敵側のスクリプトに「のけぞり」を通知
        EnemyAI enemyAI = _owner.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            // パリィされた時の専用ステート（Flinch）へ移行
            // enemyAI.ChangeState(EnemyState.Flinch); // 前回のEnemyAIに実装が必要
        }

        // ヒットストップや火花エフェクトをここで再生
        // TimeManager.Instance.HitStop(0.1f); 
    }

    private void OnGuarded(PlayerMovement player)
    {
        Debug.Log("🛡️ ガード成功！");
        // ガード時の火花エフェクトや、プレイヤーを少し後ろにノックバックさせる処理
    }

    private void OnHit(PlayerMovement player)
    {
        Debug.Log("💥 プレイヤーにヒット！");
        // ダメージ適用（HPシステムを実装したら呼び出す）
        // player.TakeDamage(damage);

        // プレイヤーにダメージアニメーションを再生させる
        // player.GetComponent<Animator>().SetTrigger("GetHit");
    }
}