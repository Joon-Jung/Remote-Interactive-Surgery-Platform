using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineHelper : MonoBehaviour
{
    public GameObject linePrefab;
    public float lineWidth = 0.002f;
    private List<GameObject> lines;
    private int lineNumber;
    // Start is called before the first frame update
    void Start()
    {
        if (linePrefab == null)
        {
            Debug.LogError("LineHelper - no line prefab assigned.");
        }
        lineNumber = 0;
    }
    public void AddNewLine(Vector3[] points, Color lineColor)
    {
        GameObject lineInstance = Instantiate(linePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        lineInstance.name = "Line_" + lineNumber.ToString();
        lineNumber++;
        LineRenderer lineRenderer = lineInstance.GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        int numOfPoints = points.Length;
        lineRenderer.positionCount = numOfPoints;
        lineRenderer.SetPositions(points);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }
    public void ClearLines()
    {
        Debug.Log("Clear line triggered.");
        for(int i =0; i <= lineNumber; i++)
        {
            string targetName = "Line_" + i.ToString();
            GameObject targetObject = GameObject.Find(targetName);
            DestroyImmediate(targetObject);
        }
        lineNumber = 0;
    }
}
