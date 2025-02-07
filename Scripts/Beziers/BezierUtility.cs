using System.Collections.Generic;
using UnityEngine;

namespace PuzzleModules
{
    public static class BezierUtility
    {
        // Caché para almacenar coeficientes binomiales calculados previamente
        private static Dictionary<(int, int), double> binomialCache = new Dictionary<(int, int), double>();

        /// <summary>
        /// Evalúa una curva Bézier de grado n usando la fórmula de Bernstein en 3D.
        /// </summary>
        public static Vector3 EvaluateBezier(List<Vector3> controlPoints, float t)
        {
            int n = controlPoints.Count - 1;
            Vector3 point = Vector3.zero;
            for (int i = 0; i <= n; i++)
            {
                double binCoeff = BinomialCoefficient(n, i); // Optimizado
                float term = (float)(binCoeff * Mathf.Pow(1 - t, n - i) * Mathf.Pow(t, i));
                point += term * controlPoints[i];
            }
            return point;
        }

        /// <summary>
        /// Genera una lista de puntos que aproximan la curva Bézier en 3D.
        /// </summary>
        public static List<Vector3> GenerateBezierCurve(List<Vector3> controlPoints, int segments)
        {
            List<Vector3> curvePoints = new List<Vector3>();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                curvePoints.Add(EvaluateBezier(controlPoints, t));
            }
            return curvePoints;
        }

        /// <summary>
        /// Genera el contorno de una spline basado en un número de bordes.
        /// </summary>
        public static List<Vector3> GenerateSplineOutline(int numEdges, float pieceSize)
        {
            List<Vector3> points = new List<Vector3>();

            // Generar los bordes de la pieza
            for (int i = 0; i < numEdges; i++)
            {
                Vector3 start = GetVertexPosition(i, numEdges, pieceSize);
                Vector3 end = GetVertexPosition((i + 1) % numEdges, numEdges, pieceSize);
                bool isTab = i % 2 == 0; // Alternar entre lengüetas y huecos

                // Generar una curva Spline para cada borde
                points.AddRange(GenerateSplineEdge(start, end, isTab));
            }

            return points;
        }

        /// <summary>
        /// Calcula la posición de un vértice en un polígono regular.
        /// </summary>
        private static Vector3 GetVertexPosition(int index, int numEdges, float pieceSize)
        {
            float angle = index * Mathf.PI * 2 / numEdges; // Calcular la posición del vértice
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * pieceSize; // Ajustar según el tamaño de la pieza
        }

        private static List<Vector3> GenerateSplineEdge(Vector3 start, Vector3 end, bool isTab)
        {
            List<Vector3> points = new List<Vector3>();

            // Calcular el punto medio
            Vector3 midPoint = (start + end) / 2;

            // Calcular la dirección del borde
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);

            // Desplazar el punto medio hacia afuera o hacia adentro
            float tabSize = 0.5f; // Ajusta este valor según el tamaño deseado de la lengüeta/hueco
            Vector3 tabPoint = midPoint + perpendicular * (isTab ? tabSize : -tabSize);

            // Generar puntos a lo largo de la curva Spline
            int splineResolution = 10; // Ajusta la resolución de la curva (número de segmentos)
            for (int i = 0; i <= splineResolution; i++)
            {
                float t = i / (float)splineResolution;
                Vector3 point = CalculateQuadraticBezierPoint(t, start, tabPoint, end);
                points.Add(point);
            }

            return points;
        }

        // Método auxiliar para calcular un punto en una curva Bézier cuadrática
        private static Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            // Fórmula de Bézier cuadrática: B(t) = (1-t)^2 * P0 + 2 * (1-t) * t * P1 + t^2 * P2
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 point = uu * p0; // (1-t)^2 * P0
            point += 2 * u * t * p1; // 2 * (1-t) * t * P1
            point += tt * p2;        // t^2 * P2

            return point;
        }

        /// <summary>
        /// Calcula el coeficiente binomial "n sobre k" de manera eficiente.
        /// </summary>
        private static double BinomialCoefficient(int n, int k)
        {
            // Usar simetría para reducir el cálculo
            if (k > n - k) k = n - k;

            // Verificar si el resultado ya está en la caché
            if (binomialCache.TryGetValue((n, k), out double cachedResult))
            {
                return cachedResult;
            }

            // Calcular iterativamente
            double result = 1.0;
            for (int i = 1; i <= k; i++)
            {
                result *= (n - (k - i)) / (double)i;
            }

            // Almacenar en la caché
            binomialCache[(n, k)] = result;

            return result;
        }
    }
}