using UnityEngine;

public class AmmoPack : MonoBehaviour, IItem
{
    public int ammo = 30;

    public void Use(GameObject target)
    {
        var playershooter = target.GetComponent<PlayerShooter>();
        if(playershooter != null && playershooter.gun != null)
        {
            playershooter.gun.ammoRemain += ammo;
        }

        Destroy(gameObject);
    }
}