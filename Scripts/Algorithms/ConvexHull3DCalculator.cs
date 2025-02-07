using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuzzleModules
{
    public class ConvexHull3DCalculator
    {
        private const float Tolerance = 1e-6f;

        #region Clases y Estructuras Auxiliares

        /// <summary>
        /// Representa una cara triangular de la malla.
        /// Se almacena también la lista de puntos que se encuentran “fuera” (sobre la semiespacio positiva) de la cara.
        /// </summary>
        public class Face
        {
            public Vector3 A, B, C;
            public Vector3 Normal;
            public List<Vector3> OutsidePoints = new List<Vector3>();

            public Face(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
                ComputeNormal();
            }

            public void ComputeNormal()
            {
                Normal = Vector3.Cross(B - A, C - A).normalized;
            }

            /// <summary>
            /// Distancia signed de un punto a la cara (con respecto a la normal)
            /// </summary>
            public float DistanceToPoint(Vector3 point)
            {
                return Vector3.Dot(Normal, point - A);
            }

            /// <summary>
            /// Indica si un punto está “por encima” de la cara, considerando una tolerancia.
            /// </summary>
            public bool IsPointAbove(Vector3 point)
            {
                return DistanceToPoint(point) > Tolerance;
            }

            /// <summary>
            /// Devuelve las 3 aristas de la cara.
            /// </summary>
            public List<Edge> GetEdges()
            {
                return new List<Edge>
                {
                    new Edge(A, B),
                    new Edge(B, C),
                    new Edge(C, A)
                };
            }
        }

        /// <summary>
        /// Estructura que representa una arista (borde) sin dirección. 
        /// Se implementa IEquatable para que dos aristas sean iguales sin importar el orden de sus vértices.
        /// </summary>
        public struct Edge : IEquatable<Edge>
        {
            public Vector3 Start, End;

            public Edge(Vector3 start, Vector3 end)
            {
                // Para que la igualdad sea independiente del orden, se ordenan según GetHashCode.
                if (start.GetHashCode() <= end.GetHashCode())
                {
                    Start = start;
                    End = end;
                }
                else
                {
                    Start = end;
                    End = start;
                }
            }

            public bool Equals(Edge other)
            {
                return (Start == other.Start && End == other.End);
            }

            public override bool Equals(object obj)
            {
                if (obj is Edge)
                    return Equals((Edge)obj);
                return false;
            }

            public override int GetHashCode()
            {
                return Start.GetHashCode() ^ End.GetHashCode();
            }
        }

        #endregion

        #region Cálculo del Convex Hull 3D

        /// <summary>
        /// Calcula la envoltura convexa 3D de un conjunto de puntos utilizando una variante del algoritmo QuickHull.
        /// Devuelve una lista de caras trianguladas.
        /// </summary>
        /// <param name="points">Lista de puntos 3D</param>
        /// <returns>Lista de caras que forman la envoltura convexa</returns>
        public static List<Face> CalculateConvexHull(List<Vector3> points)
        {
            if (points.Count < 4)
            {
                Debug.LogError("Se requieren al menos 4 puntos para calcular la envoltura convexa 3D.");
                return null;
            }

            // Paso 1: Encontrar un tetraedro inicial (4 puntos no coplanares)
            List<Vector3> initialPoints = FindInitialTetrahedron(points);
            if (initialPoints == null)
            {
                Debug.LogError("No se pudo encontrar un tetraedro inicial. Los puntos pueden ser coplanares.");
                return null;
            }

            // Calcular un punto interior (por ejemplo, el centroide del tetraedro)
            Vector3 interiorPoint = (initialPoints[0] + initialPoints[1] + initialPoints[2] + initialPoints[3]) / 4f;

            // Crear las 4 caras iniciales del tetraedro, corrigiendo la orientación para que el punto interior quede detrás.
            List<Face> faces = new List<Face>();
            faces.Add(CreateFace(initialPoints[0], initialPoints[1], initialPoints[2], interiorPoint));
            faces.Add(CreateFace(initialPoints[0], initialPoints[3], initialPoints[1], interiorPoint));
            faces.Add(CreateFace(initialPoints[0], initialPoints[2], initialPoints[3], interiorPoint));
            faces.Add(CreateFace(initialPoints[1], initialPoints[3], initialPoints[2], interiorPoint));

            // Asignar los puntos restantes a las caras (si están “por encima”)
            List<Vector3> remainingPoints = new List<Vector3>(points);
            foreach (var p in initialPoints)
                remainingPoints.Remove(p);

            foreach (var point in remainingPoints)
            {
                foreach (var face in faces)
                {
                    if (face.IsPointAbove(point))
                    {
                        face.OutsidePoints.Add(point);
                    }
                }
            }

            // Paso 2: Expansión iterativa del casco
            List<Face> convexHull = new List<Face>(faces);

            bool existsFaceWithOutside;
            do
            {
                // Buscar una cara con puntos fuera asignados.
                Face currentFace = convexHull.FirstOrDefault(f => f.OutsidePoints.Count > 0);
                existsFaceWithOutside = (currentFace != null);

                if (existsFaceWithOutside)
                {
                    // Seleccionar el punto más alejado de la cara.
                    Vector3 farthestPoint = FindFarthestPoint(currentFace);
                    // Encontrar todas las caras visibles desde ese punto.
                    List<Face> visibleFaces = FindVisibleFaces(convexHull, farthestPoint);
                    // Calcular el horizonte: aristas que son frontera entre caras visibles y no visibles.
                    List<Edge> horizon = FindHorizon(visibleFaces);
                    // Remover las caras visibles del casco.
                    foreach (var vf in visibleFaces)
                    {
                        convexHull.Remove(vf);
                    }
                    // Para cada arista del horizonte, crear una nueva cara que conecte dicha arista con el punto.
                    List<Face> newFaces = new List<Face>();
                    foreach (var edge in horizon)
                    {
                        Face newFace = CreateFace(edge.Start, edge.End, farthestPoint, interiorPoint);
                        // Reasignar puntos: se recopilan de las caras removidas que puedan estar fuera de la nueva cara.
                        List<Vector3> reassignedPoints = new List<Vector3>();
                        foreach (var vf in visibleFaces)
                        {
                            foreach (var p in vf.OutsidePoints)
                            {
                                if (newFace.IsPointAbove(p))
                                {
                                    reassignedPoints.Add(p);
                                }
                            }
                        }
                        newFace.OutsidePoints = reassignedPoints;
                        newFaces.Add(newFace);
                    }
                    convexHull.AddRange(newFaces);
                }
            } while (existsFaceWithOutside);

            return convexHull;
        }

        /// <summary>
        /// Encuentra el punto más alejado (según la distancia signed) en la lista de puntos “fuera” de una cara.
        /// </summary>
        private static Vector3 FindFarthestPoint(Face face)
        {
            Vector3 farthest = face.OutsidePoints[0];
            float maxDistance = face.DistanceToPoint(farthest);
            foreach (var p in face.OutsidePoints)
            {
                float d = face.DistanceToPoint(p);
                if (d > maxDistance)
                {
                    maxDistance = d;
                    farthest = p;
                }
            }
            return farthest;
        }

        /// <summary>
        /// Retorna la lista de caras (del casco actual) para las cuales un punto dado está “por encima”.
        /// </summary>
        private static List<Face> FindVisibleFaces(List<Face> faces, Vector3 point)
        {
            List<Face> visible = new List<Face>();
            foreach (var face in faces)
            {
                if (face.IsPointAbove(point))
                {
                    visible.Add(face);
                }
            }
            return visible;
        }

        /// <summary>
        /// Dado un conjunto de caras visibles, retorna las aristas del “horizonte” (aparecen solo en una cara visible).
        /// </summary>
        private static List<Edge> FindHorizon(List<Face> visibleFaces)
        {
            Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();
            foreach (var face in visibleFaces)
            {
                foreach (var edge in face.GetEdges())
                {
                    if (edgeCount.ContainsKey(edge))
                        edgeCount[edge]++;
                    else
                        edgeCount[edge] = 1;
                }
            }
            List<Edge> horizon = new List<Edge>();
            foreach (var kvp in edgeCount)
            {
                if (kvp.Value == 1)
                    horizon.Add(kvp.Key);
            }
            return horizon;
        }

        /// <summary>
        /// Crea una cara a partir de tres puntos y corrige su orientación usando un punto interior.
        /// </summary>
        private static Face CreateFace(Vector3 a, Vector3 b, Vector3 c, Vector3 interiorPoint)
        {
            Face face = new Face(a, b, c);
            if (face.IsPointAbove(interiorPoint))
            {
                // Si el punto interior está “por encima”, se invierte la orientación
                face = new Face(a, c, b);
            }
            return face;
        }

        /// <summary>
        /// Selecciona 4 puntos que formen un tetraedro inicial (no coplanar).
        /// Se eligen: dos puntos más distantes, el tercero que maximiza el área del triángulo y el cuarto que maximiza el volumen.
        /// </summary>
        private static List<Vector3> FindInitialTetrahedron(List<Vector3> points)
        {
            // 1. Encontrar dos puntos con la mayor distancia.
            float maxDist = 0;
            Vector3 p1 = points[0], p2 = points[0];
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    float dist = Vector3.Distance(points[i], points[j]);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        p1 = points[i];
                        p2 = points[j];
                    }
                }
            }
            // 2. Seleccionar el tercer punto que maximice el área del triángulo.
            float maxArea = 0;
            Vector3 p3 = p1;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == p1 || points[i] == p2)
                    continue;
                float area = Vector3.Cross(p2 - p1, points[i] - p1).magnitude;
                if (area > maxArea)
                {
                    maxArea = area;
                    p3 = points[i];
                }
            }
            // 3. Seleccionar el cuarto punto que maximice el volumen (no coplanar).
            float maxVolume = 0;
            Vector3 p4 = p1;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == p1 || points[i] == p2 || points[i] == p3)
                    continue;
                float volume = Mathf.Abs(Vector3.Dot(points[i] - p1, Vector3.Cross(p2 - p1, p3 - p1)));
                if (volume > maxVolume)
                {
                    maxVolume = volume;
                    p4 = points[i];
                }
            }
            if (maxVolume < Tolerance)
                return null; // Los puntos son coplanares.
            return new List<Vector3> { p1, p2, p3, p4 };
        }

        #endregion

        #region Optimización de Caras (Fusión de Triángulos Coplanares)

        /// <summary>
        /// Optimiza las caras fusionando aquellas que son coplanares.
        /// El resultado es una lista de polígonos (cada polígono es una lista ordenada de vértices).
        /// Se agrupan las caras según su normal y se calcula el concave hull 2D de la unión de vértices en ese plano.
        /// </summary>
        /// <param name="faces">Caras trianguladas del convex hull</param>
        /// <param name="tolerance">Tolerancia para considerar caras coplanares</param>
        /// <param name="k">Parámetro para el concave hull (número de vecinos a considerar)</param>
        /// <returns>Lista de polígonos optimizados</returns>
        public static List<List<Vector3>> OptimizeFaces(List<Face> faces, float tolerance = 1e-4f, int k = 3)
        {
            // Agrupar las caras por normal usando una tolerancia estricta.
            List<List<Face>> groups = new List<List<Face>>();
            foreach (var face in faces)
            {
                bool added = false;
                foreach (var group in groups)
                {
                    if (Vector3.Dot(face.Normal, group[0].Normal) > 0.999f)
                    {
                        group.Add(face);
                        added = true;
                        break;
                    }
                }
                if (!added)
                    groups.Add(new List<Face> { face });
            }

            List<List<Vector3>> optimizedPolygons = new List<List<Vector3>>();
            foreach (var group in groups)
            {
                // Unir todos los vértices de las caras en el mismo plano
                HashSet<Vector3> uniquePoints = new HashSet<Vector3>();
                foreach (var face in group)
                {
                    uniquePoints.Add(face.A);
                    uniquePoints.Add(face.B);
                    uniquePoints.Add(face.C);
                }

                // Transformar a 2D proyectado en el plano
                Vector3 normal = group[0].Normal;
                Vector3 right = Vector3.Cross(normal, Vector3.up);
                if (right.magnitude < Tolerance) right = Vector3.Cross(normal, Vector3.right);
                Vector3 up = Vector3.Cross(right, normal);
                Matrix4x4 to2D = new Matrix4x4();
                to2D.SetRow(0, new Vector4(right.x, right.y, right.z, 0));
                to2D.SetRow(1, new Vector4(up.x, up.y, up.z, 0));
                to2D.SetRow(2, new Vector4(normal.x, normal.y, normal.z, 0));
                to2D.SetRow(3, new Vector4(0, 0, 0, 1));

                List<Vector2> projectedPoints = uniquePoints
                    .Select(p => (Vector2)to2D.MultiplyPoint3x4(p))
                    .ToList();

                // Calcular el concave hull en 2D
                List<Vector2> concaveHull2D = ConcaveHull2D(projectedPoints, k);

                // Transformar de regreso a 3D
                Matrix4x4 to3D = to2D.inverse;
                List<Vector3> concaveHull3D = concaveHull2D
                    .Select(p => to3D.MultiplyPoint3x4(new Vector3(p.x, p.y, 0)))
                    .ToList();

                optimizedPolygons.Add(concaveHull3D);
            }

            return optimizedPolygons;
        }

        #endregion



        #region Concave Hull 2D

        /// <summary>
        /// Calcula la envolvente cóncava de un conjunto de puntos en 2D utilizando el método k-nearest neighbors.
        /// </summary>
        /// <param name="points">Lista de puntos en 2D.</param>
        /// <param name="k">Número de vecinos a considerar (debe ser >= 3).</param>
        /// <returns>Lista de puntos que representan el contorno cóncavo.</returns>
        public static List<Vector2> ConcaveHull2D(List<Vector2> points, int k)
        {
            if (points.Count < 3)
                return new List<Vector2>(points); // No se puede formar un contorno

            k = Mathf.Clamp(k, 3, points.Count - 1);
            List<Vector2> hull = new List<Vector2>();

            // Seleccionar el punto más a la izquierda como inicio
            Vector2 start = points.OrderBy(p => p.x).ThenBy(p => p.y).First();
            hull.Add(start);

            Vector2 current = start;
            List<Vector2> remaining = new List<Vector2>(points);
            remaining.Remove(current);

            Vector2 lastDir = Vector2.right; // Dirección inicial arbitraria

            while (hull.Count < points.Count)
            {
                if (remaining.Count == 0)
                    break;

                // Obtener los k puntos más cercanos
                List<Vector2> nearest = remaining
                    .OrderBy(p => Vector2.Distance(p, current))
                    .Take(k)
                    .ToList();

                // Encontrar el punto con el menor cambio de dirección (ángulo mínimo)
                Vector2 next = nearest
                    .OrderBy(p => Mathf.Abs(Vector2.SignedAngle(lastDir, (p - current).normalized)))
                    .First();

                if (next == start) // Si cerramos el contorno, terminamos
                    break;

                hull.Add(next);
                remaining.Remove(next);
                lastDir = (next - current).normalized;
                current = next;
            }

            return hull;
        }


    }
}

    #endregion