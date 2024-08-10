using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrailMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float arrivalDistance = 0.1f;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private float offsetY = 1;

    private List<Vector3> path = new List<Vector3>();
    private int currentPathIndex = 0;
    private bool isMoving = false;

    private void Awake()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
    }

    public void StartTrail(Vector3 startPosition)
    {
        startPosition.y = offsetY;

        transform.position = startPosition;
        path.Clear();
        path.Add(startPosition);
        currentPathIndex = 0;
        trailRenderer.Clear();
        isMoving = false;
    }

    public void AddPointToPath(Vector3 point)
    {
        point.y = offsetY;

        path.Add(point);
        if (!isMoving)
        {
            isMoving = true;
            StartCoroutine(MoveAlongPath());
        }
    }

    private IEnumerator MoveAlongPath()
    {
        while (isMoving && currentPathIndex < path.Count - 1)
        {
            Vector3 targetPosition = path[currentPathIndex + 1];

            while (Vector3.Distance(transform.position, targetPosition) > arrivalDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPosition;
            currentPathIndex++;

            if (currentPathIndex >= path.Count - 1)
            {
                isMoving = false;
            }
        }
    }

    public void ClearTrail()
    {
        StopAllCoroutines();
        path.Clear();
        currentPathIndex = 0;
        isMoving = false;
        trailRenderer.Clear();
    }
}