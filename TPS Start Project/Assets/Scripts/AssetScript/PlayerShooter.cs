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
        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable()
    {
        gun.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (playerInput.fire)
        {
            Shoot();
        }
        else if (playerInput.reload)
        {
            Reload();
        }
    }

    private void Update()
    {

    }

    public void Shoot()
    {
        if(aimState == AimState.Idle)
        {

        }
    }

    public void Reload()
    {
        
    }

    private void UpdateAimTarget()
    {
 
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

    }
}