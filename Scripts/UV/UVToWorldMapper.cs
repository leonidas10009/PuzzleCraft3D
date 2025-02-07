using UnityEngine;
using System.Collections.Generic;

namespace PuzzleModules
{
    public static class UVToWorldMapper
    {
        /// <summary>
        /// Dado un punto uv (en [0,1]x[0,1]) y la malla del modelo,
        /// busca en qué cara se encuentra y devuelve la posición 3D interpolada.
        /// Este método requiere conocer la correspondencia entre UV y caras.
        /// Para un cubo, se puede implementar de forma directa.
        /// </summary>
        public static Vector3 MapUVToWorld(Mesh mesh, Vector2 uvPoint)
        {
            // Para este ejemplo, se asume que el cubo tiene un mapeo UV conocido.
            // Por ejemplo, supongamos que la cara frontal del cubo ocupa el rectángulo [0, 0.33] x [0, 1].
            // Aquí se deben definir las condiciones para cada cara.

            // Ejemplo: si el uvPoint.x está entre 0 y 0.33, asumimos que pertenece a la cara frontal.
            if (uvPoint.x >= 0f && uvPoint.x <= 0.33f)
            {
                // Mapear uvPoint a coordenadas locales de la cara.
                // Suponiendo que la cara frontal se extiende de -0.5 a 0.5 en X y de -0.5 a 0.5 en Y.
                float localX = Mathf.Lerp(-0.5f, 0.5f, uvPoint.y); // Podrías ajustar según el mapeo
                float localY = Mathf.Lerp(-0.5f, 0.5f, uvPoint.x / 0.33f);
                // Para la cara frontal, la Z es constante (por ejemplo, 0.5)
                return new Vector3(localX, localY, 0.5f);
            }

            // Deberías extender esta lógica para las otras caras del cubo.
            // Si no se encuentra una correspondencia, devolver un valor por defecto:
            return Vector3.zero;
        }
    }
}
