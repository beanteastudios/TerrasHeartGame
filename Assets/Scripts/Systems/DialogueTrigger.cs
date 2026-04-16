using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    public string nodeName;

    [Header("References")]
    public DialogueRunner dialogueRunner;
    public TMP_Text speakerNameText;

    private bool hasTriggered;

    private void OnEnable()
    {
        if (speakerNameText != null)
            speakerNameText.color = new Color(1f, 0.78f, 0.39f, 1f);

        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueEnded);
    }

    private void OnDisable()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueEnded);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }

    private void OnDialogueEnded()
    {
        hasTriggered = false;
    }

    public void TriggerDialogue()
    {
        if (hasTriggered || dialogueRunner.IsDialogueRunning) return;
        hasTriggered = true;

        DialogueAdvancer advancer = FindAnyObjectByType<DialogueAdvancer>();
        if (advancer != null)
            advancer.OnDialogueStarted();

        dialogueRunner.StartDialogue(nodeName);
    }
}