using UnityEngine;

public class LookAtTarget : MonoBehaviour
{
    [SerializeField] private Transform target;

    void Update()
    {
        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toTarget);
                transform.rotation = targetRotation;
            }
        }
    }
}
