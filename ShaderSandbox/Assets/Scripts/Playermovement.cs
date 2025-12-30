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

    private InputSystem_Actions _input;
    private Vector2 _moveInput;
    private bool _isRunning;
    private bool _isDodging = false;
    private bool _isAttacking = false;
    private bool _isGuarding = false;
    private bool _isParrying = false;
    public bool IsGuarding => _isGuarding;
    public bool IsParrying => _isParrying;
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
        if (_isDodging || _isAttacking || _isParrying) 
        {
            ApplyGravity();
            return;
        }

        HandleMovement();
        ApplyGravity();
    }
    private void SetGuard(bool state)
    {
        // 攻撃中や回避中はガードできない（キャンセルさせたい場合は条件を調整）
        if (_isDodging || _isAttacking || _isParrying) return;

        _isGuarding = state;
        if (animator != null) animator.SetBool("Guard", _isGuarding);
    }

    private void HandleMovement()
    {
        Vector3 moveDir = Vector3.zero;

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            moveDir = cameraForward * _moveInput.y + cameraRight * _moveInput.x;
        }

        if (moveDir != Vector3.zero)
        {
            // ▼ 変更: ガード中は速度を変える
            float targetSpeed;
            if (_isGuarding) targetSpeed = guardMoveSpeed; // ガード歩き
            else targetSpeed = _isRunning ? runSpeed : walkSpeed;

            characterController.Move(moveDir * targetSpeed * Time.deltaTime);

            // ガード中は常に向きを変えるか、固定するかはお好みで
            // ここでは「移動方向に向く」ままにします（ロックオン機能があるならロックオン対象に向くのがベスト）
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            if (animator != null) animator.SetFloat("Speed", targetSpeed);
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0);
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
    private IEnumerator ParryRoutine()
    {
        _isParrying = true;

        if (animator != null) animator.SetTrigger("Parry");

        yield return new WaitForSeconds(parryWindow);
        _isParrying = false;
        yield return new WaitForSeconds(parryCooldown - parryWindow);
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        if (animator != null) 
        {
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Attack");
        }
        yield return new WaitForSeconds(attackDuration);
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

    private IEnumerator DodgeRoutine()
    {
        _isDodging = true;
        if (animator != null) animator.SetTrigger("Dodge");

        // 1. 回避方向の計算
        Vector3 dodgeDir = Vector3.zero;

        // 入力のスナップショットをとる（連打対策）
        Vector2 currentInput = _moveInput;

        if (currentInput.sqrMagnitude > 0.01f)
        {
            // 入力がある場合：カメラ基準で方向を決める
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            dodgeDir = (cameraForward * currentInput.y + cameraRight * currentInput.x).normalized;
        }
        else
        {
                // バックステップ無効なら「今向いている方向（前）」へ転がる
                // これなら連打しても前後せず、常に前に進む
                dodgeDir = transform.forward;
        }

        // 2. 向きを変える
        if (dodgeDir != Vector3.zero)
        {
            transform.forward = dodgeDir;
        }

        // 3. 移動実行
        float timer = 0;
        while (timer < dodgeDuration)
        {
            characterController.Move(dodgeDir * dodgeSpeed * Time.deltaTime);
            characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        _isDodging = false;
        if (animator != null) animator.SetFloat("Speed", 0f);
    }
}