using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    public float interactRadius = 3f;
    public LayerMask interactableLayer;
    public TMP_Text promptUI;

    private DialogueTrigger current;

    void Update()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            interactRadius,
            interactableLayer
        );

        current = hit != null ? hit.GetComponent<DialogueTrigger>() : null;

        bool dialogueRunning = current != null && current.dialogueRunner != null && current.dialogueRunner.IsDialogueRunning;
        promptUI.enabled = current != null && !dialogueRunning;

        if (current != null && !dialogueRunning && Keyboard.current.eKey.wasPressedThisFrame)
        {
            promptUI.enabled = false;
            current.TriggerDialogue();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}