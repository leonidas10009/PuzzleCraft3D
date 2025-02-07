using UnityEngine;

namespace PuzzleModules
{
    /// <summary>
    /// Estructura para identificar de forma única un borde, sin importar el orden de los extremos.
    /// </summary>
    public readonly struct EdgeKey
    {
        public Vector3 A { get; }
        public Vector3 B { get; }

        public EdgeKey(Vector3 a, Vector3 b)
        {
            // Se asignan A y B de forma ordenada para evitar duplicados (menor primero)
            if (a.x < b.x || (Mathf.Approximately(a.x, b.x) && a.y < b.y) ||
                (Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && a.z < b.z))
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EdgeKey))
                return false;
            EdgeKey other = (EdgeKey)obj;
            return A.Equals(other.A) && B.Equals(other.B);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + A.GetHashCode();
                hash = hash * 23 + B.GetHashCode();
                return hash;
            }
        }
    }
}
