using System.Collections.Generic;
using UnityEngine;
using HullDelaunayVoronoi;           // Asegúrate de tener la librería importada
using HullDelaunayVoronoi.Primitives; // Para usar Vertex3, si fuera necesario

namespace PuzzleModules
{
    /// <summary>
    /// Genera un diagrama de Voronoi en 3D (stub) utilizando la librería Hull-Delaunay-Voronoi.
    /// </summary>
    public class VoronoiGenerator3D
    {
        /// <summary>
        /// Representa un sitio en 3D.
        /// </summary>
        public class Site3D
        {
            public Vector3 Position { get; private set; }

            public Site3D(float x, float y, float z)
            {
                Position = new Vector3(x, y, z);
            }

            public Site3D(Vector3 position)
            {
                Position = position;
            }
        }

        /// <summary>
        /// Representa una celda de Voronoi en 3D.
        /// Esta implementación es un stub: se genera un cubo centrado en el sitio.
        /// </summary>
        public class Cell3D
        {
            // Lista de vértices que definen el poliedro (en este caso, un cubo)
            public List<Vector3> Vertices { get; private set; }
            // Sitio asociado a esta celda
            public Site3D Site { get; private set; }

            public Cell3D(Site3D site, List<Vector3> vertices)
            {
                Site = site;
                Vertices = vertices ?? new List<Vector3>();
            }
        }

        /// <summary>
        /// Genera el diagrama de Voronoi en 3D (stub) basado en una lista de sitios y un volumen (Bounds).
        /// NOTA: Esta implementación no calcula un Voronoi real; simplemente asigna a cada sitio una celda cúbica centrada en él.
        /// </summary>
        /// <param name="sites">Lista de sitios en 3D (Site3D).</param>
        /// <param name="bounds">Los límites del volumen en 3D.</param>
        /// <returns>Una lista de celdas Voronoi en 3D (Cell3D).</returns>
        public List<Cell3D> GenerateDiagram(List<Site3D> sites, Bounds bounds)
        {
            List<Cell3D> cells = new List<Cell3D>();

            // Aproximamos la cantidad de celdas en cada dimensión usando la raíz cúbica del número de sitios.
            int n = Mathf.CeilToInt(Mathf.Pow(sites.Count, 1f / 3f));
            float cellWidth = bounds.size.x / n;
            float cellHeight = bounds.size.y / n;
            float cellDepth = bounds.size.z / n;

            // Para cada sitio, generamos un cubo centrado en su posición.
            foreach (Site3D s in sites)
            {
                List<Vector3> vertices = new List<Vector3>()
                {
                    new Vector3(s.Position.x - cellWidth / 2, s.Position.y - cellHeight / 2, s.Position.z - cellDepth / 2),
                    new Vector3(s.Position.x + cellWidth / 2, s.Position.y - cellHeight / 2, s.Position.z - cellDepth / 2),
                    new Vector3(s.Position.x + cellWidth / 2, s.Position.y + cellHeight / 2, s.Position.z - cellDepth / 2),
                    new Vector3(s.Position.x - cellWidth / 2, s.Position.y + cellHeight / 2, s.Position.z - cellDepth / 2),
                    new Vector3(s.Position.x - cellWidth / 2, s.Position.y - cellHeight / 2, s.Position.z + cellDepth / 2),
                    new Vector3(s.Position.x + cellWidth / 2, s.Position.y - cellHeight / 2, s.Position.z + cellDepth / 2),
                    new Vector3(s.Position.x + cellWidth / 2, s.Position.y + cellHeight / 2, s.Position.z + cellDepth / 2),
                    new Vector3(s.Position.x - cellWidth / 2, s.Position.y + cellHeight / 2, s.Position.z + cellDepth / 2)
                };

                cells.Add(new Cell3D(s, vertices));
            }

            return cells;
        }
    }
}
