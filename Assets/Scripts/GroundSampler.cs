using UnityEngine;

public static class GroundSampler
{
    public static bool Sample(Vector3 from, LayerMask groundMask, out Vector3 groundPoint)
    {
        Vector3 origin = from + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f, groundMask))
        {
            groundPoint = hit.point;
            return true;
        }
        groundPoint = from;
        return false;
    }
}