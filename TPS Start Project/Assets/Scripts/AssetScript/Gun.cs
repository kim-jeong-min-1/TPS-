using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum State
    {
        Ready,
        Empty,
        Reloading
    }
    public State state { get; private set; }
    
    private PlayerShooter gunHolder;
    private LineRenderer bulletLineRenderer;
    
    private AudioSource gunAudioPlayer;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    
    public ParticleSystem muzzleFlashEffect;
    public ParticleSystem shellEjectEffect;
    
    public Transform fireTransform;
    public Transform leftHandMount;

    public float damage = 25;
    public float fireDistance = 100f;

    public int ammoRemain = 100;
    public int magAmmo;
    public int magCapacity = 30;

    public float timeBetFire = 0.12f;
    public float reloadTime = 1.8f;
    
    [Range(0f, 10f)] public float maxSpread = 3f;
    [Range(1f, 10f)] public float stability = 1f;
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;
    private float currentSpread;
    private float currentSpreadVelocity;

    private float lastFireTime;

    private LayerMask excludeTarget;

    private void Awake()
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        bulletLineRenderer.positionCount = 2;
        bulletLineRenderer.enabled = false;
    }

    //총을 쏘는 사람을 받아옴
    public void Setup(PlayerShooter gunHolder)
    {
       //입력받은 gunHolder를 현재 스크립트에 gunHolder에 넘겨줌
        this.gunHolder = gunHolder;
        //현재 excludeTarget에 받아온 gunHolder의 excludeTarget으로 함
        excludeTarget = gunHolder.excludeTarget;
    }

    private void OnEnable()
    {
        //현재 탄창의 탄약수를 최대용량까지 채워줌
        magAmmo = magCapacity;
        //탄 퍼짐을 가장 낮은 값으로
        currentSpread = 0f;
        lastFireTime = 0f;
        //총의 상태를 발사 가능한 상태로 변경
        state = State.Ready;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool Fire(Vector3 aimTarget)
    {
        //현재 총의 상태가 발사가능한 상태이고, 현재 시간이 마지막으로 발사한 시간 + 발사 간격보다 크거나 같다면 
        if(state == State.Ready && Time.time >= lastFireTime + timeBetFire)
        {
            //총알의 발사 위치부터 에임까지의 거리, 방향 벡터를 구함
            var fireDirection = aimTarget - fireTransform.position;

            //탄 퍼짐을 구현하기 위해 currentSpread 값 만큼 0으로 부터 차이나는 값을 구함
            var xError = Utility.GedRandomNormalDistribution(0f, currentSpread);
            var yError = Utility.GedRandomNormalDistribution(0f, currentSpread);

            //구해진 차이값 만큼 Quaternion.AngleAxis로 회전 시킨 값에 방향벡터를 곱해 최종적인 탄퍼짐을 구현
            fireDirection = Quaternion.AngleAxis(yError, Vector3.up) * fireDirection;
            fireDirection = Quaternion.AngleAxis(xError, Vector3.right) * fireDirection;

            //총을 쏠때마다 탄 퍼짐이 증가하게 함 stability 값이 낮을 수록 탄 퍼짐이 심해짐
            currentSpread += 1f / stability;

            lastFireTime = Time.time;
            Shot(fireTransform.position, fireDirection);

            return true;
        }
        return false;
    }
    
    //총알 발사 메서드
    private void Shot(Vector3 startPoint, Vector3 direction)
    {
        RaycastHit hit;
        Vector3 hitPosition;

        if(Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget))
        {
            var target = hit.collider.GetComponent<IDamageable>();

            //대상이 IDamageable 인터페이스를 가지고 있어 데미지를 받을 수 있는 상태라면
            if(target != null)
            {
                DamageMessage damageMessage;
                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            }
            else
            {
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }
            hitPosition = hit.point;
        }
        else
        {
            //아닐 경우 시작지점에 발사한 방향으로 최대사거리만큼 간 거리를 더해서 hitPosition에 저장함
            hitPosition = startPoint + direction * fireDistance;
        }

        StartCoroutine(ShotEffect(hitPosition));

        //발사할때마다 탄창의 총알개수를 감소함
        magAmmo--;
        //탄창이 비어있는 상태라면 총의 현재 상태를 Empty로 변경한다.
        if (magAmmo <= 0) state = State.Empty;
    }

    private IEnumerator ShotEffect(Vector3 hitPosition)
    {
        //총 발사 사운드, 이펙트를 재생함
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip);
        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);

        yield return new WaitForSeconds(0.03f);
        bulletLineRenderer.enabled = false;
    }
    
    public bool Reload()
    {
        //이미 장전중인 상태거나, 가진 총알의 개수가 없거나, 탄창의 총알 개수가 이미 최대치라면 장전하지 않음
        if(state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());
        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        //현재 상태를 장전중인 상태인 Reloading으로 변경
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(reloadTime);

        //탄창에 장전될 총알의 개수를 구함(현재 가지고 있는 총알 개수 까지만)
        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);

        //총알을 장전해줌
        magAmmo += ammoToFill;
        //가지고 있는 총알의 개수에서 장전한 만큼을 빼줌
        ammoRemain -= ammoToFill;

        //장전이 끝났으니 상태를 다시 발사 가능한 Ready로 변경
        state = State.Ready;
    }

    private void Update()
    {
        //탄 퍼짐의 값이 최대치를 넘지 않게 조절
        currentSpread = Mathf.Clamp(currentSpread, 0, maxSpread);

        //탄 퍼짐 값을 매 프레임 마다 부드럽게 0으로 이동하게끔 함
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1f / restoreFromRecoilSpeed);
    }
}