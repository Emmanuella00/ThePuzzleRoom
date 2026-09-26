using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightPulse : MonoBehaviour
{
    public Color glowColor = new Color(1f, 0.75f, 0.2f);   
    public float maxIntensity = 0.8f;
    public float speed = 3f;
    [Tooltip("Untick for objects that should only start glowing later (e.g. checkout routine steps)")]
    public bool startPulsing = true;

    private readonly List<Material> mats = new List<Material>();
    private bool pulsing = true;
    private bool flashing;

    void Start()
    {
        pulsing = startPulsing;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            mats.AddRange(r.materials);          
        foreach (Material m in mats)
            m.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        if (!pulsing || flashing) return;
        float k = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f * maxIntensity;
        SetEmission(glowColor * k);
    }

    public void MarkSolved()
    {
        pulsing = false;
        StopAllCoroutines();
        StartCoroutine(Flash(Color.green * 1.5f, 1.2f, false));
    }

    public void FlashRed()
    {
        StopAllCoroutines();
        StartCoroutine(Flash(Color.red * 1.5f, 0.5f, true));
    }

    
    public void SetPulsing(bool on)
    {
        pulsing = on;
        if (!on && !flashing) SetEmission(Color.black);
    }

    IEnumerator Flash(Color c, float seconds, bool resumePulse)
    {
        flashing = true;
        SetEmission(c);
        yield return new WaitForSeconds(seconds);
        flashing = false;
        if (!resumePulse || !pulsing) SetEmission(Color.black);
    }

    void SetEmission(Color c)
    {
        foreach (Material m in mats) m.SetColor("_EmissionColor", c);
    }
}
