using UnityEngine;
using UnityEngine.Animations.Rigging;

public class BodyTurnController : MonoBehaviour
{
    [Header("Referências")]
    public Transform hips;
    public Transform lookTarget;
    public TwoBoneIKConstraint leftLegIK;
    public TwoBoneIKConstraint rightLegIK;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    
    [Header("Configurações")]
    public float maxTorsoAngle = 50f;
    public float stepDuration = 0.3f;
    public float stepRadius = 0.5f;

    private bool isAdjustingFeet = false;
    private float lastAdjustTime;

    void Update()
    {
        Debug.Log("a");
        // Direção do olhar no plano horizontal
        Vector3 lookDir = lookTarget.position - hips.position;
        lookDir.y = 0;
        if (lookDir.magnitude < 0.01f) return;
        
       Debug.Log("b"); 
        Vector3 hipsForward = hips.forward;
        hipsForward.y = 0;
        
        float angle = Vector3.SignedAngle(hipsForward, lookDir, Vector3.up);
        angle = Mathf.Abs(angle);

        if (angle > maxTorsoAngle && !isAdjustingFeet && Time.time - lastAdjustTime > 1f)
        {
            Debug.Log("c");
            StartCoroutine(AdjustFeet(-angle)); // sinal para direção do giro
        }
    }

    System.Collections.IEnumerator AdjustFeet(float rotationAngle)
    {
        Debug.Log("d");
        isAdjustingFeet = true;
        lastAdjustTime = Time.time;

        // Desativar IK das pernas para permitir movimento livre
        leftLegIK.weight = 0f;
        rightLegIK.weight = 0f;

        // Calcular nova posição dos foot targets (giro ao redor do centro entre os pés)
        Vector3 center = (leftFootTarget.position + rightFootTarget.position) * 0.5f;
        float stepAngle = Mathf.Sign(rotationAngle) * stepRadius; // ângulo do passo

        // Interpolar targets em arco
        Quaternion rotLeft = Quaternion.Euler(0, stepAngle, 0);
        Quaternion rotRight = Quaternion.Euler(0, -stepAngle, 0);
        Vector3 leftStart = leftFootTarget.position;
        Vector3 rightStart = rightFootTarget.position;
        Vector3 leftEnd = center + rotLeft * (leftStart - center);
        Vector3 rightEnd = center + rotRight * (rightStart - center);

        float t = 0;
        while (t < stepDuration)
        {
            Debug.Log("e");
            t += Time.deltaTime;
            float blend = Mathf.SmoothStep(0, 1, t / stepDuration);
            leftFootTarget.position = Vector3.Lerp(leftStart, leftEnd, blend);
            rightFootTarget.position = Vector3.Lerp(rightStart, rightEnd, blend);
            yield return null;
        }
Debug.Log("f");
        // Reativar IK
        leftLegIK.weight = 1f;
        rightLegIK.weight = 1f;
        isAdjustingFeet = false;
    }
}