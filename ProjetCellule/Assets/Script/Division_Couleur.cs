using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Division_Couleur : MonoBehaviour
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

    [Header("Mutation de couleur")]
    [SerializeField, Range(0f, 1f)] private float mutationChance = 0.25f;

    [SerializeField]
    private List<Color> colorCycle = new List<Color>()
    {
        Color.blue,
        Color.green,
        Color.yellow
    };

    private int currentColorIndex = 0;
    private Renderer cellRenderer;

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
        cellRenderer = GetComponent<Renderer>();

        if (cellRenderer != null && colorCycle.Count > 0)
        {
            // Choisir une couleur initiale aléatoire pour la première cellule
            currentColorIndex = Random.Range(0, colorCycle.Count);
            ApplyCurrentColor();
        }

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

    void ApplyCurrentColor()
    {
        if (cellRenderer != null && colorCycle.Count > 0)
        {
            cellRenderer.material.color = colorCycle[currentColorIndex];
        }
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

        // Appliquer mutation uniquement au clone
        ApplyMutationToClone(clone);

        Debug.Log("Division - cellules totales : " + NombreCellules);

        transform.localScale = startScale;
        transform.position = startPos;

        pushTimers.Clear();
        isStretching = false;
    }

    void ApplyMutationToClone(GameObject clone)
    {
        Division_Couleur cloneDivision = clone.GetComponent<Division_Couleur>();
        if (cloneDivision == null) return;

        // Si mutation se produit
        if (Random.value <= mutationChance)
        {
            // Calculer le nouvel index de couleur
            int newIndex;

            // 50% chance d'aller en avant, 50% chance d'aller en arrière
            if (Random.value > 0.5f)
            {
                // Avancer dans le cycle
                newIndex = (currentColorIndex + 1) % colorCycle.Count;
            }
            else
            {
                // Reculer dans le cycle
                newIndex = currentColorIndex - 1;
                if (newIndex < 0) newIndex = colorCycle.Count - 1;
            }

            // Appliquer la nouvelle couleur au clone
            cloneDivision.SetColorIndex(newIndex);
            Debug.Log($"Mutation appliquée au clone: Index {newIndex}");
        }
        else
        {
            // Pas de mutation, clone garde la même couleur
            cloneDivision.SetColorIndex(currentColorIndex);
        }
    }

    // Méthode pour définir l'index de couleur (utilisée par les clones)
    public void SetColorIndex(int index)
    {
        if (index >= 0 && index < colorCycle.Count)
        {
            currentColorIndex = index;
            ApplyCurrentColor();
        }
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