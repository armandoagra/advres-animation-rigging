using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class FootIKStepper : MonoBehaviour
{
    [Header("IK")]
    public TwoBoneIKConstraint ikConstraint;
    public Transform ikTarget;       // o Transform que o Two Bone IK persegue

    [Header("Passo")]
    public float stepHeight = 0.18f;   // altura máxima do arco
    public float stepDuration = 0.22f; // segundos por passo

    // Posição atual plantada no chão (mundo)
    public Vector3 PlantedPosition { get; private set; }
    public bool IsStepping { get; private set; }

    void Awake()
    {
        // Inicializa plantado onde o pé já está
        PlantedPosition = ikTarget.position;
    }

    /// <summary>
    /// Levanta o pé, move em arco até toPos, e planta.
    /// </summary>
    public IEnumerator DoStep(Vector3 toPos)
    {
        IsStepping = true;
        Vector3 fromPos = PlantedPosition;

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);

            // Suaviza a curva de movimento horizontal
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            // Posição XZ interpola suavemente
            Vector3 pos = Vector3.Lerp(fromPos, toPos, smooth);

            // Altura: arco senoidal (zero nas pontas, máximo no meio)
            pos.y += Mathf.Sin(t * Mathf.PI) * stepHeight;

            ikTarget.position = pos;
            yield return null;
        }

        // Garante que chegou exatamente em toPos
        ikTarget.position = toPos;
        PlantedPosition = toPos;
        IsStepping = false;
    }

    /// <summary>
    /// Trava o pé onde está (sem animação).
    /// </summary>
    public void PlantAt(Vector3 pos)
    {
        PlantedPosition = pos;
        ikTarget.position = pos;
    }
}