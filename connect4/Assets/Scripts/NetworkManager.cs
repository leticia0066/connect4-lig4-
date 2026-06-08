using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

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
        ConnectToServer("127.0.0.1", 7777);
    }

    void ConnectToServer(string ip, int port)
    {
        client = new TcpClient();

        try
        {
            client.Connect(ip, port);

            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            isConnected = true;

            Debug.Log("[Rede] Conectado ao servidor");

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch
        {
            Debug.LogError("[Rede] Falha ao conectar");
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
                    Debug.Log("[Rede] Jogo iniciado!");
                }

                if (msg.StartsWith("MOVE:"))
                {
                    int col = int.Parse(msg.Split(':')[1]);

                    Connect4Manager.Instance?.ApplyOpponentMove(col);
                }
            }
            catch
            {
                Debug.LogWarning("[Rede] Erro na recepção");
            }
        }
    }

    public void SendMove(int column)
    {
        if (!isConnected || !opponentConnected || !gameStarted)
        {
            Debug.LogWarning("[Rede] Não enviado: jogo ainda não pronto.");
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