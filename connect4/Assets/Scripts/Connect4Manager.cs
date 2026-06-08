using UnityEngine;

public class Connect4Manager : MonoBehaviour
{
    public static Connect4Manager Instance;

    public GameObject piecePrefab;

    private bool isMyTurn = true;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (NetworkManager.Instance == null)
            return;

        if (!NetworkManager.Instance.gameStarted)
            return;

        if (!isMyTurn)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            int column = GetColumnClicked();

            if (column == -1)
                return;

            PlayMove(column, true);
        }
    }

    public void PlayMove(int column, bool isLocalPlayer)
    {
        SpawnPiece(column, isLocalPlayer);

        if (isLocalPlayer)
        {
            NetworkManager.Instance.SendMove(column);
            isMyTurn = false;
        }
        else
        {
            isMyTurn = true;
        }
    }

    public void ApplyOpponentMove(int column)
    {
        PlayMove(column, false);
    }

    void SpawnPiece(int column, bool isRed)
    {
        Vector3 pos = GetSpawnPosition(column);

        GameObject piece = Instantiate(piecePrefab, pos, Quaternion.identity);

        Piece p = piece.GetComponent<Piece>();

        p.SetType(isRed ? Piece.PieceType.Red : Piece.PieceType.Yellow);
    }

    Vector3 GetSpawnPosition(int column)
    {
        float startX = -3f;
        float spacing = 1f;

        return new Vector3(startX + (column * spacing), 4f, 0);
    }

    int GetColumnClicked()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int column = Mathf.RoundToInt(mousePos.x + 3f);

        if (column < 0 || column > 6)
            return -1;

        return column;
    }
}