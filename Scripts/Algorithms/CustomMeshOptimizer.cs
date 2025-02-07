using System.Collections.Generic;
using UnityEngine;

namespace PuzzleModules
{
    public class CustomMeshOptimizer
    {
        /// <summary>
        /// Optimiza los puntos interiores respetando los puntos exteriores.
        /// </summary>
        public static List<Vector3> OptimizeMesh(List<Vector3> exteriorPoints, List<Vector3> interiorPoints)
        {
            // Paso 1: Mantener los puntos exteriores fijos
            List<Vector3> optimizedPoints = new List<Vector3>(exteriorPoints);

            // Paso 2: Triangular los puntos interiores
            List<Triangle> triangles = PerformDelaunayTriangulation(exteriorPoints, interiorPoints);

            // Paso 3: Optimizar la triangulación
            List<Vector3> optimizedInteriorPoints = OptimizeInteriorPoints(interiorPoints, triangles);

            // Agregar los puntos optimizados al resultado final
            optimizedPoints.AddRange(optimizedInteriorPoints);

            return optimizedPoints;
        }

        /// <summary>
        /// Realiza la triangulación de Delaunay con restricciones de bordes exteriores.
        /// </summary>
        private static List<Triangle> PerformDelaunayTriangulation(List<Vector3> exteriorPoints, List<Vector3> interiorPoints)
        {
            List<Vector3> allPoints = new List<Vector3>(exteriorPoints);
            allPoints.AddRange(interiorPoints);

            List<Triangle> triangles = new List<Triangle>();

            // Crear un supertriángulo que contenga todos los puntos
            Triangle superTriangle = CreateSuperTriangle(allPoints);
            triangles.Add(superTriangle);

            // Agregar cada punto a la triangulación
            foreach (var point in allPoints)
            {
                List<Triangle> badTriangles = new List<Triangle>();

                // Encontrar triángulos que no respetan la propiedad de Delaunay
                foreach (var triangle in triangles)
                {
                    if (IsPointInsideCircumcircle(point, triangle))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                // Crear un polígono a partir de los bordes de los triángulos "malos"
                List<Edge> polygon = new List<Edge>();
                foreach (var badTriangle in badTriangles)
                {
                    foreach (var edge in badTriangle.GetEdges())
                    {
                        if (!IsEdgeShared(edge, badTriangles))
                        {
                            polygon.Add(edge);
                        }
                    }
                }

                // Eliminar los triángulos "malos"
                triangles.RemoveAll(t => badTriangles.Contains(t));

                // Crear nuevos triángulos a partir del punto y los bordes del polígono
                foreach (var edge in polygon)
                {
                    triangles.Add(new Triangle(edge.A, edge.B, point));
                }
            }

            // Eliminar triángulos que comparten vértices con el supertriángulo
            triangles.RemoveAll(t => t.ContainsVertex(superTriangle.A) || t.ContainsVertex(superTriangle.B) || t.ContainsVertex(superTriangle.C));

            return triangles;
        }

        /// <summary>
        /// Optimiza los puntos interiores utilizando técnicas de suavizado.
        /// </summary>
        private static List<Vector3> OptimizeInteriorPoints(List<Vector3> interiorPoints, List<Triangle> triangles)
        {
            List<Vector3> optimizedPoints = new List<Vector3>();

            foreach (var point in interiorPoints)
            {
                List<Vector3> neighbors = GetNeighbors(point, triangles);

                // Calcular la nueva posición como el promedio de los vecinos
                Vector3 newPosition = Vector3.zero;
                foreach (var neighbor in neighbors)
                {
                    newPosition += neighbor;
                }
                newPosition /= neighbors.Count;

                optimizedPoints.Add(newPosition);
            }

            return optimizedPoints;
        }

        // Obtener los vecinos de un punto en la triangulación
        private static List<Vector3> GetNeighbors(Vector3 point, List<Triangle> triangles)
        {
            HashSet<Vector3> neighbors = new HashSet<Vector3>();

            foreach (var triangle in triangles)
            {
                if (triangle.ContainsVertex(point))
                {
                    foreach (var other in triangle.GetOtherVertices(point))
                    {
                        neighbors.Add(other);
                    }
                }
            }

            return new List<Vector3>(neighbors);
        }


        // Crear un supertriángulo que contenga todos los puntos
        private static Triangle CreateSuperTriangle(List<Vector3> points)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var point in points)
            {
                if (point.x < minX) minX = point.x;
                if (point.x > maxX) maxX = point.x;
                if (point.y < minY) minY = point.y;
                if (point.y > maxY) maxY = point.y;
            }

            float dx = maxX - minX;
            float dy = maxY - minY;
            float deltaMax = Mathf.Max(dx, dy) * 2;

            Vector3 p1 = new Vector3(minX - deltaMax, minY - deltaMax, 0);
            Vector3 p2 = new Vector3(maxX + deltaMax, minY - deltaMax, 0);
            Vector3 p3 = new Vector3((minX + maxX) / 2, maxY + deltaMax, 0);

            return new Triangle(p1, p2, p3);
        }



        // Verificar si un punto está dentro del circuncírculo de un triángulo
        private static bool IsPointInsideCircumcircle(Vector3 point, Triangle triangle)
        {
            // Extraer los vértices
            Vector3 A = triangle.A;
            Vector3 B = triangle.B;
            Vector3 C = triangle.C;

            // Calcular las diferencias (trabajando en 2D, usando x e y)
            float ax = A.x - point.x;
            float ay = A.y - point.y;
            float bx = B.x - point.x;
            float by = B.y - point.y;
            float cx = C.x - point.x;
            float cy = C.y - point.y;

            // Calcular los cuadrados de las distancias
            float aSq = ax * ax + ay * ay;
            float bSq = bx * bx + by * by;
            float cSq = cx * cx + cy * cy;

            // Determinante de la matriz 3x3:
            // | ax   ay   aSq |
            // | bx   by   bSq |
            // | cx   cy   cSq |
            float det = ax * (by * cSq - bSq * cy)
                      - ay * (bx * cSq - bSq * cx)
                      + aSq * (bx * cy - by * cx);

            // IMPORTANTE: El signo del determinante depende de la orientación (sentido horario o antihorario)
            // Se recomienda normalizar la orientación de los triángulos (por ejemplo, asegurarse de que tengan
            // una orientación consistente) para interpretar correctamente el resultado.
            return det > 0;
        }


        // Verificar si un borde es compartido por varios triángulos
        private static bool IsEdgeShared(Edge edge, List<Triangle> triangles)
        {
            int count = 0;
            foreach (var triangle in triangles)
            {
                if (triangle.ContainsEdge(edge))
                {
                    count++;
                }
            }
            return count > 1;
        }

        /// <summary>
        /// Representa un triángulo en 3D.
        /// </summary>
        public struct Triangle
        {
            public Vector3 A, B, C;

            public Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
            }

            public bool ContainsVertex(Vector3 vertex)
            {
                return A == vertex || B == vertex || C == vertex;
            }

            public List<Vector3> GetOtherVertices(Vector3 vertex)
            {
                List<Vector3> others = new List<Vector3>();
                if (A != vertex) others.Add(A);
                if (B != vertex) others.Add(B);
                if (C != vertex) others.Add(C);
                return others;
            }

            public List<Edge> GetEdges()
            {
                return new List<Edge>
                {
                    new Edge(A, B),
                    new Edge(B, C),
                    new Edge(C, A)
                };
            }

            public bool ContainsEdge(Edge edge)
            {
                return (A == edge.A && B == edge.B) || (B == edge.A && C == edge.B) || (C == edge.A && A == edge.B) ||
                       (A == edge.B && B == edge.A) || (B == edge.B && C == edge.A) || (C == edge.B && A == edge.A);
            }
        }

        /// <summary>
        /// Representa un borde entre dos puntos.
        /// </summary>
        public struct Edge
        {
            public Vector3 A, B;

            public Edge(Vector3 a, Vector3 b)
            {
                A = a;
                B = b;
            }

            public override bool Equals(object obj)
            {
                if (obj is Edge edge)
                {
                    return (A == edge.A && B == edge.B) || (A == edge.B && B == edge.A);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return A.GetHashCode() ^ B.GetHashCode();
            }
        }
    }
}