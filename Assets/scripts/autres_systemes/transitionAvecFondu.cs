// ============================================================
// transitionAvecFondu.cs
// ------------------------------------------------------------
// Transition simple entre scenes avec fondu noir cree au
// runtime. AUCUN SETUP UNITY EDITOR REQUIS : pas de prefab,
// pas de GameObject a placer dans la scene, pas de Canvas a
// configurer. Tout est genere par code au moment de l'appel.
//
// USAGE BASIQUE (fondu noir seul) :
//   transitionAvecFondu.ChargerSceneAvecFondu("scene2_usine");
//
// USAGE AVEC VIDEO DE CHARGEMENT (style cinematique) :
//   transitionAvecFondu.ChargerSceneAvecVideo(
//       "scene2_usine", clipVideoChargement);
//
// Le VideoClip est joue en plein ecran pendant le chargement
// de la scene cible. Le clip est joue en loop si la scene
// charge plus vite que la duree du clip. La scene s'affiche
// quand le clip a joue au moins une fois (ou minDureeAffichage).
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class transitionAvecFondu : MonoBehaviour
{
    /// <summary>
    /// Charge une scene avec un fondu noir simple (sans video).
    /// </summary>
    public static void ChargerSceneAvecFondu(
        string sceneCible,
        float dureeFadeIn = 0.5f,
        float dureeFadeOut = 0.5f,
        float attenteApresChargement = 0.2f,
        float delaiAvantTransition = 0f)
    {
        if (!PreparerInstance(sceneCible, out var instance)) return;
        instance.StartCoroutine(instance.AttendrePuisFondue(
            sceneCible, dureeFadeIn, dureeFadeOut,
            attenteApresChargement, delaiAvantTransition));
    }

    /// <summary>
    /// Charge une scene avec une video de chargement en plein
    /// ecran. La video est jouee en loop pendant le chargement.
    /// La scene cible apparait apres minDureeAffichage secondes
    /// (ou plus si le chargement est plus long).
    /// </summary>
    public static void ChargerSceneAvecVideo(
        string sceneCible,
        VideoClip clipVideo,
        float minDureeAffichage = 2f,
        float dureeFadeIn = 0.4f,
        float dureeFadeOut = 0.4f,
        float delaiAvantTransition = 0f)
    {
        if (clipVideo == null)
        {
            Debug.LogWarning("[TransitionFondu] clipVideo null, " +
                "fallback fondu noir simple.");
            ChargerSceneAvecFondu(sceneCible, dureeFadeIn, dureeFadeOut,
                0.2f, delaiAvantTransition);
            return;
        }

        if (!PreparerInstance(sceneCible, out var instance)) return;
        instance.StartCoroutine(instance.AttendrePuisVideo(
            sceneCible, clipVideo, minDureeAffichage,
            dureeFadeIn, dureeFadeOut, delaiAvantTransition));
    }

    private IEnumerator AttendrePuisFondue(
        string sceneCible, float dureeFadeIn, float dureeFadeOut,
        float attenteApresChargement, float delaiAvant)
    {
        if (delaiAvant > 0f)
            yield return new WaitForSecondsRealtime(delaiAvant);
        yield return FondueEtCharger(sceneCible, dureeFadeIn,
            dureeFadeOut, attenteApresChargement);
    }

    private IEnumerator AttendrePuisVideo(
        string sceneCible, VideoClip clipVideo,
        float minDureeAffichage, float dureeFadeIn,
        float dureeFadeOut, float delaiAvant)
    {
        if (delaiAvant > 0f)
            yield return new WaitForSecondsRealtime(delaiAvant);
        yield return VideoEtCharger(sceneCible, clipVideo,
            minDureeAffichage, dureeFadeIn, dureeFadeOut);
    }

    private static bool PreparerInstance(
        string sceneCible,
        out transitionAvecFondu instance)
    {
        instance = null;
        if (string.IsNullOrEmpty(sceneCible))
        {
            Debug.LogError("[TransitionFondu] sceneCible vide !");
            return false;
        }

        var existante = FindFirstObjectByType<transitionAvecFondu>();
        if (existante != null)
        {
            Debug.LogWarning("[TransitionFondu] Une transition est " +
                "deja en cours, appel ignore.");
            return false;
        }

        var go = new GameObject("_transitionAvecFondu_runtime");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<transitionAvecFondu>();
        return true;
    }

    // ============================================================
    // FONDU NOIR SIMPLE
    // ============================================================

    private IEnumerator FondueEtCharger(
        string sceneCible, float dureeFadeIn,
        float dureeFadeOut, float attenteApresChargement)
    {
        Debug.Log($"[TransitionFondu] Demarrage vers '{sceneCible}'.");

        var image = CreerImageNoirePleinEcran();

        // Fade IN
        yield return Fade(image, 0f, 1f, dureeFadeIn);

        // Chargement asynchrone
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneCible);
        while (op != null && !op.isDone) yield return null;

        yield return new WaitForSecondsRealtime(attenteApresChargement);

        // Fade OUT
        yield return Fade(image, 1f, 0f, dureeFadeOut);

        Debug.Log("[TransitionFondu] Transition fondue terminee.");
        Destroy(gameObject);
    }

    // ============================================================
    // VIDEO DE CHARGEMENT EN PLEIN ECRAN
    // ============================================================

    private IEnumerator VideoEtCharger(
        string sceneCible, VideoClip clipVideo,
        float minDureeAffichage, float dureeFadeIn,
        float dureeFadeOut)
    {
        Debug.Log($"[TransitionFondu] Demarrage video '{clipVideo.name}' " +
            $"vers '{sceneCible}'.");

        // 1. Image noire pour le fade
        var image = CreerImageNoirePleinEcran();

        // 2. Fade IN noir
        yield return Fade(image, 0f, 1f, dureeFadeIn);

        // 3. Setup du VideoPlayer + RenderTexture + RawImage
        //    par-dessus l'image noire
        int largeur = (int)clipVideo.width;
        int hauteur = (int)clipVideo.height;
        if (largeur <= 0) largeur = 1920;
        if (hauteur <= 0) hauteur = 1080;

        var renderTexture = new RenderTexture(largeur, hauteur, 0);
        renderTexture.Create();

        var videoGo = new GameObject("VideoPlayerLoading");
        videoGo.transform.SetParent(transform, false);
        var videoPlayer = videoGo.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.clip = clipVideo;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.SetDirectAudioMute(0, true);

        var rawImageGo = new GameObject("VideoRawImage");
        rawImageGo.transform.SetParent(image.transform.parent, false);
        var rawImage = rawImageGo.AddComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.raycastTarget = true;

        var rtRaw = rawImage.rectTransform;
        rtRaw.anchorMin = Vector2.zero;
        rtRaw.anchorMax = Vector2.one;
        rtRaw.offsetMin = Vector2.zero;
        rtRaw.offsetMax = Vector2.zero;

        // 4. Demarrer la video
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        videoPlayer.Play();

        // 5. Chargement asynchrone de la scene (en parallele)
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneCible);
        op.allowSceneActivation = false;

        float tempsAffichage = 0f;
        while (op.progress < 0.9f || tempsAffichage < minDureeAffichage)
        {
            tempsAffichage += Time.unscaledDeltaTime;
            yield return null;
        }

        // 6. Activer la nouvelle scene
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // Petit delai pour que la nouvelle scene s'initialise
        yield return new WaitForSecondsRealtime(0.2f);

        // 7. Stopper la video et fade out
        videoPlayer.Stop();
        Destroy(rawImageGo);

        yield return Fade(image, 1f, 0f, dureeFadeOut);

        // 8. Cleanup
        if (renderTexture != null) renderTexture.Release();

        Debug.Log("[TransitionFondu] Transition video terminee.");
        Destroy(gameObject);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private Image CreerImageNoirePleinEcran()
    {
        var canvasGo = new GameObject("CanvasFonduNoir");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("ImageNoire");
        imageGo.transform.SetParent(canvas.transform, false);

        var image = imageGo.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0);
        image.raycastTarget = true;

        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return image;
    }

    private IEnumerator Fade(Image image, float alphaDebut, float alphaFin, float duree)
    {
        if (duree <= 0f)
        {
            image.color = new Color(0, 0, 0, alphaFin);
            yield break;
        }

        float t = 0f;
        while (t < duree)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(alphaDebut, alphaFin,
                Mathf.Clamp01(t / duree));
            image.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        image.color = new Color(0, 0, 0, alphaFin);
    }
}
