using UnityEngine;
using Yarn.Unity;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public string nodeName = "LeifIntro";
    public float interactRange = 5f;
    public Transform player;
    public DialogueRunner dialogueRunner;

    private bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered) return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance < interactRange)
        {
            Debug.Log("In range! Distance: " + distance);

            if (Keyboard.current != null)
            {
                Debug.Log("E key state: " + Keyboard.current.eKey.wasPressedThisFrame);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log("Starting dialogue: " + nodeName);
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
}