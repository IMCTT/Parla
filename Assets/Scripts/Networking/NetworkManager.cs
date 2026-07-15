using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking
{
   
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public bool IsServer { get; private set; }
        public bool IsConnected { get; private set; }
        public string localUsername { get; set; } = "defaultuser";

        public event Action OnConnected;
        public event Action<string> OnConnectionFailed;

        public event Action OnDisconnected;
        public event Action<string> OnMessageReceived;

        //Server
        private TcpListener listener;
        private readonly List<ConnectedClient> serverClients = new List<ConnectedClient>();

        //Cliente
        private TcpClient client;
        private StreamReader clientReader;

        private StreamWriter clientWriter;

        private bool running;

        
        private class ConnectedClient
        {
            public TcpClient TcpClient;

            public StreamWriter Writer;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        

        public async void StartAsServer(int port)
        {
            try
            {
                IsServer = true;
                running = true;
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                IsConnected = true;
                OnConnected?.Invoke();

                while (running)
                {
                    
                    TcpClient newClient = await listener.AcceptTcpClientAsync();

                    var connected = new ConnectedClient
                    {
                        TcpClient = newClient,

                        Writer = new StreamWriter(newClient.GetStream(), Encoding.UTF8) { AutoFlush = true, NewLine = "\n" }
                    };
                    serverClients.Add(connected);
                    
                    ReadClientLoop(connected);
                }
            }
            catch (Exception e)
            {
                OnConnectionFailed?.Invoke(e.Message);
            }
        }

        private async void ReadClientLoop(ConnectedClient connectedClient)
        {
            var reader = new StreamReader(connectedClient.TcpClient.GetStream(), Encoding.UTF8);
            try
            {
                while (running)
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null) break;

                    await BroadcastToClientsAsync(line);
                }
            }
            catch
            {
                
            }
            finally
            {
                serverClients.Remove(connectedClient);
                connectedClient.TcpClient.Close();
            }
        }

        private async Task BroadcastToClientsAsync(string message)
        {
            OnMessageReceived?.Invoke(message); // este s para que lo vea el host

            for (int i = serverClients.Count - 1; i >= 0; i--)
            {
                ConnectedClient currentConnected = serverClients[i];
                try
                {
                    await currentConnected.Writer.WriteLineAsync(message);
                }
                catch
                {
                    serverClients.RemoveAt(i);
                }
            }
        }

       

        public async void StartAsClient(string ip, int port)
        {
            try
            {
                IsServer = false;
                running = true;

                client = new TcpClient();
                await client.ConnectAsync(ip, port);

                NetworkStream stream = client.GetStream();
                clientReader = new StreamReader(stream, Encoding.UTF8);

                clientWriter = new StreamWriter(stream, Encoding.UTF8)
                { AutoFlush = true, NewLine = "\n" };

                IsConnected = true;
                OnConnected?.Invoke();

                while (running)
                {
                    string line = await clientReader.ReadLineAsync();
                    if (line == null) break; 

                    OnMessageReceived?.Invoke(line);
                }
            }
            catch (Exception e)
            {
                if (!IsConnected)
                    OnConnectionFailed?.Invoke(e.Message);
            }
            finally
            {
                bool wasConnected = IsConnected;

                IsConnected = false;
                if (wasConnected)
                    OnDisconnected?.Invoke();
            }
        }

        public async void SendChatMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            string formatted = FormatMessage(localUsername, message);

            if (IsServer)
            {
                await BroadcastToClientsAsync(formatted);
            }
            else if (clientWriter != null && IsConnected)
            {
                try
                {
                    await clientWriter.WriteLineAsync(formatted);
                }
                catch
                {
                    IsConnected = false;
                    OnDisconnected?.Invoke();
                }
            }
        }


        private static string FormatMessage(string username, string message)
        {
            string safeUser = string.IsNullOrWhiteSpace(username) ? "default" : username.Replace("|", "").Trim();
            string safeMessage = message.Replace("|", "");
            return $"{safeUser}|{safeMessage}";
        }

        

        public void Disconnect()
        {
            running = false;
            IsConnected = false;

            try { listener?.Stop(); } catch { }
            try { client?.Close(); } catch { }

            foreach (var c in serverClients) c.TcpClient.Close();
            serverClients.Clear();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}