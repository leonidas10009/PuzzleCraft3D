using UnityEngine;
using System.Collections.Generic;

public class PuzzleFaceTester : MonoBehaviour
{
    public GameObject modelToDivide;
    public int numberOfPieces = 16;
    public float minPieceAreaThreshold = 0.05f; // Umbral mínimo de área (5% del área total)

    void Start()
    {
        if (modelToDivide == null)
        {
            Debug.LogError("No se ha asignado un modelo 3D.");
            return;
        }

        Mesh mesh = modelToDivide.GetComponent<MeshFilter>().mesh;

        // Preprocesamiento: calcular centroides y áreas de triángulos
        List<Vector3> triangleCenters;
        List<float> triangleAreas;
        float totalMeshArea = PreprocessMesh(mesh, out triangleCenters, out triangleAreas);

        // Segmentar la malla con K-Means mejorado
        List<List<int>> pieces = SegmentMeshWithUniformSize(mesh, triangleCenters, triangleAreas, numberOfPieces, totalMeshArea * minPieceAreaThreshold);

        // Crear piezas del rompecabezas
        for (int i = 0; i < pieces.Count; i++)
        {
            CreatePuzzlePiece(mesh, pieces[i], i);
        }
    }

    float PreprocessMesh(Mesh mesh, out List<Vector3> triangleCenters, out List<float> triangleAreas)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        triangleCenters = new List<Vector3>();
        triangleAreas = new List<float>();

        float totalArea = 0;

        for (int i = 0; i < triangles.Length / 3; i++)
        {
            // Calcular el centroide del triángulo
            Vector3 center = (vertices[triangles[i * 3]] +
                              vertices[triangles[i * 3 + 1]] +
                              vertices[triangles[i * 3 + 2]]) / 3;
            triangleCenters.Add(center);

            // Calcular el área del triángulo
            Vector3 v0 = vertices[triangles[i * 3]];
            Vector3 v1 = vertices[triangles[i * 3 + 1]];
            Vector3 v2 = vertices[triangles[i * 3 + 2]];
            float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude / 2;
            triangleAreas.Add(area);

            totalArea += area;
        }

        return totalArea;
    }

    List<List<int>> SegmentMeshWithUniformSize(Mesh mesh, List<Vector3> triangleCenters, List<float> triangleAreas, int numberOfPieces, float minPieceArea)
    {
        int numberOfTriangles = triangleCenters.Count;

        // Inicialización de centroides usando muestreo ponderado por área
        List<Vector3> centroids = new List<Vector3>();
        for (int i = 0; i < numberOfPieces; i++)
        {
            int randomIndex = WeightedRandomIndex(triangleAreas);
            centroids.Add(triangleCenters[randomIndex]);
        }

        // K-Means mejorado
        List<List<int>> pieces = new List<List<int>>();
        for (int i = 0; i < numberOfPieces; i++)
        {
            pieces.Add(new List<int>());
        }

        for (int iteration = 0; iteration < 10; iteration++) // Número de iteraciones
        {
            // a. Asignación de triángulos a la pieza más cercana
            foreach (List<int> piece in pieces)
            {
                piece.Clear();
            }

            for (int i = 0; i < numberOfTriangles; i++)
            {
                Vector3 triangleCenter = triangleCenters[i];
                int closestCentroidIndex = 0;
                float minDistance = Vector3.Distance(triangleCenter, centroids[0]);

                for (int j = 1; j < numberOfPieces; j++)
                {
                    // Penalización por tamaño de la pieza
                    float currentPieceArea = CalculatePieceArea(pieces[j], triangleAreas);
                    float adjustedDistance = Vector3.Distance(triangleCenter, centroids[j]) + 0.1f * currentPieceArea;

                    if (adjustedDistance < minDistance)
                    {
                        minDistance = adjustedDistance;
                        closestCentroidIndex = j;
                    }
                }

                pieces[closestCentroidIndex].Add(i);
            }

            // b. Recálculo de centroides
            for (int i = 0; i < numberOfPieces; i++)
            {
                if (pieces[i].Count > 0)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (int triangleIndex in pieces[i])
                    {
                        sum += triangleCenters[triangleIndex];
                    }
                    centroids[i] = sum / pieces[i].Count;
                }
            }
        }

        // c. Redistribución de piezas pequeñas
        for (int i = 0; i < pieces.Count; i++)
        {
            float pieceArea = CalculatePieceArea(pieces[i], triangleAreas);
            if (pieceArea < minPieceArea)
            {
                RedistributeSmallPiece(pieces, i, triangleAreas);
            }
        }

        return pieces;
    }

    float CalculatePieceArea(List<int> pieceTriangles, List<float> triangleAreas)
    {
        float totalArea = 0;
        foreach (int triangleIndex in pieceTriangles)
        {
            totalArea += triangleAreas[triangleIndex];
        }
        return totalArea;
    }

    void RedistributeSmallPiece(List<List<int>> pieces, int smallPieceIndex, List<float> triangleAreas)
    {
        List<int> smallPiece = pieces[smallPieceIndex];
        pieces[smallPieceIndex] = new List<int>(); // Vaciar la pieza pequeña

        // Encontrar la pieza más grande para reasignar los triángulos
        int bestPieceIndex = -1;
        float maxArea = 0;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (i != smallPieceIndex) // No considerar la pieza pequeña
            {
                float pieceArea = CalculatePieceArea(pieces[i], triangleAreas);
                if (pieceArea > maxArea)
                {
                    maxArea = pieceArea;
                    bestPieceIndex = i;
                }
            }
        }

        // Redistribuir triángulos de la pieza pequeña a la pieza más grande
        if (bestPieceIndex != -1)
        {
            foreach (int triangleIndex in smallPiece)
            {
                pieces[bestPieceIndex].Add(triangleIndex);
            }
        }
    }

    int WeightedRandomIndex(List<float> weights)
    {
        float totalWeight = 0;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue < cumulativeWeight)
            {
                return i;
            }
        }

        return weights.Count - 1;
    }

    void CreatePuzzlePiece(Mesh originalMesh, List<int> pieceTriangles, int pieceIndex)
    {
        Mesh pieceMesh = new Mesh();

        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector2[] uvs = originalMesh.uv;

        List<Vector3> pieceVertices = new List<Vector3>();
        List<int> pieceTrianglesList = new List<int>();
        List<Vector2> pieceUVs = new List<Vector2>();

        foreach (int triangleIndex in pieceTriangles)
        {
            int vertexIndex1 = triangles[triangleIndex * 3];
            int vertexIndex2 = triangles[triangleIndex * 3 + 1];
            int vertexIndex3 = triangles[triangleIndex * 3 + 2];

            int newVertexIndex1 = AddVertex(pieceVertices, pieceUVs, vertices[vertexIndex1], uvs[vertexIndex1]);
            int newVertexIndex2 = AddVertex(pieceVertices, pieceUVs, vertices[vertexIndex2], uvs[vertexIndex2]);
            int newVertexIndex3 = AddVertex(pieceVertices, pieceUVs, vertices[vertexIndex3], uvs[vertexIndex3]);

            pieceTrianglesList.Add(newVertexIndex1);
            pieceTrianglesList.Add(newVertexIndex2);
            pieceTrianglesList.Add(newVertexIndex3);
        }

        pieceMesh.vertices = pieceVertices.ToArray();
        pieceMesh.triangles = pieceTrianglesList.ToArray();
        pieceMesh.uv = pieceUVs.ToArray();

        GameObject pieceObject = new GameObject("PuzzlePiece_" + pieceIndex);
        pieceObject.AddComponent<MeshFilter>().mesh = pieceMesh;
        pieceObject.AddComponent<MeshRenderer>().material = modelToDivide.GetComponent<MeshRenderer>().material;
    }

    int AddVertex(List<Vector3> vertices, List<Vector2> uvs, Vector3 vertex, Vector2 uv)
    {
        int index = vertices.IndexOf(vertex);
        if (index == -1)
        {
            vertices.Add(vertex);
            uvs.Add(uv);
            index = vertices.Count - 1;
        }
        return index;
    }
}