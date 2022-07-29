using UnityEngine;


public class PlayerShooter : MonoBehaviour
{
    public enum AimState
    {
        Idle,
        HipFire
    }

    public AimState aimState { get; private set; }

    public Gun gun;
    public LayerMask excludeTarget;
    
    private PlayerInput playerInput;
    private Animator playerAnimator;
    private Camera playerCamera;

    private float waitingTimeForReleasingAim = 2.5f;
    private float lastFireInputTime;
    
    private Vector3 aimPoint;
    private bool linedUp => !(Mathf.Abs( playerCamera.transform.eulerAngles.y - transform.eulerAngles.y) > 1f);
    private bool hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up * gun.fireTransform.position.y,gun.fireTransform.position, ~excludeTarget);
    
    void Awake()
    {
        //비트 연산자를 사용하여 excludeTarget에 현재플레이어의 레이어를 추가한다.

        //excludeTarget 그 자체와 excludeTarget + 현재 플레이어의 레이어가 다르다면
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer)))
        {
            //excludeTarget에 플레이어의 레이어가 없다는 뜻이므로 추가해준다.
            excludeTarget |= 1 << gameObject.layer;
        }
    }

    private void Start()
    {
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>(); 
    }

    private void OnEnable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (playerInput.fire)
        {
            lastFireInputTime = Time.time;
            Shoot();
        }
        else if (playerInput.reload)
        {
            Reload();
        }
    }

    private void Update()
    {
        UpdateAimTarget();

        var angle = playerCamera.transform.eulerAngles.x;

        //angle이 90과 -90 사이를 넘어가는 것을 방지 하기 위함
        if (angle > 270f) angle -= 360f;

         //angle의 값을 1~0 사이의 갑으로 바꿔줌
        angle = angle / -180f + 0.5f;
        playerAnimator.SetFloat("Angle", angle);

        if(!playerInput.fire && Time.time >= lastFireInputTime + waitingTimeForReleasingAim)
        {
            aimState = AimState.Idle;
        }

        UpdateUI();    
    }

    public void Shoot()
    {
        if(aimState == AimState.Idle)
        {
            if (linedUp) aimState = AimState.HipFire;
        }
        else if(aimState == AimState.HipFire)
        {
            if (hasEnoughDistance)
            {
                if (gun.Fire(aimPoint))
                {
                    playerAnimator.SetTrigger("Shoot");
                }
            }
            else
            {
                aimState = AimState.Idle;
            }
        }
    }

    public void Reload()
    {
        if (gun.Reload())
        {
            playerAnimator.SetTrigger("Reload");
        }
    }

    private void UpdateAimTarget()
    {
        RaycastHit hit;

        //카메라의 뷰포트상의 한점을 지정해 그 점으로 나아가는 레이를 생성함
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        //Raycast를 이용하여 ray로 부터 사정거리까지 충돌되는 물체가 있는지 확인함
        if (Physics.Raycast(ray, out hit, gun.fireDistance, ~excludeTarget)) {
            aimPoint = hit.point;

            //LineCast를 이용하여 총의 발사 위치부터 hit.point 까지 닿는 물체가 있는지 다시 한번 확인함
            if(Physics.Linecast(gun.fireTransform.position, hit.point, out hit, ~excludeTarget))
            {
                aimPoint = hit.point;
            }
        }
        else
        {
            aimPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }
    }

    private void UpdateUI()
    {
        if (gun == null || UIManager.Instance == null) return;
        
        UIManager.Instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        
        UIManager.Instance.SetActiveCrosshair(hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(aimPoint);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        //장전 중이거나 총이  null이 아닐 경우 아바타의 왼손을 총의 손잡이에 고정시켜준다.
        if (gun == null || gun.state == Gun.State.Reloading) return;

        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, gun.leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, gun.leftHandMount.rotation);
    }
}