using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class MaterialMutation
{
    public Material material;

    [Header("Effets (valeurs fixes)")]
    public float scaleDelta;
    public float lifetimeDelta;
    public float divisionIntervalDelta;
}

public class MIC_Division : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float interval = 5f;
    [SerializeField] private float stretchDuration = 0.5f;

    [Header("Limite de population")]
    [SerializeField] private int MaxCellules = 100;
    private static int NombreCellules = 0;

    [Header("Durée de vie")]
    public float MaxDuréeDeVie = 20f;
    public float TempsDeVie = 0f;

    [Header("Scale")]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 maxStretchScale = new Vector3(2f, 1f, 1f);

    [Header("Poussée")]
    [SerializeField] private float pushForce = 5f;
    [SerializeField] private float separationForce = 8f;
    [SerializeField] private float pushRadius = 0.6f;
    [SerializeField] private float stuckTimeThreshold = 0.5f;
    [SerializeField] private LayerMask cellLayer;

    [Header("Stabilisation")]
    [SerializeField] private float maxVelocity = 2f;
    [SerializeField] private float damping = 5f;
    [SerializeField] private float sleepVelocityThreshold = 0.05f;

    // =========================
    // ?? MUTATIONS PAR MATÉRIAU
    // =========================

    [Header("Mutation")]
    [SerializeField, Range(0f, 1f)] private float mutationChance = 0.25f;

    [Tooltip("1 Renderer = 1 slot de mutation")]
    [SerializeField] private List<Renderer> mutationSlots = new List<Renderer>();

    [Tooltip("Table Matériau ? Effets")]
    [SerializeField] private List<MaterialMutation> materialMutations = new List<MaterialMutation>();

    // =========================
    // VALEURS DE BASE (IMMUTABLES)
    // =========================

    private float baseLifetime;
    private float baseInterval;
    private Vector3 currentBaseScale;

    private float divisionTimer;
    private bool isStretching = false;
    private Rigidbody rb;
    private Vector3 divisionDirection;

    private Dictionary<Rigidbody, float> pushTimers = new Dictionary<Rigidbody, float>();

    // =========================
    // CYCLE DE VIE
    // =========================

    void OnEnable()
    {
        NombreCellules++;
        Debug.Log("Cellules : " + NombreCellules);
    }

    void OnDisable()
    {
        NombreCellules--;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        TempsDeVie = 0f;
    }

    void Start()
    {
        // Sauvegarde des valeurs de base
        baseLifetime = MaxDuréeDeVie;
        baseInterval = interval;

        divisionTimer = interval;

        RecalculateStatsFromMaterials();
    }

    void Update()
    {
        TempsDeVie += Time.deltaTime;
        if (TempsDeVie >= MaxDuréeDeVie)
        {
            Destroy(gameObject);
            return;
        }

        if (isStretching) return;
        if (NombreCellules >= MaxCellules) return;

        divisionTimer -= Time.deltaTime;

        if (divisionTimer <= 0f)
        {
            divisionTimer = interval;
            TriggerStretch();
        }
    }

    void OnMouseDown()
    {
        TriggerStretch();
    }

    void TriggerStretch()
    {
        if (isStretching) return;
        if (NombreCellules >= MaxCellules) return;

        divisionDirection = RandomHorizontalDirection();
        StartCoroutine(StretchAndDivide());
    }

    IEnumerator StretchAndDivide()
    {
        isStretching = true;

        Vector3 startPos = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / stretchDuration;

            // ?? étirement respectant les mutations
            transform.localScale = Vector3.Lerp(
                currentBaseScale,
                Vector3.Scale(currentBaseScale, maxStretchScale),
                t
            );

            transform.position = startPos + divisionDirection * (t * 0.25f);

            PushCells(divisionDirection);
            yield return null;
        }

        Vector3 clonePos = transform.position + divisionDirection * (currentBaseScale.x * 0.5f);
        GameObject clone = Instantiate(gameObject, clonePos, Quaternion.identity);

        ApplyMaterialMutation(clone);

        transform.localScale = currentBaseScale;
        transform.position = startPos;

        pushTimers.Clear();
        isStretching = false;
    }

    // =========================
    // ?? MUTATIONS
    // =========================

    void ApplyMaterialMutation(GameObject clone)
    {
        if (Random.value > mutationChance) return;

        MIC_Division cd = clone.GetComponent<MIC_Division>();
        if (cd == null) return;
        if (cd.mutationSlots.Count == 0) return;
        if (cd.materialMutations.Count == 0) return;

        int slotIndex = Random.Range(0, cd.mutationSlots.Count);
        MaterialMutation mutation =
            cd.materialMutations[Random.Range(0, cd.materialMutations.Count)];

        // IMPORTANT : sharedMaterial
        cd.mutationSlots[slotIndex].sharedMaterial = mutation.material;
        cd.RecalculateStatsFromMaterials();
    }

    void RecalculateStatsFromMaterials()
    {
        float scaleMultiplier = 1f;
        float lifetime = baseLifetime;
        float divInterval = baseInterval;

        foreach (Renderer slot in mutationSlots)
        {
            if (slot == null) continue;

            Material mat = slot.sharedMaterial;
            if (mat == null) continue;

            MaterialMutation mutation = GetMutationForMaterial(mat);
            if (mutation == null) continue;

            scaleMultiplier += mutation.scaleDelta;
            lifetime += mutation.lifetimeDelta;
            divInterval += mutation.divisionIntervalDelta;
        }

        scaleMultiplier = Mathf.Max(0.2f, scaleMultiplier);
        lifetime = Mathf.Max(1f, lifetime);
        divInterval = Mathf.Max(0.5f, divInterval);

        // ?? conservation de la forme + mutations visibles
        currentBaseScale = new Vector3(
            startScale.x * scaleMultiplier,
            startScale.y * scaleMultiplier,
            startScale.z * scaleMultiplier
        );

        transform.localScale = currentBaseScale;

        MaxDuréeDeVie = lifetime;
        interval = divInterval;
    }

    MaterialMutation GetMutationForMaterial(Material mat)
    {
        foreach (MaterialMutation m in materialMutations)
        {
            if (m.material == mat)
                return m;
        }
        return null; // mutation neutre
    }

    // =========================
    // PHYSIQUE
    // =========================

    Vector3 RandomHorizontalDirection()
    {
        float angle = Random.Range(0f, 360f);
        return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
    }

    void PushCells(Vector3 dir)
    {
        Vector3 center = transform.position + dir * pushRadius;
        Collider[] hits = Physics.OverlapSphere(center, pushRadius, cellLayer);

        foreach (Collider hit in hits)
        {
            if (hit.attachedRigidbody == null) continue;
            if (hit.gameObject == gameObject) continue;

            Rigidbody otherRb = hit.attachedRigidbody;

            if (!pushTimers.ContainsKey(otherRb))
                pushTimers[otherRb] = 0f;

            pushTimers[otherRb] += Time.deltaTime;

            if (pushTimers[otherRb] > stuckTimeThreshold)
            {
                Vector3 separationDir = (otherRb.position - rb.position).normalized;
                otherRb.AddForce(separationDir * separationForce, ForceMode.Impulse);
                rb.AddForce(-separationDir * separationForce, ForceMode.Impulse);
            }
            else
            {
                otherRb.AddForce(dir * pushForce, ForceMode.Force);
            }
        }
    }

    void FixedUpdate()
    {
        if (!isStretching)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, damping * Time.fixedDeltaTime);

            if (rb.linearVelocity.magnitude < sleepVelocityThreshold)
                rb.linearVelocity = Vector3.zero;
        }

        if (rb.linearVelocity.magnitude > maxVelocity)
            rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + divisionDirection * pushRadius, pushRadius);
    }
}