using System.Collections.Generic;
using UnityEngine;

namespace PuzzleModules
{
    public class ConvexHullCalculator
    {
        public struct Face
        {
            public Vector3 A, B, C;

            public Face(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
            }

            public Vector3 Normal => Vector3.Cross(B - A, C - A).normalized;

            public bool IsPointAbove(Vector3 point)
            {
                return Vector3.Dot(Normal, point - A) > 0;
            }
        }

        /// <summary>
        /// Calculates the convex hull of a set of points in 3D space using the QuickHull algorithm.
        /// </summary>
        /// <param name="points">List of points to calculate the convex hull for.</param>
        /// <returns>A list of triangular faces representing the convex hull.</returns>
        public static List<Face> CalculateConvexHull(List<Vector3> points)
        {
            if (points.Count < 4)
            {
                Debug.LogError("Convex hull calculation requires at least 4 points.");
                return null;
            }

            // Step 1: Find the extreme points
            Vector3 minX = points[0], maxX = points[0];
            foreach (var point in points)
            {
                if (point.x < minX.x) minX = point;
                if (point.x > maxX.x) maxX = point;
            }

            // Step 2: Split points into two sets
            List<Vector3> leftSet = new List<Vector3>();
            List<Vector3> rightSet = new List<Vector3>();
            foreach (var point in points)
            {
                if (point == minX || point == maxX) continue;

                float position = Vector3.Cross(maxX - minX, point - minX).z;
                if (position > 0)
                {
                    leftSet.Add(point);
                }
                else if (position < 0)
                {
                    rightSet.Add(point);
                }
            }

            // Step 3: Recursively build the hull
            List<Face> hull = new List<Face>();
            BuildHull(hull, minX, maxX, leftSet);
            BuildHull(hull, maxX, minX, rightSet);

            return hull;
        }
        public static List<Face> CalculateConvexHullWithFixedExterior(List<Vector3> exteriorPoints, List<Vector3> interiorPoints)
        {
            // Combinar puntos exteriores e interiores
            List<Vector3> allPoints = new List<Vector3>(exteriorPoints);
            allPoints.AddRange(interiorPoints);

            // Calcular la envoltura convexa de todos los puntos
            List<Face> hullFaces = CalculateConvexHull(allPoints);

            // Filtrar las caras para asegurarse de que los puntos exteriores estén incluidos
            List<Face> filteredFaces = new List<Face>();
            foreach (var face in hullFaces)
            {
                if (exteriorPoints.Contains(face.A) || exteriorPoints.Contains(face.B) || exteriorPoints.Contains(face.C))
                {
                    filteredFaces.Add(face);
                }
            }

            return filteredFaces;
        }
        /// <summary>
        /// Recursively builds the convex hull for a set of points.
        /// </summary>
        private static void BuildHull(List<Face> hull, Vector3 a, Vector3 b, List<Vector3> points)
        {
            if (points.Count == 0)
            {
                // Base case: Add the edge as a face
                hull.Add(new Face(a, b, Vector3.zero)); // Placeholder for the third vertex
                return;
            }

            // Find the farthest point from the line AB
            Vector3 farthest = FindFarthestPoint(points, a, b);

            // Split the remaining points into two sets
            List<Vector3> leftSet1 = new List<Vector3>();
            List<Vector3> leftSet2 = new List<Vector3>();
            foreach (var point in points)
            {
                if (point == farthest) continue;

                if (IsPointLeftOfLine(a, farthest, point))
                {
                    leftSet1.Add(point);
                }
                else if (IsPointLeftOfLine(farthest, b, point))
                {
                    leftSet2.Add(point);
                }
            }

            // Recursively build the hull
            BuildHull(hull, a, farthest, leftSet1);
            BuildHull(hull, farthest, b, leftSet2);
        }

        /// <summary>
        /// Finds the farthest point from a line segment.
        /// </summary>
        private static Vector3 FindFarthestPoint(List<Vector3> points, Vector3 a, Vector3 b)
        {
            Vector3 farthest = points[0];
            float maxDistance = 0;

            foreach (var point in points)
            {
                float distance = Vector3.Cross(b - a, point - a).magnitude;
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthest = point;
                }
            }

            return farthest;
        }

        /// <summary>
        /// Determines if a point is to the left of a line segment.
        /// </summary>
        private static bool IsPointLeftOfLine(Vector3 a, Vector3 b, Vector3 point)
        {
            return Vector3.Cross(b - a, point - a).z > 0;
        }
    }
}