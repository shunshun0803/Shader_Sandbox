using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // 複数のレンダラーと、それぞれの元の色を保存する配列
    private Renderer[] _renderers;
    private Color[] _originalColors;

    private void Start()
    {
        // 自分自身と子供に含まれる「全ての」Rendererを取得する（複数形 s に注意）
        _renderers = GetComponentsInChildren<Renderer>();

        // 元の色を保存する配列を準備
        _originalColors = new Color[_renderers.Length];

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
        if (_renderers == null || _renderers.Length == 0) return;

        Debug.Log($"<color=red>痛っ！ {damage} のダメージを受けた！</color>");

        // 全てのパーツを赤くする
        foreach (var rend in _renderers)
        {
            rend.material.color = Color.white;
        }

        // 時間差で戻す予約
        Invoke("ResetColor", 0.2f);
    }

    void ResetColor()
    {
        if (_renderers == null) return;

        // 全てのパーツを元の色に戻す
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].material.color = _originalColors[i];
            }
        }
    }
}