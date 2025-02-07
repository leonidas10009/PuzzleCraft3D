using System;
using System.Collections.Generic;
using System.Linq;
using LibTessDotNet;
using UnityEngine;
using PuzzleModules; // Se asume que aquí se encuentran CustomMeshOptimizer y ConvexHull3DCalculator

public class PuzzlePieceCreator : MonoBehaviour
{
    [Header("Parámetros de la pieza")]
    public float pieceSize = 1f;        // Tamaño base de la pieza
    public float tabSize = 0.4f;        // Tamaño de las lengüetas y huecos
    public int splineResolution = 8;    // Resolución de la curva Bézier
    public int numEdges = 4;            // Número de bordes (4 para una pieza cuadrada)

    private MeshFilter meshFilter;

    void Awake()
    {
        // Intenta obtener el MeshFilter; si no existe, lo añade
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        // Comprueba también el MeshRenderer
        if (GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }
    }


    void Start()
    {
        GeneratePuzzlePiece();
    }

    /// <summary>
    /// Genera la pieza de puzzle manteniendo la forma exterior (definida por el spline)
    /// y optimizando la segmentación interna (reduciendo la cantidad de caras).
    /// </summary>
    public void GeneratePuzzlePiece()
    {
        // 1. Generar el contorno exterior de la pieza mediante splines.
        List<Vector3> exteriorPoints = GenerateSplineOutline();

        // 2. Generar puntos interiores (por ejemplo, el centro de la pieza).
        List<Vector3> interiorPoints = GenerateInteriorPoints(exteriorPoints);

        // 3. Optimizar los puntos interiores sin modificar el contorno exterior.
        //    Se supone que CustomMeshOptimizer.OptimizeMesh respeta los puntos exteriores.
        List<Vector3> optimizedInteriorPoints = CustomMeshOptimizer.OptimizeMesh(exteriorPoints, interiorPoints);

        // 4. Para la triangulación usaremos el contorno exterior (invariable)
        //    y los puntos interiores optimizados se agregarán como steiner points.
        List<Vector3> tessellationPoints = new List<Vector3>(exteriorPoints);
        tessellationPoints.AddRange(optimizedInteriorPoints);

        // 5. Generar la malla inicial usando LibTessDotNet.
        Mesh initialMesh = GenerateMeshFromPoints(tessellationPoints, exteriorPoints);

        // 6. Optimizar la malla interna fusionando caras coplanares (sin alterar el contorno).
        Mesh optimizedMesh = OptimizeMeshFaces(initialMesh);

        // 7. Asignar la malla final al MeshFilter.
        meshFilter.mesh = optimizedMesh;
    }

    #region Generación del Contorno e Interiores

    /// <summary>
    /// Genera el contorno (exterior) de la pieza utilizando curvas Bézier sobre cada borde.
    /// Los puntos se generan en orden para definir la frontera.
    /// </summary>
    private List<Vector3> GenerateSplineOutline()
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < numEdges; i++)
        {
            Vector3 start = GetVertexPosition(i);
            Vector3 end = GetVertexPosition((i + 1) % numEdges);
            bool isTab = i % 2 == 0; // Alterna entre lengüeta (saliente) y hueco (entrante)

            // Generar la curva Bézier para el borde y añadir sus puntos.
            points.AddRange(GenerateSplineEdge(start, end, isTab));
        }
        return points;
    }

    /// <summary>
    /// Calcula la posición de un vértice usando coordenadas polares.
    /// </summary>
    private Vector3 GetVertexPosition(int index)
    {
        float angle = index * Mathf.PI * 2 / numEdges;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * pieceSize;
    }

    /// <summary>
    /// Genera una curva Bézier entre dos puntos, desplazando el punto medio según si es lengüeta o hueco.
    /// </summary>
    private List<Vector3> GenerateSplineEdge(Vector3 start, Vector3 end, bool isTab)
    {
        List<Vector3> controlPoints = new List<Vector3>();

        Vector3 midPoint = (start + end) / 2;
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);

        Vector3 tabPoint = midPoint + perpendicular * (isTab ? tabSize : -tabSize);

        controlPoints.Add(start);
        controlPoints.Add(tabPoint);
        controlPoints.Add(end);

        // Se utiliza una utilidad para generar la curva Bézier con la resolución especificada.
        return BezierUtility.GenerateBezierCurve(controlPoints, splineResolution);
    }

    /// <summary>
    /// Genera los puntos interiores. En este ejemplo, se utiliza el centroide del contorno.
    /// </summary>
    private List<Vector3> GenerateInteriorPoints(List<Vector3> exteriorPoints)
    {
        List<Vector3> interiorPoints = new List<Vector3>();
        Vector3 centerPoint = Vector3.zero;
        foreach (var point in exteriorPoints)
            centerPoint += point;
        centerPoint /= exteriorPoints.Count;
        interiorPoints.Add(centerPoint);
        return interiorPoints;
    }

    #endregion

    #region Generación de la Malla con LibTessDotNet

    /// <summary>
    /// Genera una malla triangulada utilizando LibTessDotNet.
    /// Se usa el contorno exterior para definir la región y se agregan
    /// puntos interiores (optimizedPoints) como steiner points.
    /// </summary>
    private Mesh GenerateMeshFromPoints(List<Vector3> allPoints, List<Vector3> exteriorPoints)
    {
        Tess tess = new Tess();

        // Se asume que 'exteriorPoints' están en el orden correcto (formando el contorno).
        ContourVertex[] contour = exteriorPoints
            .Select(p => new ContourVertex
            {
                Position = new Vec3 { X = p.x, Y = p.y, Z = p.z }
            })
            .ToArray();
        tess.AddContour(contour, ContourOrientation.Original);

        // Agregar los puntos interiores (los que no estén en el contorno) como steiner points.
        foreach (Vector3 p in allPoints)
        {
            if (!IsPointOnContour(p, exteriorPoints))
            {
                // Se agrega cada punto interior como un contorno degenerate.
                ContourVertex[] innerContour = new ContourVertex[1];
                innerContour[0] = new ContourVertex
                {
                    Position = new Vec3 { X = p.x, Y = p.y, Z = p.z }
                };
                tess.AddContour(innerContour, ContourOrientation.Original);
            }
        }

        tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

        Mesh mesh = new Mesh();
        Vector3[] vertices = tess.Vertices
            .Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z))
            .ToArray();
        int[] triangles = tess.Elements;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = MeshGenerator.GenerateUVs(vertices);
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// Verifica si un punto pertenece al contorno, usando una tolerancia.
    /// </summary>
    private bool IsPointOnContour(Vector3 point, List<Vector3> contour)
    {
        const float tol = 1e-4f;
        foreach (var p in contour)
        {
            if (Vector3.Distance(point, p) < tol)
                return true;
        }
        return false;
    }

  
    #endregion

    #region Optimización de la Malla Interna (Fusión de Caras Coplanares)

    /// <summary>
    /// Optimiza la malla interna fusionando triángulos coplanares.
    /// Para ello:
    ///   1. Se extraen las caras (usando la estructura Face de ConvexHullCalculator).
    ///   2. Se agrupan aquellas con normales similares mediante ConvexHull3DCalculator.OptimizeFaces.
    ///   3. Se re-triangulan los polígonos resultantes para obtener una malla con menos caras.
    /// </summary>
    private Mesh OptimizeMeshFaces(Mesh mesh)
    {
        // 1. Convertir los triángulos del mesh en una lista de caras de tipo ConvexHullCalculator.Face.
        List<ConvexHullCalculator.Face> faces = new List<ConvexHullCalculator.Face>();
        Vector3[] vertices = mesh.vertices;
        int[] tris = mesh.triangles;
        for (int i = 0; i < tris.Length; i += 3)
        {
            faces.Add(new ConvexHullCalculator.Face(vertices[tris[i]], vertices[tris[i + 1]], vertices[tris[i + 2]]));
        }

        // 2. Convertir las caras al tipo requerido por OptimizeFaces.
        List<ConvexHull3DCalculator.Face> convertedFaces = ConvertFaces(faces);

        // 3. Agrupar y fusionar caras coplanares (se asume que OptimizeFaces está implementado en ConvexHull3DCalculator)
        List<List<Vector3>> optimizedPolygons = ConvexHull3DCalculator.OptimizeFaces(convertedFaces);

        // 4. Triangular cada polígono optimizado usando LibTessDotNet.
        List<Vector3> finalVertices = new List<Vector3>();
        List<int> finalTriangles = new List<int>();

        foreach (List<Vector3> polygon in optimizedPolygons)
        {
            Tess tess = new Tess();
            ContourVertex[] contour = polygon
                .Select(p => new ContourVertex { Position = new Vec3 { X = p.x, Y = p.y, Z = p.z } })
                .ToArray();
            tess.AddContour(contour, ContourOrientation.Original);
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            int baseIndex = finalVertices.Count;
            finalVertices.AddRange(tess.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)));
            foreach (int index in tess.Elements)
            {
                finalTriangles.Add(baseIndex + index);
            }
        }

        Mesh optimizedMesh = new Mesh();
        optimizedMesh.vertices = finalVertices.ToArray();
        optimizedMesh.triangles = finalTriangles.ToArray();
        optimizedMesh.uv = MeshGenerator.GenerateUVs(optimizedMesh.vertices);
        optimizedMesh.RecalculateNormals();
        return optimizedMesh;
    }
    private List<ConvexHull3DCalculator.Face> ConvertFaces(List<ConvexHullCalculator.Face> faces)
    {
        List<ConvexHull3DCalculator.Face> converted = new List<ConvexHull3DCalculator.Face>();
        foreach (var face in faces)
        {
            // Se asume que ConvexHull3DCalculator.Face tiene un constructor que recibe tres Vector3
            converted.Add(new ConvexHull3DCalculator.Face(face.A, face.B, face.C));
        }
        return converted;
    }


    #endregion
}
