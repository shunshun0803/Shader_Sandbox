using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP Settings")]
    [SerializeField] private int maxHP = 10;
    private int _currentHP;
    private bool _isDead = false;
    public bool IsDead => _isDead; // 外部から読み取り専用でフラグを公開

    // 複数のレンダラーと、それぞれの元の色を保存する配列
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private EnemyAI _enemyAI;

    private void Start()
    {
        // 自分自身と子供に含まれる「全ての」Rendererを取得する（複数形 s に注意）
        _renderers = GetComponentsInChildren<Renderer>();

        // 元の色を保存する配列を準備
        _originalColors = new Color[_renderers.Length];
        _enemyAI = GetComponent<EnemyAI>();

        _currentHP = maxHP; // HPを全回復状態でスタート

        // 全パーツの元の色を覚えておく
        for (int i = 0; i < _renderers.Length; i++)
        {
            // マテリアルが複数ある場合も考慮して sharedMaterial ではなく material を使うのが無難ですが
            // 処理負荷軽減のため、ここでは単純に material.color を取得します
            _originalColors[i] = _renderers[i].material.color;
        }
    }
    public void TakeDamage(int damage)
    {
        if (_isDead) return; // 死んでたら無視
        _currentHP -= damage;
        Debug.Log($"Enemy took {damage} damage. Current HP: {_currentHP}");

        // 2. 視覚演出（提供されたコード）
        FlashWhite();

        // 3. AIに「のけぞり」を依頼する
        if (_currentHP <= 0)
        {
            Die();
        }
        else
        {
            // 生きていれば通常のノックバック
            if (_enemyAI != null) _enemyAI.OnTakeDamage(damage);
        }

        // 4. 死亡判定
        // if (currentHP <= 0) Die();
    }
    private void Die()
    {
        _isDead = true;
        if (_enemyAI != null)
        {
            _enemyAI.OnDeath(); // AIに死亡時の演出を依頼
        }
    }
    private void FlashWhite() { /* 白くする処理 */ Invoke("ResetColor", 0.2f); }

    void ResetColor()
    {
        if (_renderers == null) return;

        // 全てのパーツを元の色に戻す
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                // URPでの色変更（_BaseColorプロパティを直接叩く）
                _renderers[i].material.SetColor("_BaseColor", _originalColors[i]);
            }
        }
    }
}