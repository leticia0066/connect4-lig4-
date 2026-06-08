using UnityEngine;

public class Connect4Manager : MonoBehaviour
{
    [Header("Configurações do Jogo")]
    public int columns = 7;
    public int rows = 6;
    public GameObject player1Prefab; // PecaVermelha
    public GameObject player2Prefab; // PecaAmarela
    public float spacing = 1.1f;    
    public Transform boardStartPos; // Posição da Cell(0,0) como em Captura de tela 2026-06-05 144554.png

    private int[,] grid; 
    private bool isPlayer1Turn = true;
    private bool isGameOver = false;
    private NetworkManager network;

    void Start()
    {
        grid = new int[columns, rows];
        network = FindObjectOfType<NetworkManager>(); // Busca o componente de rede na cena
        
        // Validação básica para evitar erros no Console
        if (boardStartPos == null) Debug.LogError("Arraste a posição inicial do Board para o script!");
    }

    void Update()
    {
        if (isGameOver || network == null) return;

        // LÓGICA DE TURNO TCP:
        // Se eu sou o servidor, só posso clicar no turno do P1.
        // Se eu sou o cliente, só posso clicar no turno do P2.
        bool meuTurno = (network.isServer && isPlayer1Turn) || (!network.isServer && !isPlayer1Turn);

        if (meuTurno && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Calcula qual coluna foi clicada com base na posição do mouse e no espaçamento
            int col = Mathf.RoundToInt((mousePos.x - boardStartPos.position.x) / spacing);
            
            if (col >= 0 && col < columns)
            {
                // Verifica se a coluna não está cheia antes de enviar
                if (grid[col, rows - 1] == 0)
                {
                    // 1. Envia a jogada via TCP para o outro jogador
                    network.SendMove(col);
                    
                    // 2. Aplica a jogada localmente
                    TryPlacePiece(col);
                }
            }
        }
    }

    // Esta função é chamada tanto pelo clique local quanto pelo NetworkManager ao receber dados
    public void TryPlacePiece(int col)
    {
        if (col < 0 || col >= columns) return;

        for (int r = 0; r < rows; r++)
        {
            if (grid[col, r] == 0)
            {
                grid[col, r] = isPlayer1Turn ? 1 : 2;
                SpawnPiece(col, r);
                
                if (CheckWin(col, r))
                {
                    Debug.Log("Fim de jogo! Vencedor: Jogador " + (isPlayer1Turn ? "1" : "2"));
                    isGameOver = true;
                }
                
                // Alterna o turno
                isPlayer1Turn = !isPlayer1Turn;
                break;
            }
        }
    }

    void SpawnPiece(int col, int row)
    {
        Vector3 spawnPos = new Vector3(
            boardStartPos.position.x + (col * spacing),
            boardStartPos.position.y + (row * spacing),
            -1f 
        );

        Instantiate(isPlayer1Turn ? player1Prefab : player2Prefab, spawnPos, Quaternion.identity);
    }

    #region Algoritmo de Vitória
    bool CheckWin(int c, int r)
    {
        int player = grid[c, r];
        return (CheckDirection(c, r, 1, 0) + CheckDirection(c, r, -1, 0) >= 3) || // Horizontal
               (CheckDirection(c, r, 0, 1) + CheckDirection(c, r, 0, -1) >= 3) || // Vertical
               (CheckDirection(c, r, 1, 1) + CheckDirection(c, r, -1, -1) >= 3) || // Diagonal /
               (CheckDirection(c, r, 1, -1) + CheckDirection(c, r, -1, 1) >= 3);   // Diagonal \
    }

    int CheckDirection(int c, int r, int dc, int dr)
    {
        int count = 0;
        int p = grid[c, r];
        int nextC = c + dc;
        int nextR = r + dr;

        while (nextC >= 0 && nextC < columns && nextR >= 0 && nextR < rows && grid[nextC, nextR] == p)
        {
            count++;
            nextC += dc;
            nextR += dr;
        }
        return count;
    }
    #endregion
}