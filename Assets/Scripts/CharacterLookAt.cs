using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CharacterLookAt : MonoBehaviour
{
    // ── Referências ────────────────────────────────────────────────
    [Header("Target")]
    public Transform target;

    [Header("Aim Constraints")]
    public MultiAimConstraint headAim;
    public MultiAimConstraint spineAim;
    [Range(0f,1f)] public float headWeight  = 1f;
    [Range(0f,1f)] public float spineWeight = 0.5f;

    [Header("Pés")]
    public FootIKStepper leftFoot;
    public FootIKStepper rightFoot;
    public LayerMask groundMask;

    [Header("Rotação")]
    [Tooltip("Ângulo a partir do qual o personagem gira o corpo todo")]
    public float rotationThreshold = 45f;

    [Tooltip("Graus por segundo durante o giro")]
    public float rotationSpeed = 80f;

    [Tooltip("Quantos graus o corpo gira por passo")]
    public float degreesPerStep = 30f;

    // ── Estado interno ─────────────────────────────────────────────
    private bool isRotating = false;

    // ── Update: aim constraint + gatilho de rotação ─────────────────
    void Update()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f) return;

        float angle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);

        if (!isRotating && Mathf.Abs(angle) > rotationThreshold)
            StartCoroutine(TurnWithSteps(angle));
    }

    // ── Coroutine principal ─────────────────────────────────────────
    IEnumerator TurnWithSteps(float initialAngle)
    {
        isRotating = true;

        // 1. Suaviza os aim constraints para neutro
        yield return StartCoroutine(FadeConstraints(headWeight, spineWeight, 0f, 0f, 0.12f));

        // 2. Decide qual pé começa (o que está "para trás" em relação ao giro)
        bool leftFirst = initialAngle < 0f;

        // 3. Gira em passos até estar alinhado
        while (true)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float remaining = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);

            if (Mathf.Abs(remaining) <= 5f) break;

            // Quanto girar neste passo (no máximo degreesPerStep)
            float stepAngle = Mathf.Sign(remaining)
                              * Mathf.Min(Mathf.Abs(remaining), degreesPerStep);

            // Pé que se move neste passo (alternado)
            FootIKStepper movingFoot  = leftFirst ? leftFoot  : rightFoot;
            FootIKStepper stayingFoot = leftFirst ? rightFoot : leftFoot;
            leftFirst = !leftFirst;

            // Calcula onde o pé vai pousar:
            // projeta a posição atual do pé após a rotação parcial
            Vector3 footAfterRot = RotatePointAround(
                movingFoot.PlantedPosition,
                transform.position,
                stepAngle
            );

            // Sobe para a altura do quadril e faz raycast para o chão
            footAfterRot.y = transform.position.y + 1f;
            if (!GroundSampler.Sample(footAfterRot, groundMask, out Vector3 landPos))
                landPos = footAfterRot; // fallback: sem chão detectado

            // Executa o passo e a rotação do corpo em paralelo
            yield return StartCoroutine(StepAndRotate(
                movingFoot, landPos, stepAngle
            ));
        }

        // 4. Restaura os aim constraints
        yield return StartCoroutine(FadeConstraints(0f, 0f, headWeight, spineWeight, 0.2f));

        isRotating = false;
    }

    // ── Passo + rotação em paralelo ─────────────────────────────────
    IEnumerator StepAndRotate(FootIKStepper foot, Vector3 landPos, float angle)
    {
        // Dispara o passo (não esperamos — queremos paralelo)
        StartCoroutine(foot.DoStep(landPos));

        // Rotaciona o corpo durante a mesma duração do passo
        float elapsed   = 0f;
        float duration  = foot.stepDuration;
        Quaternion from = transform.rotation;
        Quaternion to   = from * Quaternion.Euler(0f, angle, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.rotation = to;

        // Aguarda o pé terminar caso o Slerp tenha acabado antes
        yield return new WaitWhile(() => foot.IsStepping);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    IEnumerator FadeConstraints(float fH, float fS, float tH, float tS, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            headAim.weight  = Mathf.Lerp(fH, tH, t);
            spineAim.weight = Mathf.Lerp(fS, tS, t);
            yield return null;
        }
        headAim.weight  = tH;
        spineAim.weight = tS;
    }

    /// <summary>Rotaciona um ponto ao redor de um pivô no plano XZ.</summary>
    Vector3 RotatePointAround(Vector3 point, Vector3 pivot, float degrees)
    {
        Vector3 dir = point - pivot;
        dir = Quaternion.Euler(0f, degrees, 0f) * dir;
        return pivot + dir;
    }
}