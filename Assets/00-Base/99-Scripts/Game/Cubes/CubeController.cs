using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] ParticleSystem cubeHighlightVFX;

    public bool isWalkable = true;

    public void ChangeHighlightVFX(bool state)
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

}
