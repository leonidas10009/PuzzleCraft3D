using System.Collections.Generic;
using UnityEngine;

namespace PuzzleModules
{
    public class PuzzleCellProcessor
    {
        /// <summary>
        /// Procesa una celda del diagrama de Voronoi para generar un contorno modificado en 3D.
        /// </summary>
        /// <param name="cell">La celda a procesar (tipo VoronoiGenerator3D.Cell3D).</param>
        /// <param name="connectorSize">El tamaño de los conectores entre piezas.</param>
        /// <param name="bezierSegments">El número de segmentos para suavizar los bordes.</param>
        /// <param name="planeNormal">La normal del plano en el que se encuentra la celda (se puede usar para orientar los conectores si es necesario).</param>
        /// <returns>Una lista de puntos (Vector3) que representan el contorno modificado de la celda.</returns>
        public List<Vector3> ProcessCell(VoronoiGenerator3D.Cell3D cell, float connectorSize, int bezierSegments, Vector3 planeNormal)
        {
            List<Vector3> modifiedOutline = new List<Vector3>();

            // Obtenemos los vértices de la celda; ya es List<Vector3>
            List<Vector3> cellPoints = cell.Vertices;

            // Copiamos el contorno original
            modifiedOutline.AddRange(cellPoints);

            // Agregamos conectores entre los puntos
            AddConnectors(modifiedOutline, connectorSize);

            // Suavizamos el contorno utilizando Bézier si se requieren segmentos
            if (bezierSegments > 0)
            {
                modifiedOutline = SmoothOutline(modifiedOutline, bezierSegments);
            }

            return modifiedOutline;
        }

        /// <summary>
        /// Agrega conectores entre los puntos del contorno.
        /// </summary>
        void AddConnectors(List<Vector3> outline, float connectorSize)
        {
            for (int i = 0; i < outline.Count; i++)
            {
                Vector3 currentPoint = outline[i];
                Vector3 nextPoint = outline[(i + 1) % outline.Count];

                // Calcular la dirección del conector
                Vector3 direction = (nextPoint - currentPoint).normalized;
                Vector3 connectorStart = currentPoint + direction * (connectorSize / 2);
                Vector3 connectorEnd = nextPoint - direction * (connectorSize / 2);

                // Reemplazar el punto actual con el inicio del conector
                outline[i] = connectorStart;
                // Insertar el final del conector entre current y next
                outline.Insert(i + 1, connectorEnd);
                i++; // Saltar el conector insertado
            }
        }

        /// <summary>
        /// Suaviza el contorno utilizando segmentos de Bézier.
        /// </summary>
        List<Vector3> SmoothOutline(List<Vector3> outline, int segments)
        {
            List<Vector3> smoothedOutline = new List<Vector3>();

            // Recorremos cada grupo consecutivo de tres puntos para aproximar con una curva cuadrática
            for (int i = 0; i < outline.Count; i++)
            {
                Vector3 p0 = outline[i];
                Vector3 p1 = outline[(i + 1) % outline.Count];
                Vector3 p2 = outline[(i + 2) % outline.Count];

                for (int j = 0; j <= segments; j++)
                {
                    float t = j / (float)segments;
                    Vector3 bezierPoint = CalculateBezierPoint(t, p0, p1, p2);
                    smoothedOutline.Add(bezierPoint);
                }
            }

            return smoothedOutline;
        }

        /// <summary>
        /// Calcula un punto en una curva de Bézier cuadrática en 3D.
        /// </summary>
        Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }
    }
}
