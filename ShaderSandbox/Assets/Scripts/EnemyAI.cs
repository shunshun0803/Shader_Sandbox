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
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        // 1. 索敵範囲内にプレイヤーがいれば目的地を更新
        if (distance <= _agent.stoppingDistance && Time.time >= _nextAttackTime)
        {
            StartCoroutine(PerformAttack());
        }
        else if (distance <= detectionRange)
        {
            _agent.isStopped = false; // 動く
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

        // プレイヤーの方向を向かせる
        LookAtPlayer();

        // 攻撃アニメーション再生
        _animator.SetTrigger("Attack");

        // クールダウン設定
        _nextAttackTime = Time.time + attackCooldown;

        // アニメーションが終わるまで待機（秒数はアニメに合わせるか、イベントで制御）
        yield return new WaitForSeconds(5f);

        _isAttacking = false;
    }
}