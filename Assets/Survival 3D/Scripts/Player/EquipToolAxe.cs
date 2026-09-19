using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipToolAxe : Equip
{
    public float attackRate;
    public float attackdistance;
    private bool attacking;

    [Header("Combat")] 
    public bool doesDealDamage;
    public int damage;

    [Header("Resource Gathering")]
    public bool doesGatherresources;

    //components
    private Animator anim;
    private Camera cam;

    private void Awake()
    {
        //get components
        anim = GetComponent<Animator>();
        cam = Camera.main;
    }

    public override void OnAttackInput()
    {
        if (!attacking)
        {
            attacking = true;
            anim.SetTrigger("Attack");
            Invoke("OnCanAttack",attackRate);
        }
    }
    void OnCanAttack()
    {
        attacking = false;
    }

    [Header("Audio")]
    public AudioClip hitSound;

    public void OnHit()
    {
        if (cam == null) cam = GetComponentInParent<PlayerController>()?.GetComponentInChildren<Camera>() ?? Camera.main;
        if (cam == null) return;

        float dist = Mathf.Max(attackdistance, 3.5f);
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        int ignoreMask = (1 << 8) | (1 << 2);
        if (Physics.Raycast(ray, out hit, dist, ~ignoreMask))
        {
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, hit.point);
            }

            // Resource Gathering (Wood / Trees)
            if (doesGatherresources)
            {
                var res = hit.collider.GetComponent<Resources>() ?? hit.collider.GetComponentInParent<Resources>();
                if (res != null) res.Gather(hit.point, hit.normal);

                var fruitTree = hit.collider.GetComponent<ResourceFruitTree>() ?? hit.collider.GetComponentInParent<ResourceFruitTree>();
                if (fruitTree != null) fruitTree.Gather(hit.point, hit.normal);
            }

            // Combat / Damagable
            if (doesDealDamage)
            {
                var damagable = hit.collider.GetComponent<IDamagable>() ?? hit.collider.GetComponentInParent<IDamagable>();
                if (damagable != null)
                {
                    damagable.TakePhysicDamage(damage);
                }
            }
        }
    }
    
    
}