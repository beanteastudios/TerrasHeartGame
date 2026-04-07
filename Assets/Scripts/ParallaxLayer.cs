using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    public float parallaxSpeed = 0.5f;
    public bool autoScroll = true;
    public float autoScrollSpeed = 0.3f;

    [Header("Wave Bob")]
    public float bobAmplitude = 0.05f;
    public float bobFrequency = 1f;

    private float startY;
    private float scrollOffset;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        startY = transform.position.y;
    }

    void Update()
    {
        // Auto scroll (ocean drifting)
        if (autoScroll)
            scrollOffset += autoScrollSpeed * Time.deltaTime;

        // Camera parallax
        float camX = mainCam.transform.position.x * parallaxSpeed;

        // Gentle vertical bob
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;

        transform.position = new Vector3(
            camX + scrollOffset,
            startY + bob,
            transform.position.z
        );
    }
}