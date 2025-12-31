using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float runSpeed = 8.0f;
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeSpeed = 12.0f;
    [SerializeField] private float dodgeDuration = 0.5f;

    [Header("Guard & Parry Settings")]
    [SerializeField] private float guardMoveSpeed = 2.0f;
    [SerializeField] private float parryWindow = 0.2f;    // パリィ成功判定が出る時間（受付時間）
    [SerializeField] private float parryCooldown = 0.5f;  // パリィ後の硬直（失敗時の隙）

    [Header("Combat Settings")]
    [SerializeField] private float attackDuration = 0.6f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform; // ここには Main Camera を入れるのが鉄則
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;

    [SerializeField] private PlayerLockOn playerLockOn;

    [Header("Skill Settings")]
    [SerializeField] private float skillCooldown = 3.0f; // 3秒に1回使える
    [SerializeField] private float skillDuration = 2.0f; // スキルモーションの長さ（硬直時間）

    [Header("Attack Homing Settings")]
    [SerializeField] private float homingRange = 2.0f;
    [SerializeField] private float attackRotationSpeed = 20f; // 攻撃時の回転速度（速め推奨）
    [SerializeField] private float attackStepDistance = 2.0f; // 攻撃時に踏み込む距離
    [SerializeField] private float attackStepDuration = 0.2f; // 踏み込みにかける時間（出始めの一瞬）

    private float _lastSkillTime = -999f; // 最後にスキルを使った時間
    private bool _isUsingSkill = false;   // スキル中フラグ

    private InputSystem_Actions _input;
    private Vector2 _moveInput;
    private bool _isRunning;
    private bool _isDodging = false;
    private bool _isAttacking = false;
    private bool _isGuarding = false;
    private bool _isParrying = false;
    public bool IsGuarding => _isGuarding;
    public bool IsParrying => _isParrying;
    private bool _isParryActive = false; // パリィの成功判定が出ているか
    public bool IsParryActive => _isParryActive; // 敵の攻撃スクリプトから参照する用
    private Vector3 _velocity;

    private void Awake()
    {
        _input = new InputSystem_Actions();

        _input.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _input.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

        _input.Player.Sprint.performed += ctx => _isRunning = true;
        _input.Player.Sprint.canceled += ctx => _isRunning = false;

        _input.Player.Dodge.performed += OnDodge;
        _input.Player.Attack.performed += OnAttack;

        _input.Player.Guard.performed += ctx => SetGuard(true);
        _input.Player.Guard.canceled += ctx => SetGuard(false);

        if (_input.Player.Skill != null) // 安全対策
        {
            _input.Player.Skill.performed += ctx => OnSkill();
        }
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Start()
    {
        // ▼ カーソルを非表示にし、画面中央にロック（固定）する
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    private void Update()
    {
        if (_isDodging || _isAttacking || _isParrying || _isUsingSkill)
        {
            ApplyGravity();
            return;
        }

        HandleMovement();
        ApplyGravity();
    }
    private void SetGuard(bool state)
    {// ガードを開始（trueにする）するときだけ制限をかける
        if (state && (_isDodging || _isAttacking || _isParrying || _isUsingSkill)) return;

        _isGuarding = state;
        if (animator != null) animator.SetBool("Guard", _isGuarding);
    }

    private void HandleMovement()
    {
        // 1. 入力から移動ベクトルを計算（カメラ基準）
        Vector3 moveDir = Vector3.zero;
        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0; cameraRight.y = 0;
            cameraForward.Normalize(); cameraRight.Normalize();
            moveDir = cameraForward * _moveInput.y + cameraRight * _moveInput.x;
        }

        // 2. 移動速度の決定（ガード中やダッシュ中の補正）
        float targetSpeed = _isGuarding ? guardMoveSpeed : (_isRunning ? runSpeed : walkSpeed);

        // 3. 移動実行
        if (moveDir != Vector3.zero)
        {
            characterController.Move(moveDir * targetSpeed * Time.deltaTime);
        }

        // ★★★ 4. 回転とアニメーションの分岐（ここを変更！） ★★★

        // 「ロックオン中」かどうかをチェック
        bool isLockedOn = (playerLockOn != null && playerLockOn.CurrentTarget != null);

        // 条件変更: 「ロックオン中」または「ガード中」なら、ストレイフ挙動にする
        if (isLockedOn || _isGuarding)
        {
            // 【A. 体の向き】
            Vector3 targetDir;
            if (isLockedOn)
            {
                // ロックオン中 → 敵の方を向く
                targetDir = playerLockOn.CurrentTarget.position - transform.position;
            }
            else
            {
                // ガード中（敵なし） → カメラの正面を向く
                targetDir = cameraTransform.forward;
            }

            targetDir.y = 0; // 上下は無視
            if (targetDir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
            }

            // 【B. アニメーション】
            // XとYをそのまま渡して「方向別移動」させる
            if (animator != null)
            {
                animator.SetFloat("InputX", _moveInput.x, 0.1f, Time.deltaTime);
                animator.SetFloat("InputY", _moveInput.y, 0.1f, Time.deltaTime);

                animator.SetFloat("Speed", _moveInput.magnitude);
            }
        }
        else // 【通常時：自由移動】
        {
            // ... (以前のまま：移動方向に回転＆InputYのみ使用) ...
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (animator != null)
            {
                animator.SetFloat("InputX", 0f, 0.1f, Time.deltaTime);

                float forwardAmount = _moveInput.magnitude;
                if (_isRunning) forwardAmount *= 2f;
                animator.SetFloat("InputY", forwardAmount, 0.1f, Time.deltaTime);
            }
        }
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && _velocity.y < 0) _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;
        characterController.Move(_velocity * Time.deltaTime);
    }

    // --- 攻撃処理 ---
    private void OnAttack(InputAction.CallbackContext ctx)
    {
        // 他のアクション中は入力を受け付けない
        if (_isDodging || _isAttacking || _isParrying) return;

        if (_isGuarding)
        {
            // ガード中なら「弾き（パリィ）」発動
            StartCoroutine(ParryRoutine());
        }
        else
        {
            // 通常時は「攻撃」発動
            StartCoroutine(AttackRoutine());
        }
    }
    private void OnSkill()
    {
        // 1. 他のアクション中は禁止
        if (_isDodging || _isAttacking || _isParrying || _isUsingSkill) return;
        if (_isGuarding) SetGuard(false);
        // 2. クールダウンチェック
        // 「現在の時間」が「最後に撃った時間 + 待ち時間」より小さければ、まだ使えない
        if (Time.time < _lastSkillTime + skillCooldown)
        {
            Debug.Log("スキル準備中... 残り: " + ((_lastSkillTime + skillCooldown) - Time.time) + "秒");
            return;
        }

        // 3. 発動！
        StartCoroutine(SkillRoutine());
    }
    private IEnumerator SkillRoutine()
    {
        _isUsingSkill = true;
        _lastSkillTime = Time.time;

        Transform target = playerLockOn != null ? playerLockOn.CurrentTarget : null;

        if (animator != null)
        {
            animator.SetFloat("InputX", 0);
            animator.SetFloat("InputY", 0);
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Skill");
        }

        // ★ スキル用ホーミング（攻撃と同じロジック） ★
        float timer = 0;
        // スキルの踏み込み時間は skillDuration に合わせて調整してください
        float skillStepTime = 0.3f;

        while (timer < skillStepTime)
        {
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.position);

                if (distance <= homingRange)
                {
                    // 回転（必殺技なので少し速めに設定）
                    Vector3 targetDir = (target.position - transform.position);
                    targetDir.y = 0;
                    if (targetDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), (attackRotationSpeed * 1.5f) * Time.deltaTime);
                    }

                    // 踏み込み
                    if (distance > 1.2f)
                    {
                        characterController.Move(transform.forward * (attackStepDistance / skillStepTime) * Time.deltaTime);
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0, skillDuration - skillStepTime));
        _isUsingSkill = false;
    }
    private IEnumerator ParryRoutine()
    {
        _isParrying = true;      // 【移動制限開始】
        _isParryActive = true;   // 【パリィ成功判定開始】

        if (animator != null)
        {
            animator.SetFloat("InputX", 0);
            animator.SetFloat("InputY", 0);
            animator.SetTrigger("Parry");
        }

        // 1. パリィが成功する「受付時間」だけ待つ
        yield return new WaitForSeconds(parryWindow);

        // 2. 受付時間は終了（これ以降に攻撃を食らったらダメージを受ける）
        _isParryActive = false;

        // 3. 残りの「硬直（隙）」が終わるまで待つ
        // 全体のクールダウンから受付時間を引いた残り時間
        float recoveryTime = parryCooldown - parryWindow;
        if (recoveryTime > 0)
        {
            yield return new WaitForSeconds(recoveryTime);
        }

        _isParrying = false;     // 【移動制限解除】
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        Transform target = playerLockOn != null ? playerLockOn.CurrentTarget : null;

        if (animator != null)
        {
            animator.SetFloat("InputX", 0);
            animator.SetFloat("InputY", 0);
            animator.SetTrigger("Attack");
        }

        // ★ 至近距離ホーミング開始 ★
        float timer = 0;
        while (timer < attackStepDuration)
        {
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.position);

                // 至近距離（homingRange以内）の時だけ吸い付く
                if (distance <= homingRange)
                {
                    // 1. 向きを合わせる
                    Vector3 targetDir = (target.position - transform.position);
                    targetDir.y = 0;
                    if (targetDir != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(targetDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, attackRotationSpeed * Time.deltaTime);
                    }

                    // 2. 踏み込む（密着しすぎないよう1.2m以上の時だけ移動）
                    if (distance > 1.2f)
                    {
                        Vector3 moveStep = transform.forward * (attackStepDistance / attackStepDuration) * Time.deltaTime;
                        characterController.Move(moveStep);
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0, attackDuration - attackStepDuration));
        _isAttacking = false;
    }

    // --- 回避処理 (修正版) ---
    private void OnDodge(InputAction.CallbackContext ctx)
    {
        // ガード中でも回避はできるようにする（キャンセル行動）
        // ただしガード状態は一時解除したほうが自然
        if (_isDodging || _isAttacking) return;

        if (_isGuarding) SetGuard(false); // 回避したらガード解除

        StartCoroutine(DodgeRoutine());
    }

    // 修正版 DodgeRoutine
    private IEnumerator DodgeRoutine()
    {
        _isDodging = true;

        // 「ロックオン中」または「ガード中」かを判定
        bool isStrafeMode = (playerLockOn != null && playerLockOn.CurrentTarget != null) || _isGuarding;

        // 1. 入力のスナップショット
        Vector2 snapInput = _moveInput;
        if (snapInput.sqrMagnitude < 0.01f) snapInput = new Vector2(0, 1);
        snapInput.Normalize();

        // 2. アニメーションへの適用
        if (animator != null)
        {
            animator.SetFloat("InputX", snapInput.x);
            animator.SetFloat("InputY", snapInput.y);
            animator.SetTrigger("Dodge");
        }

        // 3. 回避方向の計算
        Vector3 dodgeDir = Vector3.zero;
        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0; cameraRight.y = 0;
            cameraForward.Normalize(); cameraRight.Normalize();
            dodgeDir = (cameraForward * snapInput.y + cameraRight * snapInput.x).normalized;
        }
        else
        {
            dodgeDir = transform.forward;
        }

        // ★★★ 4. 向きの制御（ここを修正！） ★★★
        if (isStrafeMode)
        {
            // ストレイフモード中（ロックオン・ガード中）は、
            // 「回避方向に回転させない」ことで、敵を向いたままの「ステップ」になる
        }
        else
        {
            // 通常時は、回避する方向（dodgeDir）を向く
            if (dodgeDir != Vector3.zero)
            {
                transform.forward = dodgeDir;
            }
        }

        // 5. 移動実行
        float timer = 0;
        while (timer < dodgeDuration)
        {
            characterController.Move(dodgeDir * dodgeSpeed * Time.deltaTime);
            characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        _isDodging = false;
    }
}
