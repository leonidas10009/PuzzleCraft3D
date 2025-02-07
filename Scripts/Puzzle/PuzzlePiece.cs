using UnityEngine;

namespace PuzzleModules
{
    public class PuzzlePiece
    {
        public enum ConnectorType { None, Male, Female }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }

        public ConnectorType Left { get; private set; }
        public ConnectorType Top { get; private set; }
        public ConnectorType Right { get; private set; }
        public ConnectorType Bottom { get; private set; }

        public PuzzlePiece(int row, int col, float width, float height,
                           ConnectorType left, ConnectorType top, ConnectorType right, ConnectorType bottom)
        {
            Row = row;
            Col = col;
            Width = width;
            Height = height;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public bool IsNeighbor(PuzzlePiece other)
        {
            if (other.Row == this.Row && Mathf.Abs(other.Col - this.Col) == 1) return true;
            if (other.Col == this.Col && Mathf.Abs(other.Row - this.Row) == 1) return true;
            return false;
        }
    }
}