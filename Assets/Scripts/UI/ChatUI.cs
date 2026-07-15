using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chat.UI
{
    
    public class ChatUI : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private TMP_Text messageEntryPrefab;  
        [SerializeField] private TMP_InputField messageInputField;
        [SerializeField] private Button sendButton;

        private void Start()
        {
            sendButton.onClick.AddListener(OnSendClicked);
            messageInputField.onSubmit.AddListener(_ => OnSendClicked());

            NetworkManager.Instance.OnMessageReceived += AddMessageToHistory;
            NetworkManager.Instance.OnDisconnected += HandleDisconnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance == null) return;
            NetworkManager.Instance.OnMessageReceived -= AddMessageToHistory;
            NetworkManager.Instance.OnDisconnected -= HandleDisconnected;
        }

        private void OnSendClicked()
        {
            string text = messageInputField.text;
            if (string.IsNullOrWhiteSpace(text)) return;

            NetworkManager.Instance.SendChatMessage(text);
            messageInputField.text = string.Empty;
            messageInputField.ActivateInputField();
        }

        private void AddMessageToHistory(string rawMessage)
        {
            TMP_Text entry = Instantiate(messageEntryPrefab, contentParent);
            entry.text = FormatForDisplay(rawMessage);
            entry.gameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private static string FormatForDisplay(string rawMessage)
        {
            int separatorIndex = rawMessage.IndexOf('|');
            if (separatorIndex < 0)
            {
                return rawMessage;
            }

            string sender = rawMessage.Substring(0, separatorIndex);
            string text = rawMessage.Substring(separatorIndex + 1);
            return $"<b>{sender}</b>\n{text}";
        }

        private void HandleDisconnected()
        {
            TMP_Text entry = Instantiate(messageEntryPrefab, contentParent);
            entry.text = "[Conexión perdida]";
            entry.gameObject.SetActive(true);
        }
    }
}