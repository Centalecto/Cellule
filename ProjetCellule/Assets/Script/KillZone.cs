using UnityEngine;

public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        MIC_Division cell = other.GetComponent<MIC_Division>();

        if (cell != null)
        {
            cell.DestroyCell();
        }
    }
}
