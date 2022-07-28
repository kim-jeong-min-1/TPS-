using UnityEngine;

//클래스가 아니라 구조체인 이유는 클래스는 레퍼런스 타입이기 때문에
//공격을 받은 측에서 임의로 DamageMessage의 값을 수정하면
//같은 DamageMessage를 전달받은 다른 스크립트에서도 변경 사항이 똑같이 전달 되기 때문
public struct DamageMessage
{ 
    public GameObject damager;
    public float amount;

    public Vector3 hitPoint;
    public Vector3 hitNormal;
}