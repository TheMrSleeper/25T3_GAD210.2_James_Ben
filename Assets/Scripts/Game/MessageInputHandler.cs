using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MessageInputHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private int characterLimit = 32;

    private bool _hasSentThisPhase;

    private void Start()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        if (inputField != null)
        {
            inputField.characterLimit = characterLimit;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            // IMPORTANT: listen for Enter/Return via TMP's submit event
            inputField.onSubmit.AddListener(OnSubmit);
            // If onSubmit doesn't fire in your setup, you can also use:
            // inputField.onEndEdit.AddListener(OnSubmit);
        }
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(OnSubmit);
            // inputField.onEndEdit.RemoveListener(OnSubmit); // if you used it
        }
    }

    private void OnEnable()
    {
        _hasSentThisPhase = false;
        if (inputField != null)
        {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();
        }
    }

    private void Update()
    {
        if (inputField == null) return;
        if (!inputField.isFocused) return;
        if (_hasSentThisPhase) return;

        if (GameManager.Instance == null || NetworkManager.Singleton == null)
            return;

        if (GameManager.Instance.CurrentPhase.Value != TurnPhase.MessageTyping)
            return;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        if (localId != GameManager.Instance.ActivePlayerClientId.Value)
            return;

        // Send on Spacebar (after at least one character)
        if (Input.GetKeyDown(KeyCode.Space) && inputField.text.Length > 0)
        {
            SendCurrent();
            return;
        }

        // Send when character limit reached
        if (inputField.text.Length >= characterLimit)
        {
            SendCurrent();
        }
    }

    // Called by TMP when user presses Enter/Return
    private void OnSubmit(string _)
    {
        if (_hasSentThisPhase) return;
        if (GameManager.Instance == null || NetworkManager.Singleton == null) return;

        if (GameManager.Instance.CurrentPhase.Value != TurnPhase.MessageTyping)
            return;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        if (localId != GameManager.Instance.ActivePlayerClientId.Value)
            return;

        SendCurrent();
    }

    private void SendCurrent()
    {
        if (_hasSentThisPhase) return;

        string msg = inputField.text;
        _hasSentThisPhase = true;
        if (inputField != null)
            inputField.interactable = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitMessageServerRpc(msg);
        }
    }
}