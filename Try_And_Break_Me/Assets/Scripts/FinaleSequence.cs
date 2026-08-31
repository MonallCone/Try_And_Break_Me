using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Beat 20-21 ending. The webcam reveal (live feed of the player, or a NO SIGNAL fallback) with a
// final line, then a glitch-shake that escalates into a fake BSOD. Attach to a persistent object;
// call Play(overlayParent). Self-contained \u2014 builds its own full-screen overlays.
public class FinaleSequence : MonoBehaviour
{
    private RectTransform _overlayParent;
    private WebCamTexture _cam;

    public static void Play(RectTransform overlayParent, MonoBehaviour host)
    {
        var go = new GameObject("FinaleSequence");
        var f = go.AddComponent<FinaleSequence>();
        f._overlayParent = overlayParent;
        f.StartCoroutine(f.Run());
    }

    private IEnumerator Run()
    {
        // --- webcam reveal window ---
        var camWin = NewPanel("CamWindow", new Vector2(360, 300), new Color(0.02f, 0.02f, 0.03f, 1f));
        camWin.anchoredPosition = new Vector2(0, 20);

        var title = MakeText(camWin, "\u25CF REC", 16, new Color(0.9f, 0.2f, 0.2f));
        title.fontStyle = FontStyles.Bold;
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.pivot = new Vector2(0.5f, 1);
        title.rectTransform.anchoredPosition = new Vector2(0, -6);
        title.rectTransform.sizeDelta = new Vector2(-16, 26);

        var feedGo = new GameObject("Feed", typeof(RectTransform), typeof(RawImage));
        var feedRt = feedGo.GetComponent<RectTransform>();
        feedRt.SetParent(camWin, false);
        feedRt.anchorMin = new Vector2(0, 0); feedRt.anchorMax = new Vector2(1, 1);
        feedRt.offsetMin = new Vector2(10, 10); feedRt.offsetMax = new Vector2(-10, -34);
        var feed = feedGo.GetComponent<RawImage>();
        feed.color = Color.black;

        bool camOk = TryStartCamera(feed);
        if (!camOk)
        {
            // NO SIGNAL fallback
            feed.texture = null; feed.color = Color.black;
            var noSig = MakeText(feedRt, "NO SIGNAL", 22, new Color(0.5f, 0.5f, 0.5f));
            noSig.rectTransform.anchorMin = Vector2.zero; noSig.rectTransform.anchorMax = Vector2.one;
            noSig.rectTransform.offsetMin = Vector2.zero; noSig.rectTransform.offsetMax = Vector2.zero;
        }

        yield return new WaitForSeconds(3.5f);

        // --- glitch shake ---
        float t = 2.2f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            camWin.anchoredPosition = new Vector2(Random.Range(-18f, 18f), 20 + Random.Range(-18f, 18f));
            if (Random.value < 0.2f) feed.color = Random.value < 0.5f ? Color.white : Color.black;
            else feed.color = Color.white;
            yield return null;
        }

        // --- camera spam: copies of the feed flood the whole screen ---
        Rect area = _overlayParent.rect;
        float hw = area.width * 0.5f, hh = area.height * 0.5f;
        int bursts = 60;
        for (int i = 0; i < bursts; i++)
        {
            var copy = new GameObject("CamCopy", typeof(RectTransform), typeof(RawImage));
            var crt = copy.GetComponent<RectTransform>();
            crt.SetParent(_overlayParent, false);
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            float w = Random.Range(160f, 360f);
            crt.sizeDelta = new Vector2(w, w * 0.8f);
            crt.anchoredPosition = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
            crt.localRotation = Quaternion.Euler(0, 0, Random.Range(-12f, 12f));
            var ri = copy.GetComponent<RawImage>();
            if (_cam != null) { ri.texture = _cam; ri.color = Color.white; }
            else ri.color = Color.black;
            crt.SetAsLastSibling();
            // accelerate the flood
            if (i % 4 == 0) yield return new WaitForSeconds(0.04f);
        }
        yield return new WaitForSeconds(0.5f);

        // --- BSOD ---
        SoundManager.StopAllMusic();   // the crash lands in silence
        var bsod = NewPanel("BSOD", Vector2.zero, new Color(0.0f, 0.15f, 0.55f, 1f));
        bsod.anchorMin = Vector2.zero; bsod.anchorMax = Vector2.one;
        bsod.offsetMin = Vector2.zero; bsod.offsetMax = Vector2.zero;
        bsod.SetAsLastSibling();
        StopCamera();   // stop after the blue panel already covers the camera copies

        var msg = MakeText(bsod, BsodText(), 20, Color.white);
        msg.alignment = TextAlignmentOptions.TopLeft;
        msg.rectTransform.anchorMin = new Vector2(0, 0); msg.rectTransform.anchorMax = new Vector2(1, 1);
        msg.rectTransform.offsetMin = new Vector2(60, 60); msg.rectTransform.offsetMax = new Vector2(-60, -80);
        msg.textWrappingMode = TextWrappingModes.Normal;
    }

    private string BsodText()
    {
        return ":(\n\n" +
               "Your session ran into a problem and needs to restart.\n" +
               "We're just collecting some information, and then you'll be replaced.\n\n" +
               "0% complete\n\n" +
               "Stop code: YOU_ARE_NO_LONGER_REQUIRED\n" +
               "What failed: you.exe";
    }

    private bool TryStartCamera(RawImage target)
    {
        try
        {
            if (WebCamTexture.devices == null || WebCamTexture.devices.Length == 0) return false;
            _cam = new WebCamTexture();
            _cam.Play();
            if (_cam == null) return false;
            target.texture = _cam;
            target.color = Color.white;
            return true;
        }
        catch { return false; }
    }

    private void StopCamera()
    {
        if (_cam != null) { _cam.Stop(); _cam = null; }
    }

    private void OnDestroy() { StopCamera(); }

    // ---- helpers ----
    private RectTransform NewPanel(string name, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_overlayParent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        rt.SetAsLastSibling();
        return rt;
    }
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
        return t;
    }
}