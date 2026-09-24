using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Makes an object softly pulse with a glow, meaning "you can interact with me".
// Call MarkSolved() to flash green and stop glowing.
public class HighlightPulse : MonoBehaviour
{
    public Color glowColor = new Color(1f, 0.75f, 0.2f);   // warm yellow
    public float maxIntensity = 0.8f;
    public float speed = 3f;

    private readonly List<Material> mats = new List<Material>();
    private bool pulsing = true;

    void Start()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            mats.AddRange(r.materials);          // instances, so other objects aren't affected
        foreach (Material m in mats)
            m.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        if (!pulsing) return;
        float k = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f * maxIntensity;
        SetEmission(glowColor * k);
    }

    public void MarkSolved()
    {
        pulsing = false;
        StartCoroutine(FlashGreen());
    }

    IEnumerator FlashGreen()
    {
        SetEmission(Color.green * 1.5f);
        yield return new WaitForSeconds(1.2f);
        SetEmission(Color.black);
    }

    void SetEmission(Color c)
    {
        foreach (Material m in mats) m.SetColor("_EmissionColor", c);
    }
}
