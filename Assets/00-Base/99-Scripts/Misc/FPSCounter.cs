using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public float updateInterval = 0.5f;
    public TextMeshProUGUI fpsText;

    private float accum = 0f;
    private int frames = 0;
    private float timeleft;
    private float fps;

    void Start()
    {
        timeleft = updateInterval;

        // Ensure we have a TextMeshProUGUI component
        if (fpsText == null)
        {
            Debug.LogError("Please assign a TextMeshProUGUI component to the FPSCounter script!");
            enabled = false;
        }
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        if (timeleft <= 0.0)
        {
            fps = accum / frames;
            timeleft = updateInterval;
            accum = 0f;
            frames = 0;

            fpsText.text = $"FPS: {fps:F2}";
        }
    }
}