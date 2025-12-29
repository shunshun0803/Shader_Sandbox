using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    // どこからでも呼べるようにする（シングルトン）
    public static HitStop Instance;

    private void Awake()
    {
        Instance = this;
    }

    // 時間を止めるメソッド
    public void Stop(float duration)
    {
        // すでに止まっていたら上書きしない（ガタつき防止）
        if (Time.timeScale == 0) return;

        // 時間を止める
        Time.timeScale = 0.0f;
        
        // 指定時間待ってから戻すコルーチンを開始
        StartCoroutine(WaitAndRestore(duration));
    }

    IEnumerator WaitAndRestore(float duration)
    {
        // TimeScaleが0だと普通のWaitForSecondsは動かないので、Realtimeを使う
        yield return new WaitForSecondsRealtime(duration);

        // 時間を元に戻す
        Time.timeScale = 1.0f;
    }
}