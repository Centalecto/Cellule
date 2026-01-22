using UnityEngine;
using System.Collections.Generic;

public class CultureManager : MonoBehaviour
{
    [Header("Culture")]
    [SerializeField] private float culturePlaneY = 0.069f;
    [SerializeField] private Vector3 startPosition = Vector3.zero;

    [Header("Cellule de base")]
    [SerializeField] private GameObject baseCellPrefab;

    public AudioSource SonReset;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCulture();
        }
    }

    public void ResetCulture()
    {
        MIC_Division[] allCells = Object.FindObjectsByType<MIC_Division>(FindObjectsSortMode.None);

        int aliveCells = 0;

        SonReset.Play();

        foreach (MIC_Division cell in allCells)
        {
            cell.DestroyCell();
        }


        bool hasStoredCell = false;
       

        // Si aucune cellule stockée ? recréer une cellule de base
        if (!hasStoredCell)
        {
            SpawnBaseCell();
        }
    }

    private void SpawnBaseCell()
    {
        Vector3 pos = startPosition;
        pos.y = culturePlaneY;

        GameObject cellGO = Instantiate(baseCellPrefab, pos, Quaternion.identity);

        MIC_Division cell = cellGO.GetComponent<MIC_Division>();
        if (cell != null)
        {
            cell.enabled = true;        // CRITIQUE

        }
    }
}
