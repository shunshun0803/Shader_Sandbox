using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;

public class PlayerLockOn : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float searchRadius = 20.0f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float switchThreshold = 0.5f;

    [Header("UI")]
    [SerializeField] private GameObject cursorPrefab;
    // ▼ 変更: デフォルトのオフセットは、専用ポイントがない敵の予備用になります
    [SerializeField] private Vector3 defaultOffset = new Vector3(0, 1.2f, 0);

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera lockOnCamera;

    private InputSystem_Actions _input;
    private Transform _currentTargetEnemy; // 敵のルートオブジェクト
    private Transform _currentTargetAimPoint; // 実際に狙うポイント（AimPoint または Enemy中心）
    private GameObject _currentCursorInstance;
    private bool _canSwitchTarget = true;

    // ... (Awake, OnEnable, OnDisable, Update, HandleTargetSwitching は変更なし) ...
    private void Awake()
    {
        _input = new InputSystem_Actions();
    }
    private void OnEnable()
    {
        _input.Enable();
        _input.Player.LockOn.performed += OnLockOnPerformed;
    }
    private void OnDisable()
    {
        _input.Player.LockOn.performed -= OnLockOnPerformed;
        _input.Disable();
        if (_currentCursorInstance != null) Destroy(_currentCursorInstance);
    }
    private void Update()
    {
        if (_currentTargetEnemy != null)
        {
            HandleTargetSwitching();
            UpdateCursorPosition();
        }
    }
    private void HandleTargetSwitching()
    {
        Vector2 lookInput = _input.Player.Look.ReadValue<Vector2>();
        if (Mathf.Abs(lookInput.x) > switchThreshold)
        {
            if (_canSwitchTarget)
            {
                SwitchTarget(lookInput.x);
                _canSwitchTarget = false;
            }
        }
        else
        {
            _canSwitchTarget = true;
        }
    }

    // --- 変更点ここから ---

    private void SwitchTarget(float directionX)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
        if (hits.Length <= 1) return;

        float closestDistance = Mathf.Infinity;
        Transform nextTarget = null;

        foreach (var hit in hits)
        {
            if (hit.transform == _currentTargetEnemy) continue;
            if (hit.transform == transform) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nextTarget = hit.transform;
            }
        }

        if (nextTarget != null)
        {
            LockOn(nextTarget);
        }
    }

    private void OnLockOnPerformed(InputAction.CallbackContext ctx)
    {
        if (_currentTargetEnemy != null) Unlock();
        else FindTarget();
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = hit.transform;
            }
        }

        if (bestTarget != null) LockOn(bestTarget);
    }

    private void LockOn(Transform targetEnemy)
    {
        if (_currentCursorInstance != null) Destroy(_currentCursorInstance);

        _currentTargetEnemy = targetEnemy;

        // ▼ 追加: 敵が専用の狙い点を持っているかチェック
        var targetPointComponent = targetEnemy.GetComponent<LockOnTargetPoint>();
        if (targetPointComponent != null && targetPointComponent.aimTransform != null)
        {
            // 持っていればそれを使う
            _currentTargetAimPoint = targetPointComponent.aimTransform;
        }
        else
        {
            // 持っていなければ敵のルートを使う（後でオフセットを足す）
            _currentTargetAimPoint = targetEnemy;
        }


        // カメラ設定（AimPointを見るようにする）
        if (lockOnCamera != null)
        {
            lockOnCamera.LookAt = _currentTargetAimPoint;
            lockOnCamera.Priority = 20;
        }

        if (cursorPrefab != null)
        {
            _currentCursorInstance = Instantiate(cursorPrefab);
        }
    }

    private void Unlock()
    {
        _currentTargetEnemy = null;
        _currentTargetAimPoint = null; // リセット

        if (lockOnCamera != null)
        {
            lockOnCamera.LookAt = null;
            lockOnCamera.Priority = 0;
        }

        if (_currentCursorInstance != null)
        {
            Destroy(_currentCursorInstance);
        }
    }

    private void UpdateCursorPosition()
    {
        // ターゲットかAimPointが何らかの理由で消えたらロック解除
        if (_currentTargetEnemy == null || _currentTargetAimPoint == null)
        {
            if (_currentCursorInstance != null) Destroy(_currentCursorInstance);
            Unlock();
            return;
        }

        if (_currentCursorInstance != null)
        {
            Vector3 finalPos;

            // AimPointが敵のルートと違うなら、専用の点が設定されているということ
            if (_currentTargetAimPoint != _currentTargetEnemy)
            {
                // 専用の点の位置をそのまま使う（埋まっててもシェーダーが解決）
                finalPos = _currentTargetAimPoint.position;
            }
            else
            {
                // 専用点がない場合は、ルート位置 + デフォルトオフセット
                finalPos = _currentTargetEnemy.position + defaultOffset;
            }

            // カーソル位置適用
            _currentCursorInstance.transform.position = finalPos;
            
            // カメラに向ける
            if (Camera.main != null)
            {
                _currentCursorInstance.transform.LookAt(Camera.main.transform);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}