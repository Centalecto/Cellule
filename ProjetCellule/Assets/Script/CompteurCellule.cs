using UnityEngine;
using System.Collections;

public class CompteurCellule : MonoBehaviour
{
    // -------- SURPOPULATION --------
    [Header("Surcharge de population")]
    [SerializeField] private bool enableOverpopulationPenalty = true;
    [SerializeField] private int populationLimit = 50;
    [SerializeField] private float lifetimePenalty = 5f;

    public static bool IsOverpopulated { get; private set; }
    public static float CurrentLifetimePenalty { get; private set; }

    // -------- SOUS-POPULATION --------
    [Header("Sous-population")]
    [SerializeField] private bool enableUnderpopulation = true;
    [SerializeField] private int underpopulationThreshold = 20;
    [SerializeField] private float lifetimeBonus = 5f;

    public static bool IsUnderpopulated { get; private set; }
    public static float CurrentLifetimeBonus { get; private set; }

    // -------- COMPTEUR --------
    [Header("Fréquence de vérification (secondes)")]
    [SerializeField] private float checkInterval = 1f;

    public int CurrentCellCount { get; private set; }
    public float NombreCellules => CurrentCellCount;

    private Coroutine monitorRoutine;

    void OnEnable()
    {
        monitorRoutine = StartCoroutine(MonitorCells());
    }

    void OnDisable()
    {
        if (monitorRoutine != null)
            StopCoroutine(monitorRoutine);
    }

    IEnumerator MonitorCells()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            CountCells();
            EvaluatePopulation();
            yield return wait;
        }
    }

    void CountCells()
    {
        MIC_Division[] cells = Object.FindObjectsByType<MIC_Division>(
            FindObjectsSortMode.None
        );

        CurrentCellCount = cells.Length;
    }

    void EvaluatePopulation()
    {
        // ----- SURPOPULATION -----
        IsOverpopulated =
            enableOverpopulationPenalty &&
            CurrentCellCount >= populationLimit;

        CurrentLifetimePenalty =
            IsOverpopulated ? lifetimePenalty : 0f;

        // ----- SOUS-POPULATION -----
        IsUnderpopulated =
            enableUnderpopulation &&
            CurrentCellCount <= underpopulationThreshold;

        CurrentLifetimeBonus =
            IsUnderpopulated ? lifetimeBonus : 0f;

        Debug.Log(
            $"[CompteurCellule] Cellules : {CurrentCellCount} | " +
            $"Surpop : {IsOverpopulated} | Sous-pop : {IsUnderpopulated}"
        );
    }
}