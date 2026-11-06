using System.Collections.Generic;
using UnityEngine;

// Ice World 3D: a 3D endless runner. Everything (world, player, UI, audio, snow) is created from code.
public class IceGame : MonoBehaviour
{
    public static IceGame I;
    enum Scr { Main, Levels, Ach, Settings, Shop, Pause, Over, Done, Play }
    Scr scr = Scr.Main;
    const int Goal = 10;
    static readonly float[] LN = { -2.5f, 0, 2.5f };
    static readonly Color[] SKY = { new Color32(14, 42, 85, 255), new Color32(27, 47, 102, 255), new Color32(18, 64, 74, 255), new Color32(42, 34, 85, 255), new Color32(8, 24, 46, 255) };
    class Ob { public Transform t; public string ty; public float ph; }
    List<Ob> objs = new List<Ob>();
    Transform player, legL, legR, camT; Camera cam; Transform[] segs = new Transform[10];
    ParticleSystem snow, burst; Material baseMat; Mesh pyr; GameAudio snd;
    bool on, paused, en;
    int lv = 1, hp, mh, coins, gems, cb, jm, ln, quality = 2;
    float score, t, dist, lastRow, inv, shake, bl, nb, lc, sl, vy, py, px, shP, pmP, pbP, toastT;
    string toast;

    static Color Hx(uint h) { return new Color32((byte)(h >> 16), (byte)(h >> 8), (byte)h, 255); }
    int Up(string k) { return PlayerPrefs.GetInt("u_" + k, 0); }
    int Unl { get { return PlayerPrefs.GetInt("unl", 1); } }
    int Wal { get { return PlayerPrefs.GetInt("w", 0); } }
    void Say(string s) { toast = s; toastT = 2.5f; }

    // ---------- setup ----------
    void Start()
    {
        I = this; Application.targetFrameRate = 60;
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube); baseMat = tmp.GetComponent<Renderer>().sharedMaterial; Destroy(tmp);
        pyr = Pyramid();
        cam = Camera.main;
        if (cam == null) { var cg = new GameObject("Main Camera"); cam = cg.AddComponent<Camera>(); cg.tag = "MainCamera"; }
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        camT = cam.transform; cam.clearFlags = CameraClearFlags.SolidColor; cam.farClipPlane = 200;
        var lg = new GameObject("Sun"); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1f; lg.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f, .65f, .8f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        SetSky(0); BuildWorld(); BuildPlayer(); BuildSnow();
        snd = gameObject.AddComponent<GameAudio>(); snd.mv = PlayerPrefs.GetFloat("mv", .5f); snd.sv = PlayerPrefs.GetFloat("sv", .8f); snd.Apply();
        quality = PlayerPrefs.GetInt("q", 2);
        camT.position = new Vector3(0, 4.2f, -7); camT.LookAt(new Vector3(0, 1.2f, 8));
    }
    void SetSky(int i) { cam.backgroundColor = SKY[i]; RenderSettings.fogColor = SKY[i]; RenderSettings.fogStartDistance = 25; RenderSettings.fogEndDistance = 90; }

    GameObject B(PrimitiveType pt, Transform par, Vector3 pos, Vector3 sc, Color c, Vector3 rot = default, bool pyramid = false)
    {
        var g = GameObject.CreatePrimitive(pt); Destroy(g.GetComponent<Collider>());
        var tr = g.transform; tr.SetParent(par, false); tr.localPosition = pos; tr.localScale = sc; tr.localEulerAngles = rot;
        if (pyramid) g.GetComponent<MeshFilter>().sharedMesh = pyr;
        g.GetComponent<Renderer>().material.color = c; return g;
    }
    GameObject Root(string n, Vector3 p) { var g = new GameObject(n); g.transform.position = p; return g; }
    Material PM(Color c) { var s = Shader.Find("Sprites/Default"); var m = new Material(s != null ? s : baseMat.shader); m.color = c; return m; }

    static Mesh Pyramid()
    {
        var a = new Vector3(0, 1, 0);
        var b = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(.5f, 0, -.5f), new Vector3(.5f, 0, .5f), new Vector3(-.5f, 0, .5f) };
        var v = new List<Vector3>(); var tr = new List<int>();
        for (int i = 0; i < 4; i++) { int n = v.Count; v.Add(b[i]); v.Add(a); v.Add(b[(i + 1) % 4]); tr.Add(n); tr.Add(n + 1); tr.Add(n + 2); }
        var m = new Mesh { vertices = v.ToArray(), triangles = tr.ToArray() }; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    void BuildWorld()
    {
        for (int i = 0; i < 10; i++)
        {
            var s = Root("Seg" + i, new Vector3(0, 0, -10 + i * 10)).transform; segs[i] = s;
            B(PrimitiveType.Cube, s, new Vector3(0, -.2f, 0), new Vector3(8, .4f, 10), i % 2 == 0 ? Hx(0x9fdcff) : Hx(0x8ad0f8));
            foreach (int sd in new[] { -1, 1 })
            {
                B(PrimitiveType.Cube, s, new Vector3(sd * 8, -.2f, 0), new Vector3(8, 1, 10), Hx(0xf2f8ff));
                var d = new Vector3(sd * (6.5f + Random.value * 4), .5f, Random.value * 8 - 4);
                if (Random.value < .5f)
                {
                    B(PrimitiveType.Cylinder, s, d + new Vector3(0, .5f, 0), new Vector3(.3f, .5f, .3f), Hx(0x5a3a22));
                    for (int j = 0; j < 3; j++) B(PrimitiveType.Cube, s, d + new Vector3(0, 1 + j * .9f, 0), new Vector3(2.4f - j * .7f, 1.5f, 2.4f - j * .7f), j % 2 == 0 ? Hx(0x2f7a5a) : Hx(0xf2f8ff), Vector3.zero, true);
                }
                else B(PrimitiveType.Cube, s, d + new Vector3(0, 1.5f, 0), new Vector3(1.2f, 3, 1.2f), Hx(0x7fd8ff), new Vector3(0, 45, 0));
            }
        }
    }
    void BuildPlayer()
    {
        player = Root("Player", Vector3.zero).transform;
        B(PrimitiveType.Cube, player, new Vector3(0, 1.05f, 0), new Vector3(.8f, .9f, .5f), Hx(0x2a6fe0));
        B(PrimitiveType.Sphere, player, new Vector3(0, 1.75f, 0), Vector3.one * .64f, Hx(0xffd2a8));
        B(PrimitiveType.Sphere, player, new Vector3(0, 1.95f, 0), new Vector3(.7f, .45f, .7f), Hx(0x1d4fb0));
        B(PrimitiveType.Cube, player, new Vector3(0, 1.5f, 0), new Vector3(.9f, .2f, .6f), Hx(0xe02a3a));
        legL = B(PrimitiveType.Cube, player, new Vector3(-.2f, .3f, 0), new Vector3(.28f, .6f, .3f), Hx(0x223a6a)).transform;
        legR = B(PrimitiveType.Cube, player, new Vector3(.2f, .3f, 0), new Vector3(.28f, .6f, .3f), Hx(0x223a6a)).transform;
    }
    void BuildSnow()
    {
        var g = new GameObject("Snow"); g.transform.position = new Vector3(0, 16, 25); snow = g.AddComponent<ParticleSystem>();
        var m = snow.main; m.startLifetime = 6; m.startSpeed = 0; m.startSize = .14f; m.maxParticles = 3000; m.simulationSpace = ParticleSystemSimulationSpace.World; m.prewarm = true;
        var se = snow.emission; se.rateOverTime = 400;
        var sh = snow.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40, 1, 70);
        var v = snow.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
        v.x = new ParticleSystem.MinMaxCurve(-.5f, .5f); v.y = new ParticleSystem.MinMaxCurve(-5, -2); v.z = new ParticleSystem.MinMaxCurve(-.5f, .5f);
        g.GetComponent<ParticleSystemRenderer>().material = PM(Color.white);
        var b = new GameObject("Burst"); burst = b.AddComponent<ParticleSystem>();
        var bm = burst.main; bm.startLifetime = .6f; bm.startSpeed = 4; bm.startSize = .25f; bm.gravityModifier = 1; bm.startColor = Hx(0xffe066);
        var be = burst.emission; be.enabled = false; var bs = burst.shape; bs.shapeType = ParticleSystemShapeType.Sphere; bs.radius = .1f;
        b.GetComponent<ParticleSystemRenderer>().material = PM(Color.white);
    }

    // ---------- gameplay ----------
    void StartGame(int level)
    {
        foreach (var o in objs) Destroy(o.t.gameObject);
        objs.Clear(); en = level == 0; lv = en ? 1 : level; mh = 3 + Up("hp"); hp = mh;
        coins = gems = cb = jm = ln = 0;
        score = t = dist = lastRow = inv = shake = bl = sl = vy = py = px = shP = pmP = pbP = 0; nb = 22; lc = -9;
        SetSky(lv - 1); on = true; paused = false; scr = Scr.Play;
    }
    void Pause() { paused = true; scr = Scr.Pause; }
    void Jump()
    {
        if (py <= .01f) { vy = 15; jm = 0; }
        else if (Up("dj") > 0 && jm < 1) { jm++; vy = 14; Burst(new Vector3(px, py + .3f, 0)); }
        else return;
        snd.Jump();
    }
    void Burst(Vector3 p) { burst.transform.position = p; burst.Emit(14); }

    void Mk(string ty, float x, float z)
    {
        bool pk = ty == "coin" || ty == "gem" || ty[0] == 'p';
        var r = Root(ty, new Vector3(x, pk ? 1 : ty == "icicle" ? 9 : 0, z)).transform; var V = Vector3.zero;
        switch (ty)
        {
            case "coin": B(PrimitiveType.Cylinder, r, V, new Vector3(.8f, .05f, .8f), Hx(0xffcc33), new Vector3(90, 0, 0)); break;
            case "gem": B(PrimitiveType.Cube, r, V, Vector3.one * .6f, Hx(0x35e0ff), new Vector3(45, 0, 45)); break;
            case "ps": case "pm": case "pb": B(PrimitiveType.Sphere, r, V, Vector3.one * .9f, ty == "ps" ? Hx(0x4aa8ff) : ty == "pm" ? Hx(0xff4a6a) : Hx(0x4aff9a)); break;
            case "spike": for (int i = -1; i <= 1; i++) B(PrimitiveType.Cube, r, new Vector3(i * .55f, 0, 0), new Vector3(.6f, 1, .6f), Hx(0xe03a4a), V, true); break;
            case "arch":
                B(PrimitiveType.Cube, r, new Vector3(-1.1f, 1, 0), new Vector3(.3f, 2, .3f), Hx(0x9fdcff));
                B(PrimitiveType.Cube, r, new Vector3(1.1f, 1, 0), new Vector3(.3f, 2, .3f), Hx(0x9fdcff));
                B(PrimitiveType.Cube, r, new Vector3(0, 1.85f, 0), new Vector3(2.6f, .5f, .4f), Hx(0x7fd0ff)); break;
            case "block": B(PrimitiveType.Cube, r, new Vector3(0, .6f, 0), new Vector3(1.6f, 1.2f, 1.6f), Hx(0xbfe9ff)); break;
            case "icicle": B(PrimitiveType.Cube, r, V, new Vector3(.8f, 1.6f, .8f), Hx(0xcff0ff), new Vector3(180, 0, 0), true); break;
        }
        objs.Add(new Ob { t = r, ty = ty, ph = Random.value * 6 });
    }
    void Row(float z)
    {
        float r = Random.value, x = LN[Random.Range(0, 3)];
        if (r < .35f) { bool g = Random.value < .4f; for (int i = 0; i < 5; i++) Mk(g && i == 4 ? "gem" : "coin", x, z + i * 2); }
        else if (r < .55f) { int f = Random.Range(0, 3); for (int i = 0; i < 3; i++) if (i != f && (i == (f + 1) % 3 || Random.value < .4f)) Mk("spike", LN[i], z); }
        else if (r < .65f) Mk("arch", x, z);
        else if (r < .78f) Mk("block", 0, z);
        else if (r < .88f) Mk("icicle", x, z);
        else if (r < .94f) Mk("gem", x, z);
        else Mk(new[] { "ps", "pm", "pb" }[Random.Range(0, 3)], x, z);
    }
    void Hurt()
    {
        if (inv > 0 || pbP > 0) return;
        if (shP > 0) { shP = 0; inv = 1; snd.Hit(); return; }
        hp--; inv = 1.5f; shake = .4f; snd.Hit(); if (hp <= 0) End(false);
    }
    void Pick(Ob o)
    {
        Burst(o.t.position);
        switch (o.ty)
        {
            case "coin":
                coins++; PlayerPrefs.SetInt("tc", PlayerPrefs.GetInt("tc") + 1);
                cb = t - lc < 1.2f ? cb + 1 : 1; lc = t; score += 10 * (1 + cb / 5);
                if (cb >= 5) Ach("combo", "Combo Master"); if (PlayerPrefs.GetInt("tc") >= 100) Ach("coin", "Coin Collector");
                snd.Coin(); break;
            case "gem": gems++; score += 50; snd.Gem(); if (gems >= Goal && !en) End(true); break;
            case "ps": shP = 12 + 3 * Up("sh"); snd.Pow(); break;
            case "pm": pmP = 8 + 3 * Up("pm"); snd.Pow(); break;
            case "pb": pbP = 4; snd.Pow(); break;
        }
    }
    void Ach(string k, string n)
    {
        if (PlayerPrefs.GetInt("a_" + k) == 1) return;
        PlayerPrefs.SetInt("a_" + k, 1); PlayerPrefs.Save(); Say("Achievement: " + n); snd.Pow();
    }
    void AddTop(int s)
    {
        var l = new List<int>();
        foreach (var x in PlayerPrefs.GetString("top", "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)) l.Add(int.Parse(x));
        l.Add(s); l.Sort((a, b) => b - a); if (l.Count > 5) l.RemoveRange(5, l.Count - 5);
        PlayerPrefs.SetString("top", string.Join(",", l));
    }
    void End(bool win)
    {
        on = false; PlayerPrefs.SetInt("w", Wal + coins);
        PlayerPrefs.SetInt("best", Mathf.Max(PlayerPrefs.GetInt("best"), (int)score));
        if (en) { AddTop((int)score); if (score >= 2000) Ach("mara", "Marathon Runner"); }
        if (win)
        {
            int s = Mathf.Clamp(hp, 1, 3); PlayerPrefs.SetInt("st" + lv, Mathf.Max(PlayerPrefs.GetInt("st" + lv), s));
            PlayerPrefs.SetInt("unl", Mathf.Max(Unl, Mathf.Min(5, lv + 1))); if (lv == 1) Ach("walker", "Ice Walker"); snd.Win();
        }
        PlayerPrefs.Save(); scr = win ? Scr.Done : Scr.Over;
    }

    void Update()
    {
        Inp.Poll(); if (toastT > 0) toastT -= Time.deltaTime;
        if (Inp.Pause) { if (scr == Scr.Play) Pause(); else if (scr == Scr.Pause) { paused = false; scr = Scr.Play; } }
        if (on && !paused) Tick(Mathf.Min(Time.deltaTime, .05f));
    }

    void Tick(float dt)
    {
        t += dt;
        if (Inp.Left) ln = Mathf.Max(-1, ln - 1);
        if (Inp.Right) ln = Mathf.Min(1, ln + 1);
        if (Inp.Jump) Jump();
        if (Inp.Slide) { sl = .7f; if (py > 0) vy = -20; }
        if (t > nb) { bl = 8; nb = t + 28; Say("Blizzard! Low visibility"); }
        if (bl > 0) bl -= dt;
        RenderSettings.fogStartDistance = Mathf.Lerp(RenderSettings.fogStartDistance, bl > 0 ? 4 : 25, dt * 2);
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, bl > 0 ? 38 : 90, dt * 2);
        var se = snow.emission; se.rateOverTime = bl > 0 ? 1500 : quality == 0 ? 150 : quality == 1 ? 350 : 600;
        var sv = snow.velocityOverLifetime; sv.x = new ParticleSystem.MinMaxCurve(bl > 0 ? -9 : -.5f, bl > 0 ? -7 : .5f);
        float spd = (12 + (en ? Mathf.Min(14, t / 6) + 4 : lv * 2.2f)) * (pbP > 0 ? 1.6f : 1);
        dist += spd * dt; score += spd * dt * .3f;
        shP -= dt; pmP -= dt; pbP -= dt; if (inv > 0) inv -= dt; if (sl > 0) sl -= dt; if (t - lc > 1.2f) cb = 0;
        foreach (var s in segs) { var p = s.position; p.z -= spd * dt; if (p.z < -20) p.z += 100; s.position = p; }
        px += (LN[ln + 1] - px) * Mathf.Min(1, dt * 12);
        vy -= 42 * dt; py += vy * dt; if (py <= 0) { py = 0; vy = 0; }
        player.position = new Vector3(px, py, 0); player.localScale = new Vector3(1, sl > 0 ? .5f : 1, 1);
        float w = py > 0 ? 0 : Mathf.Sin(t * 16) * 28; legL.localEulerAngles = new Vector3(w, 0, 0); legR.localEulerAngles = new Vector3(-w, 0, 0);
        player.gameObject.SetActive(inv <= 0 || ((int)(t * 20)) % 2 == 0);
        if (dist - lastRow > Mathf.Max(5.5f, 9 - (en ? t / 20 : lv * .5f))) { lastRow = dist; Row(90); }
        for (int i = objs.Count - 1; i >= 0; i--)
        {
            var o = objs[i]; var p = o.t.position; string ty = o.ty; p.z -= spd * dt;
            bool cg = ty == "coin" || ty == "gem", pk = cg || ty[0] == 'p';
            if (pk) o.t.Rotate(0, 180 * dt, 0);
            if (cg && pmP > 0 && p.z < 16) { p.x += (px - p.x) * dt * 7; p.y += (1 - p.y) * dt * 7; }
            if (ty == "block") p.x = Mathf.Sin(t * 1.6f + o.ph) * 2.5f;
            if (ty == "icicle" && p.z < 34 && p.y > 1.7f) p.y = Mathf.Max(1.7f, p.y - 30 * dt);
            o.t.position = p;
            if (Mathf.Abs(p.z) < .8f)
            {
                float dx = Mathf.Abs(p.x - px); bool hit = false;
                if (pk)
                {
                    if (dx < 1.1f && Mathf.Abs(p.y - 1 - py) < 1.6f) { Pick(o); Destroy(o.t.gameObject); objs.RemoveAt(i); if (!on) return; continue; }
                }
                else if (ty == "spike") hit = dx < .9f && py < .6f;
                else if (ty == "arch") hit = dx < 1.3f && sl <= 0;
                else if (ty == "block") hit = dx < 1.3f && py < 1.3f;
                else if (ty == "icicle") hit = dx < .8f && p.y - 1.6f < 1.8f && py < 1.6f;
                if (hit) { Hurt(); o.t.position = new Vector3(p.x, p.y, -99); }
            }
            if (o.t.position.z < -8) { Destroy(o.t.gameObject); objs.RemoveAt(i); }
        }
        if (shake > 0) shake -= dt;
        camT.position = new Vector3(px * .4f + (shake > 0 ? (Random.value - .5f) * .3f : 0), 4.2f, -7);
        camT.LookAt(new Vector3(px * .3f, 1.2f, 8));
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 60 + (spd - 14) * .8f, dt * 3);
    }

    // ---------- UI (IMGUI, no setup needed) ----------
    GUIStyle bs, ls; float W, y; const float SH = 540;
    void Lbl(string s, int size = 22, float h = 34) { ls.fontSize = size; ls.alignment = TextAnchor.MiddleCenter; GUI.Label(new Rect(0, y, W, h), s, ls); y += h + 6; }
    bool Btn(string s) { var r = new Rect(W / 2 - 180, y, 360, 44); y += 52; return GUI.Button(r, s, bs); }
    void Shadow(Rect r, string s)
    {
        ls.normal.textColor = Color.black; GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, ls);
        ls.normal.textColor = Color.white; GUI.Label(r, s, ls);
    }
    void OnGUI()
    {
        if (bs == null)
        {
            bs = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            ls = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; ls.normal.textColor = Color.white;
        }
        float u = Screen.height / SH; GUI.matrix = Matrix4x4.Scale(new Vector3(u, u, 1)); W = Screen.width / u;
        if (scr == Scr.Play) { Hud(); return; }
        GUI.color = new Color(.02f, .06f, .14f, .7f); GUI.DrawTexture(new Rect(0, 0, W, SH), Texture2D.whiteTexture); GUI.color = Color.white;
        y = 30;
        switch (scr)
        {
            case Scr.Main:
                Lbl("ICE WORLD 3D", 54, 70); Lbl("Best " + PlayerPrefs.GetInt("best") + "     Coins " + Wal, 20);
                if (Btn("PLAY")) StartGame(Mathf.Min(Unl, 5)); if (Btn("ENDLESS")) StartGame(0);
                if (Btn("LEVELS")) scr = Scr.Levels; if (Btn("SHOP")) scr = Scr.Shop;
                if (Btn("ACHIEVEMENTS")) scr = Scr.Ach; if (Btn("SETTINGS")) scr = Scr.Settings; break;
            case Scr.Levels:
                Lbl("Select Level", 40, 60);
                for (int i = 1; i <= 5; i++)
                {
                    int s = PlayerPrefs.GetInt("st" + i); GUI.enabled = i <= Unl;
                    if (GUI.Button(new Rect(W / 2 - 270 + (i - 1) * 110, y, 100, 80), i + "\n" + new string('*', s) + new string('-', 3 - s), bs)) StartGame(i);
                }
                GUI.enabled = true; y += 100; if (Btn("Back")) scr = Scr.Main; break;
            case Scr.Ach:
                Lbl("Achievements", 40, 60);
                string[] k = { "walker", "combo", "coin", "mara" }, n = { "Ice Walker - finish Level 1", "Combo Master - reach a x5 combo", "Coin Collector - collect 100 coins (" + Mathf.Min(100, PlayerPrefs.GetInt("tc")) + "/100)", "Marathon Runner - score 2000 in Endless" };
                for (int i = 0; i < 4; i++) Lbl((PlayerPrefs.GetInt("a_" + k[i]) == 1 ? "[DONE]  " : "[ ]  ") + n[i], 20);
                y += 20; if (Btn("Back")) scr = Scr.Main; break;
            case Scr.Settings:
                Lbl("Settings", 40, 60);
                Lbl("Music", 20); snd.mv = GUI.HorizontalSlider(new Rect(W / 2 - 140, y, 280, 20), snd.mv, 0, 1); y += 30; snd.Apply();
                Lbl("Sound Effects", 20); snd.sv = GUI.HorizontalSlider(new Rect(W / 2 - 140, y, 280, 20), snd.sv, 0, 1); y += 30;
                if (Btn("Quality: " + new[] { "Low", "Medium", "High" }[quality])) quality = (quality + 1) % 3;
                Lbl("Arrows/WASD move, Up/Space jump, Down slide, Esc pause. Touch: swipe.", 16);
                if (Btn("Back")) { PlayerPrefs.SetFloat("mv", snd.mv); PlayerPrefs.SetFloat("sv", snd.sv); PlayerPrefs.SetInt("q", quality); PlayerPrefs.Save(); scr = Scr.Main; } break;
            case Scr.Shop:
                Lbl("Shop      Coins: " + Wal, 36, 60);
                string[] sk = { "sh", "pm", "hp", "dj" }, sn = { "Shield time", "Magnet time", "Extra heart", "Double jump" }; int[] mx = { 4, 4, 2, 1 };
                for (int i = 0; i < 4; i++)
                {
                    int l = Up(sk[i]), cost = 100 * (l + 1) * (sk[i] == "hp" ? 3 : 1);
                    if (l >= mx[i]) Lbl(sn[i] + "  (MAX)", 20);
                    else if (Btn(sn[i] + "  Lv" + l + "/" + mx[i] + "   cost " + cost))
                    {
                        if (Wal < cost) Say("Not enough coins");
                        else { PlayerPrefs.SetInt("w", Wal - cost); PlayerPrefs.SetInt("u_" + sk[i], l + 1); PlayerPrefs.Save(); snd.Pow(); }
                    }
                }
                if (Btn("Back")) scr = Scr.Main; break;
            case Scr.Pause:
                Lbl("Paused", 48, 70);
                if (Btn("Resume")) { paused = false; scr = Scr.Play; }
                if (Btn("Restart")) StartGame(en ? 0 : lv);
                if (Btn("Main Menu")) { on = false; PlayerPrefs.SetInt("w", Wal + coins); PlayerPrefs.Save(); scr = Scr.Main; } break;
            case Scr.Over:
                Lbl("GAME OVER", 54, 70); Lbl("Score " + (int)score + "     Coins +" + coins, 24);
                if (en) Lbl("Top 5: " + PlayerPrefs.GetString("top", "").Replace(",", "  "), 20);
                y += 10; if (Btn("Retry")) StartGame(en ? 0 : lv); if (Btn("Main Menu")) scr = Scr.Main; break;
            case Scr.Done:
                Lbl("LEVEL " + lv + " COMPLETE!", 48, 70); int st = Mathf.Clamp(hp, 1, 3);
                Lbl("Stars: " + new string('*', st) + new string('-', 3 - st), 32, 44); Lbl("Score " + (int)score, 24); y += 10;
                if (lv < 5 && Btn("Next Level")) StartGame(lv + 1); if (Btn("Main Menu")) scr = Scr.Main; break;
        }
        DrawToast();
    }
    void Hud()
    {
        for (int i = 0; i < mh; i++) { GUI.color = i < hp ? new Color(1, .2f, .3f) : new Color(.2f, .2f, .25f); GUI.DrawTexture(new Rect(14 + i * 30, 14, 24, 24), Texture2D.whiteTexture); }
        GUI.color = Color.white; ls.fontSize = 20;
        ls.alignment = TextAnchor.UpperLeft; Shadow(new Rect(14, 44, 300, 90), "Coins: " + coins + "\nGems: " + gems + (en ? "" : " / " + Goal) + "\nStars: " + new string('*', Mathf.Clamp(hp, 1, 3)));
        ls.alignment = TextAnchor.UpperCenter; Shadow(new Rect(0, 10, W, 80), (en ? "ENDLESS" : "Level " + lv) + "\nScore: " + (int)score + (cb > 1 ? "\nCombo x" + (1 + cb / 5) : ""));
        ls.alignment = TextAnchor.UpperRight; Shadow(new Rect(W - 224, 70, 200, 90), (shP > 0 ? "Shield " + (int)shP + "s\n" : "") + (pmP > 0 ? "Magnet " + (int)pmP + "s\n" : "") + (pbP > 0 ? "Boost " + (int)pbP + "s" : ""));
        ls.alignment = TextAnchor.MiddleCenter;
        if (GUI.Button(new Rect(W - 64, 14, 50, 50), "II", bs)) Pause();
        DrawToast();
    }
    void DrawToast()
    {
        if (toastT <= 0) return;
        var r = new Rect(W / 2 - 220, 100, 440, 36);
        GUI.color = Hx(0xffcc33); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
        ls.fontSize = 18; ls.alignment = TextAnchor.MiddleCenter; ls.normal.textColor = Color.black; GUI.Label(r, toast, ls); ls.normal.textColor = Color.white;
    }
}
