using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    public Renderer fireRenderer;
    public Light fireLight;
    public Color baseColor = new Color(1f, 0.4f, 0f);
    public float minIntensity = 0.8f;
    public float maxIntensity = 2.5f;
    public float minLightIntensity = 3f;
    public float maxLightIntensity = 4f;
    public float flickerSpeed = 8f;

    private Material fireMat;

    void Start()
    {
        fireMat = fireRenderer.material;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        
        // Emission du cube
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        fireMat.SetColor("_EmissionColor", baseColor * intensity);

        // Point Light
        fireLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, noise);
    }
}