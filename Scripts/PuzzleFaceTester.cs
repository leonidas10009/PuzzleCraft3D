using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Puzzle3DGenerator : MonoBehaviour
{
    [Header("Configuración General")]
    public GameObject modelToDivide; // Modelo 3D que se dividirá en piezas
    public int numberOfPieces = 20; // Número de piezas del rompecabezas
    public float pieceDepth = 0.5f; // Profundidad de las piezas del rompecabezas
    public Material puzzleMaterial; // Material para las piezas del rompecabezas

    void Start()
    {
        if (modelToDivide == null)
        {
            Debug.LogError("Por favor, asigna un modelo 3D para dividir.");
            return;
        }

        // Obtener la malla del modelo 3D
        Mesh originalMesh = modelToDivide.GetComponent<MeshFilter>().mesh;
        if (originalMesh == null)
        {
            Debug.LogError("El modelo 3D no tiene una malla asignada.");
            return;
        }


        // Dividir la malla en regiones basadas en la topología
        List<List<int>> segmentedRegions = SegmentMesh(originalMesh, numberOfPieces);

        // Crear las piezas del rompecabezas
        foreach (List<int> region in segmentedRegions)
        {
            CreatePuzzlePiece(originalMesh, region, pieceDepth);
        }
        
    }

    /// <summary>
    /// Divide la malla en regiones basadas en la topología y el número de piezas especificado.
    /// </summary>
    /// <param name="mesh">La malla original del modelo.</param>
    /// <param name="numberOfPieces">El número de piezas en las que se dividirá la malla.</param>
    /// <returns>Una lista de regiones, donde cada región es una lista de índices de triángulos.</returns>
    List<List<int>> SegmentMesh(Mesh mesh, int numberOfPieces)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Calcular los centroides y las normales de los triángulos
        Vector3[] triangleCentroids = new Vector3[triangles.Length / 3];
        Vector3[] triangleNormals = new Vector3[triangles.Length / 3];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            // Calcular el centroide del triángulo
            triangleCentroids[i / 3] = (v0 + v1 + v2) / 3f;

            // Calcular la normal del triángulo
            triangleNormals[i / 3] = Vector3.Cross(v1 - v0, v2 - v0).normalized;
        }

        // Inicializar las regiones
        List<List<int>> regions = new List<List<int>>();
        for (int i = 0; i < numberOfPieces; i++)
        {
            regions.Add(new List<int>());
        }

        // Asignar triángulos a las regiones usando K-Means
        int[] clusterAssignments = KMeans(triangleCentroids, triangleNormals, numberOfPieces);

        // Asegurar que cada región tenga al menos un triángulo
        for (int i = 0; i < clusterAssignments.Length; i++)
        {
            int clusterIndex = clusterAssignments[i];
            regions[clusterIndex].Add(i * 3); // Agregar el índice del primer vértice del triángulo
        }

        // Combinar regiones vacías con las más cercanas
        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].Count == 0)
            {
                // Encontrar la región más cercana
                int closestRegion = FindClosestRegion(i, regions, triangleCentroids);
                if (closestRegion != -1)
                {
                    regions[closestRegion].AddRange(regions[i]);
                    regions.RemoveAt(i);
                    i--; // Ajustar el índice después de eliminar
                }
            }
        }

        return regions;
    }
    /// <summary>
    /// Encuentra la región más cercana a una región vacía.
    /// </summary>
    int FindClosestRegion(int emptyRegionIndex, List<List<int>> regions, Vector3[] centroids)
    {
        float minDistance = float.MaxValue;
        int closestRegion = -1;

        for (int i = 0; i < regions.Count; i++)
        {
            if (i == emptyRegionIndex || regions[i].Count == 0) continue;

            // Calcular la distancia entre los centroides de las regiones
            Vector3 emptyCentroid = CalculateRegionCentroid(emptyRegionIndex, regions, centroids);
            Vector3 regionCentroid = CalculateRegionCentroid(i, regions, centroids);
            float distance = Vector3.Distance(emptyCentroid, regionCentroid);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestRegion = i;
            }
        }

        return closestRegion;
    }
    /// <summary>
    /// Calcula el centroide de una región.
    /// </summary>
    Vector3 CalculateRegionCentroid(int regionIndex, List<List<int>> regions, Vector3[] centroids)
    {
        Vector3 centroid = Vector3.zero;
        foreach (int triangleIndex in regions[regionIndex])
        {
            centroid += centroids[triangleIndex / 3];
        }
        return centroid / regions[regionIndex].Count;
    }

    /// <summary>
    /// Implementación del algoritmo K-Means para agrupar puntos en un número específico de clústeres.
    /// </summary>
    int[] KMeans(Vector3[] centroids, Vector3[] normals, int numberOfClusters)
    {
        Vector3[] clusterCentroids = new Vector3[numberOfClusters];
        int[] assignments = new int[centroids.Length];
        bool hasChanged;

        // Inicializar los centroides aleatoriamente
        for (int i = 0; i < numberOfClusters; i++)
        {
            clusterCentroids[i] = centroids[Random.Range(0, centroids.Length)];
        }

        do
        {
            hasChanged = false;

            // Asignar cada punto al clúster más cercano
            for (int i = 0; i < centroids.Length; i++)
            {
                float minDistance = float.MaxValue;
                int closestCluster = 0;

                for (int j = 0; j < numberOfClusters; j++)
                {
                    // Considerar tanto la distancia espacial como la diferencia en las normales
                    float distance = Vector3.Distance(centroids[i], clusterCentroids[j]);
                    float normalDifference = Vector3.Angle(normals[i], clusterCentroids[j]) / 180f; // Normalizada
                    float combinedMetric = distance + normalDifference;

                    if (combinedMetric < minDistance)
                    {
                        minDistance = combinedMetric;
                        closestCluster = j;
                    }
                }

                if (assignments[i] != closestCluster)
                {
                    assignments[i] = closestCluster;
                    hasChanged = true;
                }
            }

            // Recalcular los centroides
            for (int i = 0; i < numberOfClusters; i++)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;

                for (int j = 0; j < centroids.Length; j++)
                {
                    if (assignments[j] == i)
                    {
                        sum += centroids[j];
                        count++;
                    }
                }

                if (count > 0)
                {
                    clusterCentroids[i] = sum / count;
                }
            }
        } while (hasChanged);

        return assignments;
    }

    /// <summary>
    /// Crea una pieza del rompecabezas basada en una región de triángulos.
    /// </summary>
    void CreatePuzzlePiece(Mesh mesh, List<int> region, float depth)
    {
        // Crear un nuevo GameObject para la pieza del rompecabezas
        GameObject pieceObj = new GameObject("PuzzlePiece");
        pieceObj.transform.parent = transform;
        pieceObj.transform.position = Vector3.zero;

        // Crear una nueva malla para la pieza
        Mesh pieceMesh = new Mesh();

        // Extraer los vértices, triángulos y coordenadas UV de la región
        List<Vector3> pieceVertices = new List<Vector3>();
        List<int> pieceTriangles = new List<int>();
        List<Vector2> pieceUVs = new List<Vector2>();
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        for (int i = 0; i < region.Count; i++)
        {
            int triangleIndex = region[i];
            for (int j = 0; j < 3; j++)
            {
                int vertexIndex = mesh.triangles[triangleIndex + j];

                // Mapear vértices únicos
                if (!vertexMap.ContainsKey(vertexIndex))
                {
                    vertexMap[vertexIndex] = pieceVertices.Count;
                    pieceVertices.Add(mesh.vertices[vertexIndex]);
                    pieceUVs.Add(mesh.uv[vertexIndex]); // Copiar las coordenadas UV
                }

                pieceTriangles.Add(vertexMap[vertexIndex]);
            }
        }

        // Crear la malla de la pieza
        pieceMesh.vertices = pieceVertices.ToArray();
        pieceMesh.triangles = pieceTriangles.ToArray();
        pieceMesh.uv = pieceUVs.ToArray(); // Asignar las coordenadas UV
        pieceMesh.RecalculateNormals();

        // Asignar la malla al GameObject
        MeshFilter mf = pieceObj.AddComponent<MeshFilter>();
        MeshRenderer mr = pieceObj.AddComponent<MeshRenderer>();
        mf.mesh = pieceMesh;

        // Asignar el material original
        mr.material = puzzleMaterial;
    }
}