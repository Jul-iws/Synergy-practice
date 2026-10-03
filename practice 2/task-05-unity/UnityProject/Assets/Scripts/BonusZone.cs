using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class BonusZone : MonoBehaviour
{
    [SerializeField] private string animatorParameter = "InBonusZone";

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        SetAnimatorState(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        SetAnimatorState(other, false);
    }

    private void SetAnimatorState(Collider other, bool value)
    {
        var animator = other.GetComponentInParent<Animator>();
        if (animator != null) animator.SetBool(animatorParameter, value);
    }
}

