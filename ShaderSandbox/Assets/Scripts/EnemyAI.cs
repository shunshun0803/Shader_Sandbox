using UnityEngine;
using UnityEngine.AI;
using System.Collections;


public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;

    [Header("AI Settings")]
    public float detectionRange = 20f; // プレイヤーに気づく距離

    [Header("Attack Settings")]
    public float attackCooldown = 2.0f; // 次の攻撃までの待ち時間
    private float _nextAttackTime = 0f;
    private bool _isAttacking = false;
    private bool _isDead = false;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        // プレイヤーを探す（Tagが"Player"であることを確認してください）
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    void Update()
    {
        if (_isDead) return;
        if (_player == null) return;

        if (_isAttacking)
        {
            _agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, _player.position);

        if (_isAttacking)
        {
            // 攻撃アニメーションの「振り下ろす前（予備動作中）」だけ
            // 緩やかにプレイヤーの方を向き続ける処理
            SmoothLookAtPlayer(10f); // 10f は回転速度。お好みで調整
            return;
        }

        if (distance <= _agent.stoppingDistance && Time.time >= _nextAttackTime)
        {
            StartCoroutine(PerformAttack());
        }

        else if (distance <= detectionRange)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }
        else
        {
            _agent.isStopped = true;
        }

        float currentSpeed = _agent.velocity.magnitude;
        _animator.SetFloat("Speed", currentSpeed);

        if (distance <= _agent.stoppingDistance + 1f)
        {
            LookAtPlayer();
        }
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0; // 上下は無視
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    private IEnumerator PerformAttack()
    {
        _isAttacking = true;
        _agent.isStopped = true; // 攻撃中は足を止める
        _agent.velocity = Vector3.zero;

        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 攻撃アニメーション再生
        _animator.SetTrigger("Attack");

        // クールダウン設定
        _nextAttackTime = Time.time + attackCooldown;

        // アニメーションが終わるまで待機（秒数はアニメに合わせるか、イベントで制御）
        yield return new WaitForSeconds(5f);

        _isAttacking = false;
    }
    private void SmoothLookAtPlayer(float speed)
    {
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Slerp を使って滑らかに補間
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
        }
    }
    public void OnTakeDamage(int damage) // 引数を受け取るように変更
    {
        _isAttacking = false;
        StopAllCoroutines();

        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        _animator.SetTrigger("GetHit");

        // ダメージ量に応じてノックバックの強さを変える
        // 例：ダメージの 1/10 を強さにする（調整してください）
        float knockbackStrength = damage * 0.2f;

        // コルーチンに強さを渡す
        StartCoroutine(KnockbackRoutine(knockbackStrength));
    }

    private IEnumerator KnockbackRoutine(float strength) // 引数を受け取る
    {
        float timer = 0.2f;

        while (timer > 0)
        {
            // 引数で受け取った strength を使って移動
            transform.position -= transform.forward * Time.deltaTime * strength;

            timer -= Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = false;
    }

    public void OnDeath()
    {
        if (_isDead) return;
        _isDead = true;

        _isAttacking = false;
        StopAllCoroutines(); // 全ての動作（ノックバック等）を中断

        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }
        _agent.enabled = false;

        // 2. 死亡アニメーション
        _animator.SetTrigger("IsDead"); // もし死亡用アニメがあれば。なければGetHitのままでもOK

        // 3. 派手な吹き飛びを開始
        //StartCoroutine(BlowAwayRoutine());
        Destroy(gameObject, 3.0f);
    }

}