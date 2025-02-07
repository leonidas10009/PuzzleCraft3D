using System;
using System.Collections.Generic;
using LibTessDotNet;
using PuzzleModules;
using UnityEngine;


public class PuzzleMeshGenerator : MonoBehaviour
{

    public float pieceSize = 1f;        // Tamaño base de la pieza
    public float tabSize = 0.4f;         // Tamaño de las lengüetas y huecos
    public int bezierSegments = 6;        // Número de segmentos para suavizar las curvas Bézier
    public int numEdges = 10;             // Número de bordes (por ejemplo, 4 para un cuadrado)
    public int splineResolution = 8;     // Resolución de la Spline (número de puntos por curva)

    private MeshFilter meshFilter;

    void Start()
    {
        // Asegurarse de que haya un MeshFilter en el objeto
        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        // Asegurarse de que haya un MeshRenderer para visualizar el Mesh
        if (gameObject.GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }

        GeneratePuzzlePiece(); // Generar una pieza de ejemplo
    }

    void GeneratePuzzlePiece()
    {
        // Generar el contorno de la pieza utilizando Splines
        List<Vector3> splinePoints = BezierUtility.GenerateSplineOutline(numEdges,pieceSize);

        // Generar la malla a partir del contorno usando LibTessDotNet
        Mesh mesh = GenerateMeshFromSpline(splinePoints);

        // Asignar la malla generada al MeshFilter
        meshFilter.mesh = mesh;
    }

    Mesh GenerateMeshFromSpline(List<Vector3> splinePoints)
    {
        Mesh mesh = new Mesh();

        // Convertir los puntos de la Spline a ContourVertex[]
        ContourVertex[] contour = new ContourVertex[splinePoints.Count];
        for (int i = 0; i < splinePoints.Count; i++)
        {
            contour[i] = new ContourVertex
            {
                Position = new Vec3(splinePoints[i].x, splinePoints[i].y, splinePoints[i].z)
            };
        }

        // Usar LibTessDotNet para tessellación
        Tess tess = new Tess();
        tess.AddContour(contour); // Pasar el contorno como ContourVertex[]

        // Realizar la tessellación
        tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

        // Crear los vértices y triángulos de la malla
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        foreach (var vertex in tess.Vertices)
        {
            vertices.Add(new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
        }

        // Cambiar la forma de iterar sobre los elementos
        for (int i = 0; i < tess.Elements.Length; i += 3)
        {
            triangles.Add(tess.Elements[i]);
            triangles.Add(tess.Elements[i + 1]);
            triangles.Add(tess.Elements[i + 2]);
        }

        // Asignar los datos a la malla
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // Generar UVs normalizados
        mesh.uv = MeshGenerator.GenerateUVs(vertices.ToArray());

        // Recalcular normales
        mesh.RecalculateNormals();

        return mesh;
    }

  
}