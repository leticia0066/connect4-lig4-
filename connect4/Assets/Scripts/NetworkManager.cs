using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    [Header("Config")]
    public string serverIP = "127.0.0.1";
    public int port = 7777;

    public bool isHost = false;

    public bool isConnected = false;
    public bool opponentConnected = false;
    public bool gameStarted = false;

    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread receiveThread;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        client = new TcpClient();

        try
        {
            Debug.Log("[Rede] Tentando conectar em " + serverIP + ":" + port);

            client.Connect(serverIP, port);

            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            isConnected = true;

            Debug.Log("[Rede] Conectado com sucesso!");

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Rede] Falha ao conectar: " + e.Message);
        }
    }

    void ReceiveLoop()
    {
        while (client != null && client.Connected)
        {
            try
            {
                string msg = reader.ReadLine();
                if (msg == null) continue;

                Debug.Log("[Rede] Recebido: " + msg);

                if (msg == "OPPONENT_JOINED")
                    opponentConnected = true;

                if (msg == "START_GAME")
                {
                    gameStarted = true;
                    Debug.Log("[Rede] JOGO INICIADO!");
                }

                if (msg.StartsWith("MOVE:"))
                {
                    int col = int.Parse(msg.Split(':')[1]);
                    Connect4Manager.Instance?.ApplyOpponentMove(col);
                }
            }
            catch
            {
                Debug.LogWarning("[Rede] Erro no ReceiveLoop");
            }
        }
    }

    public void SendMove(int column)
    {
        if (!isConnected || !opponentConnected || !gameStarted)
        {
            Debug.LogWarning("[Rede] Jogada bloqueada (jogo não pronto)");
            return;
        }

        try
        {
            writer.WriteLine("MOVE:" + column);
        }
        catch
        {
            Debug.LogError("[Rede] Erro ao enviar jogada");
        }
    }

    void OnApplicationQuit()
    {
        client?.Close();
    }
}