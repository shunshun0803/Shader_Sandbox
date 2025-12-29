using UnityEngine;

public class LockOnTargetPoint : MonoBehaviour
{
    // ここに「狙ってほしい場所（空のオブジェクト）」をセットする
    public Transform aimTransform;

    // エディタ上で場所をわかりやすくするためのギズモ表示
    private void OnDrawGizmosSelected()
    {
        if (aimTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aimTransform.position, 0.2f);
        }
    }
}