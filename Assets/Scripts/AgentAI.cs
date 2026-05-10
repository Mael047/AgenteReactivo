using UnityEngine;
using System.Collections.Generic;

public class AgentAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float detectionRadius = 5f;
    public LayerMask trashLayer;
    UIManager uiManager;

    float perimeterAccum = 0f;
    Vector2 lastPos;
    List<Vector2> visitedPoints = new List<Vector2>();

    Rigidbody2D rb;
    GameObject target;
    Vector2 moveDirection;
    float circlingTimer = 0f;

    int startPointIndex; // índice real donde empezó el booorde
    bool loopCheckEnabled = false;

    // Vértices del contorno del collider
    List<Vector2> boundaryPoints = new List<Vector2>();
    int currentPointIndex = 0;

    enum State { Exploring, Approaching, Circling }
    State currentState = State.Exploring;

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        lastPos = transform.position;
        rb = GetComponent<Rigidbody2D>();
        ChooseRandomDirection();
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Exploring:
                Explore();
                DetectTrash();
                break;
            case State.Approaching:
                ApproachTrash();
                break;
            case State.Circling:
                CircleTrash();
                break;
        }
    }

    void Explore()
    {
        rb.linearVelocity = moveDirection * moveSpeed;
        uiManager.ActualizarHUD("Explorando", 0f, 0f);
    }

    void ChooseRandomDirection()
    {
        moveDirection = Random.insideUnitCircle.normalized;
        Invoke(nameof(ChooseRandomDirection), 3f);
    }

    void DetectTrash()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Basura"))
            {
                target = hit.gameObject;
                currentState = State.Approaching;
                CancelInvoke();
                break;
            }
        }
    }

    void ApproachTrash()
    {
        if (target == null) { GoExplore(); return; }

        Collider2D col = target.GetComponent<Collider2D>();
        Vector2 closest = col.ClosestPoint(transform.position);
        Vector2 dir = (closest - (Vector2)transform.position);
        float dist = dir.magnitude;

        rb.linearVelocity = dir.normalized * moveSpeed;

        if (dist <= 0.1f)
        {
            if (BuildBoundary())
            {
                currentState = State.Circling;
            }
            else
            {
                GoExplore();
            }
        }
    }

    bool BuildBoundary()
    {
        boundaryPoints.Clear();

        PolygonCollider2D poly = target.GetComponent<PolygonCollider2D>();

        if (poly == null)
        {
            Debug.LogError("No hay PolygonCollider2D en la basura");
            return false;
        }

        Debug.Log($"Paths en el collider: {poly.pathCount}");

        for (int p = 0; p < poly.pathCount; p++)
        {
            Vector2[] path = poly.GetPath(p);
            Debug.Log($"Path {p}: {path.Length} puntos");

            foreach (Vector2 v in path)
            {
                boundaryPoints.Add((Vector2)poly.transform.TransformPoint(v));
            }
        }

        if (boundaryPoints.Count == 0)
        {
            Debug.LogError("boundaryPoints sigue vacío después de leer paths");
            return false;
        }

        Debug.Log($"Total puntos del borde: {boundaryPoints.Count}");

        float minDist = float.MaxValue;
        for (int i = 0; i < boundaryPoints.Count; i++)
        {
            float d = Vector2.Distance(transform.position, boundaryPoints[i]);
            if (d < minDist)
            {
                minDist = d;
                currentPointIndex = i;
            }
        }

        startPointIndex = currentPointIndex;
        loopCheckEnabled = false;
        perimeterAccum = 0f;
        visitedPoints.Clear();
        lastPos = transform.position;

        return true;
    }

    float CalcularArea()
    {
        float area = 0f;
        int n = visitedPoints.Count;
        if (n < 3) return 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = visitedPoints[i];
            Vector2 b = visitedPoints[(i + 1) % n];
            area += (a.x * b.y) - (b.x * a.y);
        }
        return Mathf.Abs(area) / 2f;
    }

    void ReportAndDestroy()
    {
        float area = CalcularArea();
        uiManager.MostrarResumen(perimeterAccum, area);
        uiManager.ActualizarHUD("Explorando", 0f, 0f);

        Destroy(target);
        perimeterAccum = 0f;
        visitedPoints.Clear();
        circlingTimer = 0f;
        GoExplore();
    }



    void CircleTrash()
    {
        if (target == null) { GoExplore(); return; }

        Vector2 pos = transform.position;
        Vector2 goal = boundaryPoints[currentPointIndex];
        float distToGoal = Vector2.Distance(pos, goal);

        if (distToGoal < 0.15f)
        {
            currentPointIndex = (currentPointIndex + 1) % boundaryPoints.Count;

            int verticesRecorridos = (currentPointIndex - startPointIndex + boundaryPoints.Count)
                                      % boundaryPoints.Count;

            if (!loopCheckEnabled && verticesRecorridos > boundaryPoints.Count / 4)
                loopCheckEnabled = true;

            if (loopCheckEnabled && currentPointIndex == startPointIndex)
            {
                ReportAndDestroy();
                return;
            }

            goal = boundaryPoints[currentPointIndex];
        }

        perimeterAccum += Vector2.Distance(pos, lastPos);
        lastPos = pos;
        visitedPoints.Add(pos);

        float areaActual = CalcularArea();
        uiManager.ActualizarHUD("Bordeando", perimeterAccum, areaActual);

        Vector2 dir = (goal - pos).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }


    void GoExplore()
    {
        target = null;
        boundaryPoints.Clear();
        currentState = State.Exploring;
        ChooseRandomDirection();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (currentState == State.Exploring || currentState == State.Approaching)
            {
                Vector2 normal = collision.contacts[0].normal;
                moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
                rb.linearVelocity = moveDirection * moveSpeed;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (boundaryPoints != null && boundaryPoints.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < boundaryPoints.Count; i++)
                Gizmos.DrawLine(boundaryPoints[i], boundaryPoints[(i + 1) % boundaryPoints.Count]);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(boundaryPoints[currentPointIndex], 0.05f);
        }
    }
}