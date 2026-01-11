using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MIC_Division : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float interval = 5f;
    [SerializeField] private float stretchDuration = 0.5f;

    //Pour limiter le nombre de cellules
    [Header("Limite de population")]
    [SerializeField] private int MaxCellules = 100;
    private static int NombreCellules = 0;

    //Durée de vie des cellules
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

    [Header("Mutation visuelle")]
    [SerializeField, Range(0f, 1f)] private float mutationChance = 0.25f;

    [SerializeField] private List<Renderer> mutableRenderers = new List<Renderer>();
    [SerializeField] private List<Material> possibleMaterials = new List<Material>();

    private float divisionTimer;

    private bool isStretching = false;
    private Rigidbody rb;
    private Vector3 divisionDirection;

    private Dictionary<Rigidbody, float> pushTimers = new Dictionary<Rigidbody, float>();


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
        TempsDeVie = 0;
    }

    void Start()
    {
        transform.localScale = startScale;
        divisionTimer = interval;
    }

    void Update()
    {
        TempsDeVie += Time.deltaTime;
        if (TempsDeVie >= MaxDuréeDeVie)
        {
            Destroy(gameObject);
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

            transform.localScale = Vector3.Lerp(startScale, maxStretchScale, t);
            transform.position = startPos + divisionDirection * (t * 0.25f);

            PushCells(divisionDirection);

            yield return null;
        }

        Vector3 clonePos = transform.position + divisionDirection * (maxStretchScale.x * 0.5f);
        GameObject clone = Instantiate(gameObject, clonePos, Quaternion.identity);
        ApplyVisualMutation(clone);

        Debug.Log("Division ? cellules totales : " + NombreCellules);

        transform.localScale = startScale;
        transform.position = startPos;

        pushTimers.Clear();
        isStretching = false;
    }

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

            // Timer de collision prolongée
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
                // Poussée normale
                otherRb.AddForce(dir * pushForce, ForceMode.Force);
            }
        }
    }

    void ApplyVisualMutation(GameObject clone)
    {
        // Test de chance
        if (Random.value > mutationChance) return;

        MIC_Division cloneDivision = clone.GetComponent<MIC_Division>();
        if (cloneDivision == null) return;

        if (cloneDivision.mutableRenderers.Count == 0) return;
        if (cloneDivision.possibleMaterials.Count == 0) return;

        // Choix aléatoire du Renderer
        Renderer targetRenderer =
            cloneDivision.mutableRenderers[Random.Range(0, cloneDivision.mutableRenderers.Count)];

        // Choix aléatoire du matériau
        Material newMat =
            cloneDivision.possibleMaterials[Random.Range(0, cloneDivision.possibleMaterials.Count)];

        // IMPORTANT : material (instance unique)
        targetRenderer.material = newMat;
    }

    void FixedUpdate()
    {
        if (!isStretching)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, damping * Time.fixedDeltaTime);

            if (rb.linearVelocity.magnitude < sleepVelocityThreshold)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        if (rb.linearVelocity.magnitude > maxVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + divisionDirection * pushRadius, pushRadius);
    }
}