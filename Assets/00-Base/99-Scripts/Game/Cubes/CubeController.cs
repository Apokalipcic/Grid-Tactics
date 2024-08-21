using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class CubeController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] ParticleSystem cubeHighlightVFX;

    [Header("Occupation")]
    [SerializeField] private GameObject occupant;

    [Header("Debug")]
    [SerializeField] bool activateThisObject = true;

    public bool isWalkable = true;

    private ParticleSystem.MainModule mainModule;

    #region Activation

    private void OnValidate()
    {
        activateThisObject = this.gameObject.activeSelf;
    }
    public void ActivateThisCube()
    {
        if (!activateThisObject)
            return;

        mainModule = cubeHighlightVFX.main;

        this.gameObject.SetActive(true);
        this.transform.GetComponentInParent<GridController>().AddNewCell(this.transform);
    }
    #endregion

    #region Occupation Methods
    public void OnOccupy(GameObject obj)
    {
        if (occupant != null && occupant != obj)
        {
            Debug.LogWarning($"In Cube Controller I removed something check this out.");
            //// Handle case where a new object is pushing out the current occupant
            //IPushable currentOccupant = occupant.GetComponent<IPushable>();
            //if (currentOccupant != null)
            //{
            //    Vector3 pushDirection = (occupant.transform.position - obj.transform.position).normalized;
            //    currentOccupant.Push(pushDirection);
            //}
        }

        occupant = obj;
        isWalkable = false;
    }

    public void OnDeoccupy()
    {
        occupant = null;
        isWalkable = true;
    }

    public bool IsOccupied()
    {
        return occupant != null;
    }

    public GameObject GetOccupant()
    {
        return occupant;
    }
    #endregion

    #region VFX 
    public void ChangeHighlightVFX(bool state, Color? color = null)
    {
        if (state)
        {
            if (color.HasValue)
            {
                mainModule.startColor = color.Value;
            }
            cubeHighlightVFX.Play();
        }
        else
        {
            cubeHighlightVFX.Stop();
        }
    }

    public void ChangeHighlightEnemyVFX(bool state)
    {
        if (state)
        {
            cubeHighlightVFX.Play();
        }
        else
        {
            cubeHighlightVFX.Stop();
        }
    }
    #endregion
}
