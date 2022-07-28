using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerInput playerInput;
    private Animator animator;
    
    private Camera followCam;
    
    public float speed = 6f;
    public float jumpVelocity = 20f;
    [Range(0.01f, 1f)] public float airControlPercent;

    public float speedSmoothTime = 0.1f;
    public float turnSmoothTime = 0.1f;
    
    private float speedSmoothVelocity;
    private float turnSmoothVelocity;
    
    private float currentVelocityY;
    
    public float currentSpeed =>
        new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
    
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        followCam = Camera.main;
    }

    private void FixedUpdate()
    {
        if (currentSpeed > 0.2f || playerInput.fire) Rotate();

        Move(playerInput.moveInput);
        
        if (playerInput.jump) Jump();
    }

    private void Update()
    {
        UpdateAnimation(playerInput.moveInput);
    }

    public void Move(Vector2 moveInput)
    {
        //이동할 타겟 속도를 구함
        var targetSpeed = speed * moveInput.magnitude;
        //방향 벡터로 사용할 값을 구함
        var moveDirection = Vector3.Normalize(transform.forward * moveInput.y + transform.right * moveInput.x);

        //현재속도에서 타겟속도까지 부드럽게 이동하는 변수(공중에 떠있는 상태라면 더 느리게)
        var smoothTime = characterController.isGrounded ? speedSmoothTime : speedSmoothTime / airControlPercent;
        targetSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        //프레임이 늘어날때 마다 점점 증가하는 중력값 구하기
        currentVelocityY += Time.deltaTime * Physics.gravity.y;

        //구해진 모든 값을 더하여 움직임
        var velocity = moveDirection * targetSpeed + Vector3.up * currentVelocityY;
        characterController.Move(velocity * Time.deltaTime);

        //플레이어가 땅에 닿은 상태라면 중력값을 0으로 만들어줌
        if (characterController.isGrounded) currentVelocityY = 0f;
    }

    public void Rotate()
    {
        //플레이어의 y축에만 회전값을 적용하기 위해 vector3.up을 곱함
        var targetRotation = followCam.transform.eulerAngles.y;

        //플레이어 y축의 부드러운 회전을 위한 연산
        targetRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);
        transform.eulerAngles = Vector3.up * targetRotation;
    }

    public void Jump()
    {
        //플레이어가 땅에 닿은 상태가 아니라면 점프를 하지 못하게함
        if (!characterController.isGrounded) return;
        //플레이어의 현재 y축의 속도에 순간적인 점프 속도를 적용시킴
        currentVelocityY = jumpVelocity;
    }

    private void UpdateAnimation(Vector2 moveInput)
    {
        //현재스피드 / 최대 스피드 값을 구해 현재 스피드가 최대스피드에 비례하여 몇 %의 스피드인지 구하여 구한 값을 곱한다.
        var animationSpeedPrecent = currentSpeed / speed;

        //현재 속도에 비례하여 다른 애니메이션이 나오기 위함
        animator.SetFloat("Vertical Move", moveInput.y * animationSpeedPrecent, 0.05f, Time.deltaTime);
        animator.SetFloat("Horizontal Move", moveInput.x * animationSpeedPrecent, 0.05f, Time.deltaTime);
    }
}