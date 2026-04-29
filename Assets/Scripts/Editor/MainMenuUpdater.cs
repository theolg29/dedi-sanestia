using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public class MainMenuUpdater
{
    static MainMenuUpdater()
    {
        EditorApplication.delayCall += RunUpdate;
    }

    static void RunUpdate()
    {
        if (EditorPrefs.GetBool("MainMenuUpdaterRun_SanestiaUI_v4", false))
            return;
        
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = default;
        bool wasOpen = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).path == scenePath)
            {
                scene = SceneManager.GetSceneAt(i);
                wasOpen = true;
                break;
            }
        }

        if (!wasOpen)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        if (!scene.IsValid()) return;

        bool changed = false;

        // 1. Remove Title
        TextMeshProUGUI[] tmps = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach(var tmp in tmps)
        {
            if (tmp.gameObject.scene == scene && tmp.text != null && tmp.text.ToLower().Contains("sanestia"))
            {
                Object.DestroyImmediate(tmp.gameObject);
                changed = true;
            }
        }

        // 1.5 Fix Video Background
        var videoPlayer = Object.FindAnyObjectByType<UnityEngine.Video.VideoPlayer>();
        if (videoPlayer == null) 
        {
            GameObject bgObj = GameObject.Find("sanestia-bg");
            if (bgObj != null) videoPlayer = bgObj.GetComponent<UnityEngine.Video.VideoPlayer>();
        }
        if (videoPlayer != null)
        {
            videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.playOnAwake = true;
            changed = true;
        }

        // Fix background panel that might be blocking the video
        foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (img.gameObject.scene == scene && (img.gameObject.name.ToLower().Contains("panel") || img.gameObject.name.ToLower() == "background"))
            {
                var rt = img.GetComponent<RectTransform>();
                if (rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                    changed = true;
                }
            }
        }

        // 1.6 Add/Center Logo "Sanestia (1)"
        GameObject logoObj = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene == scene && go.name.Contains("Sanestia (1)"))
            {
                logoObj = go;
                break;
            }
        }

        if (logoObj == null)
        {
            string[] guids = AssetDatabase.FindAssets("Sanestia (1) t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (logoSprite != null)
                {
                    GameObject canvas = GameObject.Find("Canvas");
                    if (canvas == null)
                    {
                        foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
                        {
                            if (c.gameObject.scene == scene) { canvas = c.gameObject; break; }
                        }
                    }
                    if (canvas != null)
                    {
                        logoObj = new GameObject("Sanestia (1)");
                        logoObj.transform.SetParent(canvas.transform, false);
                        var img = logoObj.AddComponent<Image>();
                        img.sprite = logoSprite;
                        img.SetNativeSize();
                        changed = true;
                    }
                }
            }
        }

        if (logoObj != null)
        {
            var logoRt = logoObj.GetComponent<RectTransform>();
            if (logoRt != null)
            {
                logoRt.anchorMin = new Vector2(0.5f, 0.5f);
                logoRt.anchorMax = new Vector2(0.5f, 0.5f);
                logoRt.pivot = new Vector2(0.5f, 0.5f);
                logoRt.anchoredPosition = new Vector2(0f, 200f); // above buttons
                logoObj.SetActive(true);
                changed = true;
            }
        }

        // 2. Remove Credits and Settings buttons
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach(var btn in buttons)
        {
            if (btn.gameObject.scene != scene) continue;

            string name = btn.gameObject.name.ToLower();
            bool isTarget = name.Contains("credit") || name.Contains("param") || name.Contains("setting");

            if (!isTarget)
            {
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && tmp.text != null)
                {
                    string text = tmp.text.ToLower();
                    if (text.Contains("param") || text.Contains("credit") || text.Contains("crédit"))
                    {
                        isTarget = true;
                    }
                }
            }

            if (isTarget)
            {
                Object.DestroyImmediate(btn.gameObject);
                changed = true;
            }
        }

        // 3. Move Quit below Play
        GameObject btnPlay = null;
        GameObject btnQuit = null;

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene == scene)
            {
                if (go.name == "ButtonJouer") btnPlay = go;
                if (go.name == "ButtonQuit" || go.name == "ButtonQuitter" || go.name == "ButtonBack") btnQuit = go;
            }
        }

        if (btnPlay != null && btnQuit != null)
        {
            RectTransform playRt = btnPlay.GetComponent<RectTransform>();
            RectTransform quitRt = btnQuit.GetComponent<RectTransform>();
            
            // Placing Quit button closer to the Play button (45 units below)
            quitRt.anchoredPosition = new Vector2(playRt.anchoredPosition.x, playRt.anchoredPosition.y - 45f);
            changed = true;

            // 4. Cleaner style for buttons
            Button[] remainingButtons = Resources.FindObjectsOfTypeAll<Button>();
            foreach(var btn in remainingButtons)
            {
                if (btn.gameObject.scene != scene) continue;
                
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, 45f); // Increased vertical padding
                }
                
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0f, 0f, 0f, 1f); // 100% black opacity
                }
                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                    text.fontStyle = FontStyles.Bold;
                    text.fontSize = 26; // slightly larger for clean look
                }
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Antigravity] MainMenu updated successfully!");
        }

        if (!wasOpen)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        EditorPrefs.SetBool("MainMenuUpdaterRun_SanestiaUI_v4", true);
    }
}
