using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

public class DialogueAdvancer : MonoBehaviour
{
    public DialogueRunner dialogueRunner;

    private bool dialogueJustStarted = false;

    void Update()
    {
        if (!dialogueRunner.IsDialogueRunning) return;

        if (dialogueJustStarted)
        {
            dialogueJustStarted = false;
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.eKey.wasPressedThisFrame ||
            keyboard.spaceKey.wasPressedThisFrame)
        {
            dialogueRunner.RequestNextLine();
        }
    }

    public void OnDialogueStarted()
    {
        dialogueJustStarted = true;
    }
}