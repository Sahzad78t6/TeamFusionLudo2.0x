using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipToolAxe : Equip
{
    public float attackRate = 0.5f;
    public float attackdistance = 4.0f;
    private bool attacking;
    private bool hasHitThisAttack;

    [Header("Combat")] 
    public bool doesDealDamage = true;
    public int damage = 15;

    [Header("Resource Gathering")]
    public bool doesGatherresources = true;

    //components
    private Animator anim;
    private Camera cam;

    [Header("Audio")]
    public AudioClip hitSound;

    private void Awake()
    {
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        cam = GetComponentInParent<PlayerController>()?.GetComponentInChildren<Camera>() ?? Camera.main;
    }

    public override void OnAttackInput()
    {
        if (!attacking)
        {
            attacking = true;
            hasHitThisAttack = false;

            if (anim == null) anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Attack");
            }

            // Guaranteed hit execution: delayed slightly to match swing motion
            StartCoroutine(DelayedHit());

            float rate = attackRate > 0.05f ? attackRate : 0.5f;
            Invoke("OnCanAttack", rate);
        }
    }

    private IEnumerator DelayedHit()
    {
        yield return new WaitForSeconds(0.12f);
        if (!hasHitThisAttack)
        {
            OnHit();
        }
    }

    void OnCanAttack()
    {
        attacking = false;
        hasHitThisAttack = false;
    }

    public void OnHit()
    {
        if (hasHitThisAttack) return;
        hasHitThisAttack = true;

        if (cam == null) cam = GetComponentInParent<PlayerController>()?.GetComponentInChildren<Camera>() ?? Camera.main;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float dist = Mathf.Max(attackdistance, 4.0f);
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, dist);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.layer == 8 || hit.collider.gameObject.layer == 2) continue;
            if (hit.collider.transform.IsChildOf(transform) || (PlayerController.instance != null && hit.collider.transform.IsChildOf(PlayerController.instance.transform))) continue;
            if (hit.collider.isTrigger) continue;

            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, hit.point);
            }

            // 1. Wood / Trees Gathering
            var res = hit.collider.GetComponent<Resources>() ?? hit.collider.GetComponentInParent<Resources>();
            if (res != null)
            {
                res.Gather(hit.point, hit.normal);
                return;
            }

            var fruitTree = hit.collider.GetComponent<ResourceFruitTree>() ?? hit.collider.GetComponentInParent<ResourceFruitTree>();
            if (fruitTree != null)
            {
                fruitTree.Gather(hit.point, hit.normal);
                return;
            }

            // 2. Stone
            var stone = hit.collider.GetComponent<ResourceStone>() ?? hit.collider.GetComponentInParent<ResourceStone>();
            if (stone != null)
            {
                stone.Gather(hit.point, hit.normal);
                return;
            }

            // 3. Combat
            var damagable = hit.collider.GetComponent<IDamagable>() ?? hit.collider.GetComponentInParent<IDamagable>();
            if (damagable != null)
            {
                damagable.TakePhysicDamage(damage > 0 ? damage : 15);
                return;
            }

            break;
        }
    }
}