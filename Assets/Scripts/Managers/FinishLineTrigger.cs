using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class FinishLineTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask triggerLayers = ~0;

    private void Awake()
    {
        ConfigurePhysics();
    }

    private void Reset()
    {
        ConfigurePhysics();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInTriggerLayers(other))
            return;

        ReportMover(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsInTriggerLayers(other))
            return;

        ReportMover(other);
    }

    private bool IsInTriggerLayers(Collider other)
    {
        return (triggerLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    private void ConfigurePhysics()
    {
        var boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        var rigidbody = GetComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    private void ReportMover(Collider other)
    {
        foreach (var behaviour in other.GetComponentsInParent<MonoBehaviour>())
        {
            if (behaviour is IMovable movable && GameSys.Instance != null)
            {
                GameSys.Instance.ReportFinished(movable);
                return;
            }
        }
    }
}
