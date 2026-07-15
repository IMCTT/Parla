using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
   
    public class ConnectionUI : MonoBehaviour
    {
        [Header("Modo")]
        [SerializeField] private Toggle serverModeToggle; // ON = Cliente-Servidor, OFF = Cliente

        [Header("Campos")]
        [SerializeField] private TMP_InputField usernameInputField;
        [SerializeField] private TMP_InputField ipInputField;
        [SerializeField] private TMP_InputField portInputField;
        [SerializeField] private Button connectButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Paneles")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private GameObject chatPanel;


        private void Start()
        {
            connectButton.onClick.AddListener(OnConnectClicked);

            

            NetworkManager.Instance.OnConnected += HandleConnected;
            NetworkManager.Instance.OnConnectionFailed += HandleConnectionFailed;

            chatPanel.SetActive(false);
            connectionPanel.SetActive(true);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance == null) return;
            NetworkManager.Instance.OnConnected -= HandleConnected;
            NetworkManager.Instance.OnConnectionFailed -= HandleConnectionFailed;
        }

        private void OnConnectClicked()
        {
            if (!int.TryParse(portInputField.text, out int port))
            {
                SetStatus("Puerto invalido.");
                return;
            }

            bool startAsServer = serverModeToggle != null && serverModeToggle.isOn;

            string username = string.IsNullOrWhiteSpace(usernameInputField.text)
                ? "Jugador"
                : usernameInputField.text.Trim();
            NetworkManager.Instance.localUsername = username;

            connectButton.interactable = false;

            if (startAsServer)
            {
                NetworkManager.Instance.StartAsServer(port);
            }
            else
            {
                string ip = string.IsNullOrWhiteSpace(ipInputField.text) ? "127.0.0.1" : ipInputField.text.Trim();
                NetworkManager.Instance.StartAsClient(ip, port);
            }
        }

        private void HandleConnected()
        {
            SetStatus("Conectado.");
            connectionPanel.SetActive(false);
            chatPanel.SetActive(true);
        }

        private void HandleConnectionFailed(string error)
        {
            connectButton.interactable = true;
            SetStatus($"Error: {error}");
        }

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }
    }
}