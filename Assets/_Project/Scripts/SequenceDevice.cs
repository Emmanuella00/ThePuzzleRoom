using System.Collections;
using UnityEngine;


public class SequenceDevice : MonoBehaviour
{
    [Tooltip("On-screen control hint, e.g. [E] Open curtains")]
    public string prompt = "[E] Switch off";

    [Header("Action: animate (optional)")]
    [Tooltip("Object to animate (empty = this object)")]
    public Transform animateTarget;
    public bool changeScale;
    public Vector3 targetLocalScale = Vector3.one;
    [Tooltip("Local position offset to slide by, e.g. (0, 0.5, 0)")]
    public Vector3 moveBy;
    public float animTime = 1f;

    [Header("Action: sound / switch things (optional)")]
    [Tooltip("Music that stops when pressed (the stereo)")]
    public AudioSource stopAudio;
    public GameObject[] turnOff;    
    public GameObject[] turnOn;
    public AudioClip pressSound;

    [HideInInspector] public SequenceTask sequence;   
    public bool Done { get; private set; }

    private HighlightPulse glow;

    void Awake()
    {
        glow = GetComponent<HighlightPulse>();
        if (animateTarget == null) animateTarget = transform;
    }

    
    public void Press()
    {
        if (sequence != null) sequence.OnDevicePressed(this);
    }

    public void SetGlowing(bool on) { if (glow) glow.SetPulsing(on); }
    public void FlashRed() { if (glow) glow.FlashRed(); }

    
    public void Activate()
    {
        Done = true;
        if (glow) glow.MarkSolved();                  
        if (stopAudio) stopAudio.Stop();
        foreach (GameObject g in turnOff) if (g) g.SetActive(false);
        foreach (GameObject g in turnOn) if (g) g.SetActive(true);
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        Vector3 s0 = animateTarget.localScale;
        Vector3 s1 = changeScale ? targetLocalScale : s0;
        Vector3 p0 = animateTarget.localPosition;
        Vector3 p1 = p0 + moveBy;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, animTime);
            float k = Mathf.SmoothStep(0f, 1f, t);
            animateTarget.localScale = Vector3.Lerp(s0, s1, k);
            animateTarget.localPosition = Vector3.Lerp(p0, p1, k);
            yield return null;
        }
    }
}
