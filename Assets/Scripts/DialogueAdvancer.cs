using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

public class DialogueAdvancer : MonoBehaviour
{
    public DialogueRunner dialogueRunner;

    void Update()
    {
        if (!dialogueRunner.IsDialogueRunning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.eKey.wasPressedThisFrame ||
            keyboard.spaceKey.wasPressedThisFrame)
        {
            dialogueRunner.RequestNextLine();
        }
    }
}