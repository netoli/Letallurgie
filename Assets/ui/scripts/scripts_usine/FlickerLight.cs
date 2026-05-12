using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    public Renderer fireRenderer;
    public Color baseColor = new Color(1f, 0.4f, 0f); // orange
    public float minIntensity = 0.8f;
    public float maxIntensity = 2.5f;
    public float flickerSpeed = 8f;

    private Material fireMat;

    void Start()
    {
        fireMat = fireRenderer.material;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        fireMat.SetColor("_EmissionColor", baseColor * intensity);
    }
}