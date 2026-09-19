using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class EquipManager : MonoBehaviour
{
    public Equip currentEquip;
    public Transform equipParent;
    private PlayerController controller;
    
    
    //s
    public static EquipManager instance;

    private void Awake()
    {
        instance = this;
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (controller == null) controller = PlayerController.instance ?? GetComponent<PlayerController>();

        if (Input.GetMouseButtonDown(0) && (controller == null || controller.canLook))
        {
            if (currentEquip != null)
            {
                currentEquip.OnAttackInput();
            }
            else
            {
                UnarmedAttack();
            }
        }
        if (Input.GetMouseButtonDown(1) && currentEquip != null && (controller == null || controller.canLook))
        {
            currentEquip.OnAltAttackInput();
        }
    }

    public void UnarmedAttack()
    {
        Camera cam = controller != null ? controller.GetComponentInChildren<Camera>() : Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        int ignoreMask = (1 << 8) | (1 << 2);
        if (Physics.Raycast(ray, out hit, 3.5f, ~ignoreMask))
        {
            var res = hit.collider.GetComponent<Resources>() ?? hit.collider.GetComponentInParent<Resources>();
            if (res != null) res.Gather(hit.point, hit.normal);

            var fruitTree = hit.collider.GetComponent<ResourceFruitTree>() ?? hit.collider.GetComponentInParent<ResourceFruitTree>();
            if (fruitTree != null) fruitTree.Gather(hit.point, hit.normal);

            var stone = hit.collider.GetComponent<ResourceStone>() ?? hit.collider.GetComponentInParent<ResourceStone>();
            if (stone != null) stone.Gather(hit.point, hit.normal);

            var damagable = hit.collider.GetComponent<IDamagable>() ?? hit.collider.GetComponentInParent<IDamagable>();
            if (damagable != null) damagable.TakePhysicDamage(5);
        }
    }

    public void OnAttackInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed && (controller == null || controller.canLook))
        {
            if (currentEquip != null)
                currentEquip.OnAttackInput();
            else
                UnarmedAttack();
        }
    }
    
    public void OnAltAttackInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed && currentEquip != null && controller.canLook == true)
        {
            currentEquip.OnAltAttackInput();
        }
    }

    public void EquipNewItem(ItemDatabase item)
    {
        UnEquipItem();
        currentEquip = Instantiate(item.equipPrefab, equipParent).GetComponent<Equip>();
    }

    public void UnEquipItem()
    {
        if (currentEquip != null)
        {
            Destroy(currentEquip.gameObject);
            currentEquip = null;
        }
    }
    
    
}
