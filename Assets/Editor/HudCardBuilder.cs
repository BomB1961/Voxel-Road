using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VoxelRoad.UI;

namespace VoxelRoad.EditorTools
{
    /// <summary>HUD 점수 카드 + NEW BEST 배너를 일괄 빌드. 멱등(재실행 시 기존 파괴 후 재생성).</summary>
    public static class HudCardBuilder
    {
        private const string MenuPath = "Tools/Voxel Road/Build HUD Score Card";
        private const string CardName = "ScoreCard";
        private const string BannerName = "NewBestBanner";
        private const string SpritePath = "Assets/Art/UI/round_card_r12.png";
        private const string StarSpritePath = "Assets/Art/UI/star_white.png";
        private const string FontAssetPath = "Assets/Fonts/Kenney Mini Square SDF.asset";
        private const string OutlineMatPath = "Assets/Fonts/Kenney Mini Square SDF - HUD Outline.mat";
        private const string BannerOutlineMatPath = "Assets/Fonts/Kenney Mini Square SDF - Banner Outline.mat";

        private const int CardLeft = 24;
        private const int CardTop = 24;
        private const int PadLeft = 20;
        private const int PadRight = 36; // Pop 효과 시 텍스트 우측 확장 흡수 버퍼
        private const int PadTop = 12;
        private const int PadBottom = 12;
        private const int Spacing = 16;
        private const int StripeWidth = 6;
        private const int ScoreFontSize = 84;
        private const float ScoreTextHeight = 110f; // 84 * 1.2 + 약간 여유
        private const int BannerFontSize = 108;
        private const float BannerTextHeight = 140f;
        private const float ShadowOffsetY = -4f;

        private static readonly Color32 BackdropColor = new Color32(0x15, 0x17, 0x1C, 220);
        private static readonly Color32 ShadowColor = new Color32(0, 0, 0, 110);
        private static readonly Color32 GoldColor = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
        private static readonly Color32 RedColor = new Color32(0xFF, 0x3B, 0x30, 0xFF);
        private static readonly Color32 BrightYellowColor = new Color32(0xFF, 0xEB, 0x3B, 0xFF);

        [MenuItem(MenuPath)]
        public static void Build()
        {
            var hud = FindHudParent();
            if (hud == null)
            {
                Debug.LogError("[HudCardBuilder] Canvas/HUD 오브젝트를 찾을 수 없음");
                return;
            }

            // 9-slice 임포트 설정이 늦게 적용될 수 있어 명시적으로 강제 재임포트
            if (System.IO.File.Exists(SpritePath))
                AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            if (System.IO.File.Exists(StarSpritePath))
                AssetDatabase.ImportAsset(StarSpritePath, ImportAssetOptions.ForceUpdate);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError($"[HudCardBuilder] 스프라이트 없음: {SpritePath}. 임포트 후 재시도.");
                return;
            }
            var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarSpritePath);

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            EnsureFallbackIsDynamic(fallbackFont);
            EnsureFontFallback(fontAsset, fallbackFont);
            var outlineMat = EnsureOutlineMaterial(fontAsset);
            var bannerOutlineMat = EnsureBannerOutlineMaterial(fontAsset);

            var gameHud = Object.FindFirstObjectByType<GameHUD>(FindObjectsInactive.Include);
            var tracker = Object.FindFirstObjectByType<ScoreTracker>(FindObjectsInactive.Include);
            var pop = Object.FindFirstObjectByType<UIScorePop>(FindObjectsInactive.Include);

            // 원본 텍스트 참조 캡처 (1회차: 활성 / N회차: 우리가 비활성화시킨 상태)
            var origScoreText = FindOriginalText(hud.transform, "ScoreText");
            var origBestText = FindOriginalText(hud.transform, "BestScoreText");
            if (origScoreText == null && gameHud != null)
                origScoreText = ReadTmpRef(gameHud, "_scoreText");
            if (origBestText == null && gameHud != null)
                origBestText = ReadTmpRef(gameHud, "_bestScoreText");

            // 기존 카드/배너 정리
            DestroyExisting(hud, CardName);
            DestroyExisting(hud, BannerName);

            var card = BuildScoreCard(hud, sprite, fontAsset, outlineMat);
            var banner = BuildNewBestBanner(hud, sprite, fontAsset, bannerOutlineMat, starSprite);

            var newScoreText = card.transform.Find("ScoreLabel").GetComponent<TMP_Text>();
            var digitsText = card.transform.Find("ScoreDigits").GetComponent<TMP_Text>();

            // 참조 재배선
            if (gameHud != null)
            {
                var so = new SerializedObject(gameHud);
                // GameHUD는 "SCORE 00042" 형태의 합쳐진 텍스트를 자신의 _scoreText에 쓰는데,
                // 우리는 ScoreLabel(정적 'SCORE') / ScoreDigits(숫자만) 로 분리했으므로
                // GameHUD의 출력이 보이는 새 라벨에 닿지 않도록 원본(비활성·alpha=0)으로 되돌림.
                if (origScoreText != null)
                {
                    var sProp = so.FindProperty("_scoreText");
                    if (sProp != null) sProp.objectReferenceValue = origScoreText;
                }
                // _bestScoreText는 GameHUD.Awake null 체크에 걸리므로 원본을 유지(비활성 상태)
                if (origBestText != null)
                {
                    var bProp = so.FindProperty("_bestScoreText");
                    if (bProp != null && bProp.objectReferenceValue == null)
                        bProp.objectReferenceValue = origBestText;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(gameHud);
            }

            if (pop != null)
            {
                var so = new SerializedObject(pop);
                var prop = so.FindProperty("_target");
                // 숫자 부분만 Pop 대상 (SCORE 라벨은 정적)
                if (prop != null) prop.objectReferenceValue = digitsText.GetComponent<RectTransform>();
                var peakProp = so.FindProperty("_peakScale");
                if (peakProp != null) peakProp.floatValue = 1.20f;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(pop);
            }

            // 숫자 바인딩 와이어링
            var digitsBinding = digitsText.GetComponent<ScoreDigitsBinding>();
            if (digitsBinding != null && tracker != null)
            {
                var bso = new SerializedObject(digitsBinding);
                var tProp = bso.FindProperty("_tracker");
                var dProp = bso.FindProperty("_digitsText");
                if (tProp != null) tProp.objectReferenceValue = tracker;
                if (dProp != null) dProp.objectReferenceValue = digitsText;
                bso.ApplyModifiedProperties();
                EditorUtility.SetDirty(digitsBinding);
            }

            // 배너 와이어링
            var bannerScript = banner.GetComponent<NewBestBanner>();
            if (bannerScript != null && tracker != null)
            {
                var so = new SerializedObject(bannerScript);
                var tProp = so.FindProperty("_tracker");
                var rProp = so.FindProperty("_bannerRect");
                if (tProp != null) tProp.objectReferenceValue = tracker;
                if (rProp != null) rProp.objectReferenceValue = banner.GetComponent<RectTransform>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(bannerScript);
            }

            // 원본 텍스트는 활성 유지(UIScorePop 코루틴 호스트)하되 alpha=0으로 투명 처리
            HideTextInPlace(origScoreText);
            HideTextInPlace(origBestText);

            EditorUtility.SetDirty(hud);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.scene);
            Debug.Log("[HudCardBuilder] 완료. 씬 저장 잊지 마세요.");
        }

        private static GameObject FindHudParent()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                var hud = c.transform.Find("HUD");
                if (hud != null) return hud.gameObject;
            }
            return canvases.Length > 0 ? canvases[0].gameObject : null;
        }

        private static void DestroyExisting(GameObject parent, string name)
        {
            var existing = parent.transform.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        private static TMP_Text FindOriginalText(Transform hudRoot, string targetName)
        {
            foreach (var t in hudRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.gameObject.name != targetName) continue;
                if (IsUnderManagedNode(t.transform)) continue;
                return t;
            }
            return null;
        }

        private static bool IsUnderManagedNode(Transform t)
        {
            var p = t;
            while (p != null)
            {
                if (p.name == CardName || p.name == BannerName) return true;
                p = p.parent;
            }
            return false;
        }

        private static void HideTextInPlace(TMP_Text text)
        {
            if (text == null) return;
            text.gameObject.SetActive(true);
            var c = text.color;
            c.a = 0f;
            text.color = c;
            text.text = string.Empty;
            EditorUtility.SetDirty(text);
        }

        private static TMP_Text ReadTmpRef(Component owner, string fieldName)
        {
            var so = new SerializedObject(owner);
            var prop = so.FindProperty(fieldName);
            if (prop == null) return null;
            return prop.objectReferenceValue as TMP_Text;
        }

        private static GameObject BuildScoreCard(GameObject hud, Sprite sprite, TMP_FontAsset font, Material outlineMat)
        {
            var card = new GameObject(CardName, typeof(RectTransform));
            card.transform.SetParent(hud.transform, false);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(CardLeft, -CardTop);

            var hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(PadLeft, PadRight, PadTop, PadBottom);
            hlg.spacing = Spacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateBackdropImage(card.transform, "Shadow", sprite, ShadowColor, ShadowOffsetY).transform.SetSiblingIndex(0);
            CreateBackdropImage(card.transform, "Backdrop", sprite, BackdropColor, 0f).transform.SetSiblingIndex(1);

            var stripe = new GameObject("LeftStripe", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            stripe.transform.SetParent(card.transform, false);
            var stripeImg = stripe.GetComponent<Image>();
            stripeImg.color = GoldColor;
            stripeImg.raycastTarget = false;
            var stripeLE = stripe.GetComponent<LayoutElement>();
            stripeLE.preferredWidth = StripeWidth;
            stripeLE.preferredHeight = ScoreTextHeight;

            // "SCORE" 정적 라벨 (Pop 대상 아님)
            var labelGO = new GameObject("ScoreLabel", typeof(RectTransform), typeof(LayoutElement));
            labelGO.transform.SetParent(card.transform, false);
            var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) labelTmp.font = font;
            if (outlineMat != null) labelTmp.fontSharedMaterial = outlineMat;
            labelTmp.fontSize = ScoreFontSize;
            labelTmp.color = Color.white;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.text = "SCORE";
            labelTmp.enableAutoSizing = false;
            labelTmp.raycastTarget = false;
            labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
            var labelLE = labelGO.GetComponent<LayoutElement>();
            labelLE.preferredHeight = ScoreTextHeight;

            // 숫자 부분만 별도 TMP — Pop 효과 대상
            var digitsGO = new GameObject("ScoreDigits", typeof(RectTransform), typeof(LayoutElement));
            digitsGO.transform.SetParent(card.transform, false);
            var digitsTmp = digitsGO.AddComponent<TextMeshProUGUI>();
            if (font != null) digitsTmp.font = font;
            if (outlineMat != null) digitsTmp.fontSharedMaterial = outlineMat;
            digitsTmp.fontSize = ScoreFontSize;
            digitsTmp.color = Color.white;
            digitsTmp.alignment = TextAlignmentOptions.MidlineLeft;
            digitsTmp.text = "00000";
            digitsTmp.enableAutoSizing = false;
            digitsTmp.raycastTarget = false;
            digitsTmp.textWrappingMode = TextWrappingModes.NoWrap;
            var digitsLE = digitsGO.GetComponent<LayoutElement>();
            digitsLE.preferredHeight = ScoreTextHeight;

            digitsGO.AddComponent<ScoreDigitsBinding>();

            return card;
        }

        private static GameObject BuildNewBestBanner(GameObject hud, Sprite sprite, TMP_FontAsset font, Material outlineMat, Sprite starSprite)
        {
            var banner = new GameObject(BannerName, typeof(RectTransform), typeof(CanvasGroup));
            banner.transform.SetParent(hud.transform, false);
            var rt = (RectTransform)banner.transform;
            // 화면 정중앙 기준 위쪽으로 약간 오프셋 (Y+ = 위)
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 240);

            var hlg = banner.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var fitter = banner.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 좌측 별 (텍스트와 같은 높이로 맞춤)
            CreateStarImage(banner.transform, "LeftStar", starSprite);

            // 텍스트 (별만 분리되어 폰트 글리프 한계 우회)
            var textGO = new GameObject("BannerText", typeof(RectTransform), typeof(LayoutElement));
            textGO.transform.SetParent(banner.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            if (outlineMat != null) tmp.fontSharedMaterial = outlineMat;
            tmp.fontSize = BannerFontSize;
            tmp.color = BrightYellowColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = true;
            tmp.text = "Best Record";
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            var le = textGO.GetComponent<LayoutElement>();
            le.preferredHeight = BannerTextHeight;

            // 우측 별
            CreateStarImage(banner.transform, "RightStar", starSprite);

            banner.AddComponent<NewBestBanner>();

            var cg = banner.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            return banner;
        }

        private static void CreateStarImage(Transform parent, string name, Sprite starSprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = starSprite;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            var le = go.GetComponent<LayoutElement>();
            // 텍스트 cap-height에 시각적 균형 맞도록 폰트 사이즈의 약 90%
            float starSize = BannerFontSize * 0.9f;
            le.preferredWidth = starSize;
            le.preferredHeight = starSize;
        }

        private static GameObject CreateBackdropImage(Transform parent, string name, Sprite sprite, Color32 color, float offsetY)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0, offsetY);
            rt.offsetMax = new Vector2(0, offsetY);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;

            var le = go.GetComponent<LayoutElement>();
            le.ignoreLayout = true;

            return go;
        }

        private static Material EnsureOutlineMaterial(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);
            if (existing != null) return existing;
            if (font == null || font.material == null) return null;

            var mat = new Material(font.material);
            mat.name = "Kenney Mini Square SDF - HUD Outline";
            mat.EnableKeyword("OUTLINE_ON");
            if (mat.HasProperty("_OutlineColor")) mat.SetColor("_OutlineColor", Color.black);
            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 0.2f);
            mat.EnableKeyword("UNDERLAY_ON");
            if (mat.HasProperty("_UnderlayColor")) mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
            if (mat.HasProperty("_UnderlayOffsetX")) mat.SetFloat("_UnderlayOffsetX", 1f);
            if (mat.HasProperty("_UnderlayOffsetY")) mat.SetFloat("_UnderlayOffsetY", -1f);
            if (mat.HasProperty("_UnderlaySoftness")) mat.SetFloat("_UnderlaySoftness", 0.5f);
            AssetDatabase.CreateAsset(mat, OutlineMatPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material EnsureBannerOutlineMaterial(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(BannerOutlineMatPath);
            if (existing != null)
            {
                ApplyBannerOutlineProps(existing);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }
            if (font == null || font.material == null) return null;

            var mat = new Material(font.material);
            mat.name = "Kenney Mini Square SDF - Banner Outline";
            ApplyBannerOutlineProps(mat);
            AssetDatabase.CreateAsset(mat, BannerOutlineMatPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // LiberationSans SDF의 atlas가 정적 250자(ASCII+Latin1)로 베이크되어 ★ 같은 심볼 미포함.
        // Dynamic 모드로 전환하면 런타임에 글리프를 atlas에 추가 렌더링 가능 (소스 LiberationSans.ttf에서 ★ 자동 조회).
        private static void EnsureFallbackIsDynamic(TMP_FontAsset fallback)
        {
            if (fallback == null) return;
            if (fallback.atlasPopulationMode == AtlasPopulationMode.Dynamic) return;
            fallback.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            EditorUtility.SetDirty(fallback);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFontFallback(TMP_FontAsset primary, TMP_FontAsset fallback)
        {
            if (primary == null || fallback == null) return;
            if (primary.fallbackFontAssetTable == null)
                primary.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            if (primary.fallbackFontAssetTable.Contains(fallback)) return;
            primary.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(primary);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyBannerOutlineProps(Material mat)
        {
            mat.EnableKeyword("OUTLINE_ON");
            if (mat.HasProperty("_OutlineColor")) mat.SetColor("_OutlineColor", new Color(1f, 1f, 1f, 0.4f));
            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 0.08f);
            mat.EnableKeyword("UNDERLAY_ON");
            if (mat.HasProperty("_UnderlayColor")) mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
            if (mat.HasProperty("_UnderlayOffsetX")) mat.SetFloat("_UnderlayOffsetX", 1f);
            if (mat.HasProperty("_UnderlayOffsetY")) mat.SetFloat("_UnderlayOffsetY", -1f);
            if (mat.HasProperty("_UnderlaySoftness")) mat.SetFloat("_UnderlaySoftness", 0.5f);
        }
    }
}
