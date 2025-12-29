using UnityEngine;
using UnityEngine.InputSystem; // 必須

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField] private float rotationSmoothTime = 0.12f; // 方向転換の滑らかさ
    [SerializeField] private float gravity = -15.0f;

    // 参照
    private CharacterController _controller;
    private InputSystem_Actions _input; // 自動生成されたクラス
    private Transform _cameraTransform;

    // 内部変数
    private Vector2 _moveInput;
    private Vector3 _velocity;
    private float _targetRotation;
    private float _rotationVelocity;
    private Animator _animator;
    private int _animIDAttack;
    private void Start()
    {
        // マウスカーソルを中央に固定して非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = new InputSystem_Actions();
        
        // カメラのTransformを取得（メインカメラがTag付けされていること）
        if (Camera.main != null) _cameraTransform = Camera.main.transform;
        _animator = GetComponent<Animator>();

        _animIDAttack = Animator.StringToHash("Attack");
    }

    private void OnEnable()
    {
        // Input Systemの有効化
        _input.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _input.Player.Move.canceled += ctx => _moveInput = Vector2.zero;
        _input.Player.Attack.performed += ctx => OnAttack();
        
        _input.Enable();
    }

    private void OnDisable()
    {
        _input.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleGravity();
        UpdateAnimation();

    }

    private void HandleMovement()
    {
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag("Attack")) 
        {
            // 攻撃中なら移動量を0にして、処理を終了
            _moveInput = Vector2.zero;
            return;
        }

        // 入力がないなら処理しない
        if (_moveInput == Vector2.zero) return;

        // 1. カメラの向きを考慮した移動方向の計算
        // Atan2を使って、入力(x,y)を角度に変換し、カメラのY軸回転を足す
        float targetAngle = Mathf.Atan2(_moveInput.x, _moveInput.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
        
        // 2. キャラクターの向きをスムーズに回転させる（Damp）
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        // 3. 計算した方向へ移動
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        _controller.Move(moveDir.normalized * (moveSpeed * Time.deltaTime));
    }

    private void HandleGravity()
    {
        // Debug.Log("HandleGravity called");
        // 簡易的な重力処理
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // 地面に押し付ける微弱な力
        }

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
    // ▼ このメソッドを上書きしてください
    private void UpdateAnimation()
    {
        if (_animator == null) return;

        // キー入力の大きさを取得 (0 or 1)
        float inputMagnitude = _moveInput.magnitude;
        
        // ▼ 追加: キーボード用の歩き・走り制御
        // Shiftキーが押されていれば 1.0 (走り)、押されていなければ 0.5 (歩き) にする
        // (パッドの場合はスティックの倒し具合も考慮する)
        bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        
        if (inputMagnitude > 0)
        {
            // 入力がある時：Shiftありなら1、なしなら0.5にする
            inputMagnitude = isSprinting ? 0.5f : 1.0f;
        }
        // ▲ ここまで
        
        // アニメーターに通知
        _animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);
    }
    private void OnAttack()
    {
        // アニメーターに「Attack」トリガーを送る
        _animator.SetTrigger(_animIDAttack);
        
        // デバッグ用ログ（動かない時の確認用）
        Debug.Log("Slash!"); 
    }
}