using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionPromptTrigger : MonoBehaviour
{
    public TMP_Text promptText;
    public Transform player;
    public float interactRange = 5f;

    void Update()
    {
        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        promptText.enabled = distance < interactRange;
    }
}