using UnityEngine;

public class Piece : MonoBehaviour
{
    public enum PieceType
    {
        Red,
        Yellow
    }

    public PieceType type;

    public void SetType(PieceType newType)
    {
        type = newType;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        sr.color = (type == PieceType.Red) ? Color.red : Color.yellow;
    }
}