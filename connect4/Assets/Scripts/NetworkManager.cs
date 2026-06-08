using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    [Header("Configurações de Conexão")]
    public string ipAddress = "127.0.0.1";
    public int port = 25000;
    public bool isServer = true;

    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private Thread listenThread;
    
    private int receivedColumn = -1;
    private Connect4Manager gameManager;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        gameManager = FindObjectOfType<Connect4Manager>();
        
        if (isServer)
            StartServer();
        else
            StartClient();
    }

    #region Servidor / Cliente Setup
    void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Parse(ipAddress), port);
            server.Start();
            Debug.Log($"[Servidor] Aguardando conexão na porta {port}...");
            
            listenThread = new Thread(() => {
                try
                {
                    client = server.AcceptTcpClient();
                    stream = client.GetStream();
                    Debug.Log("[Servidor] Cliente conectado!");
                    
                    receiveThread = new Thread(ReceiveData);
                    receiveThread.Start();
                }
                catch (Exception)
                {
                    // Ignora exceção ao fechar o servidor enquanto espera conexão
                }
            });
            listenThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("[Servidor] Erro: " + e.Message);
        }
    }

    void StartClient()
    {
        try
        {
            client = new TcpClient();
            client.Connect(ipAddress, port);
            stream = client.GetStream();
            Debug.Log("[Cliente] Conectado ao servidor!");

            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("[Cliente] Erro ao conectar: " + e.Message);
        }
    }
    #endregion

    #region Comunicação (Enviar e Receber)
    void ReceiveData()
    {
        byte[] buffer = new byte[4]; 
        while (client != null && client.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    int col = BitConverter.ToInt32(buffer, 0);
                    
                    lock (this)
                    {
                        receivedColumn = col;
                    }
                }
                else
                {
                    // Se ler 0 bytes, o outro lado fechou a conexão de forma limpa
                    break;
                }
            }
            catch (Exception)
            {
                break;
            }
        }
    }

    public void SendMove(int column)
    {
        if (stream == null || client == null || !client.Connected) 
        {
            Debug.LogWarning("[Rede] Não enviado: Nenhum oponente conectado ainda.");
            return;
        }

        try
        {
            byte[] data = BitConverter.GetBytes(column);
            stream.Write(data, 0, data.Length);
            Debug.Log($"[Rede] Coluna {column} enviada com sucesso.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Rede] Erro ao enviar: " + e.Message);
        }
    }
    #endregion

    void Update()
    {
        lock (this)
        {
            if (receivedColumn != -1)
            {
                gameManager.TryPlacePiece(receivedColumn);
                receivedColumn = -1; 
            }
        }
    }

    // CORREÇÃO DO ERRO DO CONSOLE: Fecha tudo de forma segura ao sair
    void OnApplicationQuit()
    {
        // 1. Fecha o fluxo de dados e os sockets para interromper os bloqueios de leitura/escuta
        if (stream != null) { stream.Close(); stream = null; }
        if (client != null) { client.Close(); client = null; }
        if (server != null) { server.Stop(); server = null; }

        // 2. Aguarda o encerramento amigável das Threads em segundo plano
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(50); 
        }
        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Join(50);
        }
        
        Debug.Log("[Rede] Conexões encerradas com sucesso.");
    }
}