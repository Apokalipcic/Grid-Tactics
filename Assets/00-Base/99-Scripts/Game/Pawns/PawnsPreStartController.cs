using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PawnsPreStartController : MonoBehaviour
{
    private void Start()
    {
        for (int i = 0; i < this.transform.childCount; i++)
        {
            PawnMovement pawn = this.transform.GetChild(i).GetComponent<PawnMovement>();

            GameManager.Instance.AddPawn(pawn, pawn.tag);
            pawn.gameObject.SetActive(false);
        }
    }
}
