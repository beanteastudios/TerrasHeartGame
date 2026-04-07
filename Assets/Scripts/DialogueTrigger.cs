using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    public string nodeName = "LeifIntro";
    public float interactRange = 5f;
    public Transform player;
    public DialogueRunner dialogueRunner;
    public TMP_Text speakerNameText;

    private bool hasTriggered = false;

    void Start()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueEnded);
    }

    void OnEnable()
    {
        if (speakerNameText != null)
            speakerNameText.color = new Color(1f, 0.78f, 0.39f, 1f);
    }

    void Update()
    {
        if (hasTriggered) return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance < interactRange)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                hasTriggered = true;
                dialogueRunner.StartDialogue(nodeName);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    void OnDisable()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueEnded);
    }

    void OnDialogueEnded()
    {
        hasTriggered = false;
    }
}