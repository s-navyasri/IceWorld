using UnityEngine;

// Music and sound effects are generated in code, so no audio files are needed.
public class GameAudio : MonoBehaviour
{
    public float mv = .5f, sv = .8f;
    AudioSource mus, fx;
    AudioClip coin, gem, hit, jump, pow, win;

    void Awake()
    {
        mus = gameObject.AddComponent<AudioSource>(); fx = gameObject.AddComponent<AudioSource>();
        coin = Tone(880, 1320, .12f, 1); gem = Tone(1200, 1800, .22f, 0); hit = Tone(200, 50, .35f, 2);
        jump = Tone(300, 600, .15f, 0); pow = Tone(500, 1000, .3f, 0); win = Tone(500, 1000, .6f, 0);
        mus.clip = Music(); mus.loop = true; mus.Play(); Apply();
    }
    public void Apply() { mus.volume = mv * .35f; }
    public void Coin() { fx.PlayOneShot(coin, sv * .5f); }
    public void Gem() { fx.PlayOneShot(gem, sv); }
    public void Hit() { fx.PlayOneShot(hit, sv); }
    public void Jump() { fx.PlayOneShot(jump, sv * .6f); }
    public void Pow() { fx.PlayOneShot(pow, sv); }
    public void Win() { fx.PlayOneShot(win, sv); }

    static AudioClip Tone(float f1, float f2, float d, int w)
    {
        int sr = 44100, n = (int)(sr * d); var a = new float[n]; double ph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n; ph += 2 * System.Math.PI * Mathf.Lerp(f1, f2, t) / sr;
            float s = w == 0 ? (float)System.Math.Sin(ph) : w == 1 ? (System.Math.Sin(ph) > 0 ? .5f : -.5f) : (float)((ph / (2 * System.Math.PI)) % 1.0) * 2 - 1;
            a[i] = s * (1 - t) * (1 - t) * .6f;
        }
        var c = AudioClip.Create("tone", n, 1, sr, false); c.SetData(a, 0); return c;
    }

    static AudioClip Music()
    {
        float[] m = { 262, 330, 392, 523, 392, 330, 294, 392 }; int sr = 44100, ns = (int)(sr * .38f);
        var a = new float[ns * m.Length];
        for (int k = 0; k < m.Length; k++)
            for (int i = 0; i < ns; i++)
                a[k * ns + i] = (float)System.Math.Sin(2 * System.Math.PI * m[k] * i / sr) * Mathf.Exp(-4f * i / ns) * .5f;
        var c = AudioClip.Create("music", a.Length, 1, sr, false); c.SetData(a, 0); return c;
    }
}
