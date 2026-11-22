using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Calavera.EditorTools
{
    /// <summary>
    /// Genera TODO el proyecto desde el menú Calavera > Construir proyecto completo:
    /// sprites, clips y Animator Controllers, prefab de carta, y las 4 escenas con su interfaz.
    /// </summary>
    public static class ProjectBuilder
    {
        const string Root = "Assets/Calavera";
        const string ArtDir = Root + "/Art";
        const string AnimDir = Root + "/Animations";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";

        class Art { public Sprite rounded, skull; public Sprite[] symbols; }
        class Anims { public AnimatorController card, mascot; }

        // ------------------------------------------------------------------ ENTRADA
        [MenuItem("Calavera/Construir proyecto completo")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            Art art = GenerateArt();
            Anims anims = GenerateAnimations();
            CardView cardPrefab = CreateCardPrefab(art, anims.card);

            BuildMenu(art, anims);
            BuildInstructions(art, anims);
            BuildGame(art, anims, cardPrefab);
            BuildResults(art, anims);

            RegisterScenes();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(ScenePath(SceneNames.Menu));
            EditorUtility.DisplayDialog("Calavera Memorama",
                "Proyecto construido.\n\nPresiona Play con la escena MenuPrincipal abierta.", "OK");
        }

        [MenuItem("Calavera/Abrir menú principal")]
        public static void OpenMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath(SceneNames.Menu));
        }

        static string ScenePath(string name) { return SceneDir + "/" + name + ".unity"; }

        static void EnsureFolders()
        {
            foreach (string d in new[] { ArtDir, AnimDir, PrefabDir, SceneDir }) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ ARTE PROCEDURAL
        static readonly Vector2[] PolyTri = { new Vector2(0f, 0.85f), new Vector2(0.85f, -0.7f), new Vector2(-0.85f, -0.7f) };
        static readonly Vector2[] PolyDiamond = { new Vector2(0f, 0.88f), new Vector2(0.66f, 0f), new Vector2(0f, -0.88f), new Vector2(-0.66f, 0f) };
        static readonly Vector2[] PolyHex = MakeRegular(6, 0.88f, 0f);
        static readonly Vector2[] PolyStar = MakeStar();
        static readonly Vector2[] PolyNose = { new Vector2(115f, 94f), new Vector2(141f, 94f), new Vector2(128f, 120f) };

        static Vector2[] MakeRegular(int n, float r, float startDeg)
        {
            var p = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = (startDeg + i * 360f / n) * Mathf.Deg2Rad;
                p[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return p;
        }

        static Vector2[] MakeStar()
        {
            var p = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = (90f + i * 36f) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? 0.9f : 0.4f;
                p[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return p;
        }

        static bool InPoly(float x, float y, Vector2[] p)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                if (((p[i].y > y) != (p[j].y > y)) &&
                    (x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                    inside = !inside;
            }
            return inside;
        }

        static bool Circ(float x, float y, float cx, float cy, float r)
        {
            return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
        }

        static bool RRect(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            if (x < x0 || x > x1 || y < y0 || y > y1) return false;
            float dx = Mathf.Max(Mathf.Max(x0 + r - x, x - (x1 - r)), 0f);
            float dy = Mathf.Max(Mathf.Max(y0 + r - y, y - (y1 - r)), 0f);
            return dx * dx + dy * dy <= r * r;
        }

        /// <summary>Pinta una figura en blanco con bordes suavizados (supermuestreo 3x3).</summary>
        static Texture2D Paint(int w, int h, Func<float, float, bool> inside)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            const int S = 3;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < S; sy++)
                        for (int sx = 0; sx < S; sx++)
                            if (inside(x + (sx + 0.5f) / S, y + (sy + 0.5f) / S)) hit++;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(255 * hit / (S * S)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static bool SkullShape(float x, float y)
        {
            bool head = Circ(x, y, 128f, 140f, 96f) || RRect(x, y, 70f, 30f, 186f, 120f, 22f);
            if (!head) return false;
            if (Circ(x, y, 92f, 134f, 27f) || Circ(x, y, 164f, 134f, 27f)) return false; // ojos
            if (InPoly(x, y, PolyNose)) return false;                                      // nariz
            if (y >= 38f && y <= 84f &&
                (Mathf.Abs(x - 108f) <= 3.5f || Mathf.Abs(x - 128f) <= 3.5f || Mathf.Abs(x - 148f) <= 3.5f)) return false; // dientes
            if (y >= 82f && y <= 89f && x >= 82f && x <= 174f) return false;             // línea de la boca
            return true;
        }

        static bool SymbolShape(int i, float x, float y)
        {
            switch (i)
            {
                case 0: return x * x + y * y <= 0.64f;                                  // círculo
                case 1: return Mathf.Abs(x) <= 0.72f && Mathf.Abs(y) <= 0.72f;          // cuadrado
                case 2: return InPoly(x, y, PolyTri);                                   // triángulo
                case 3: return InPoly(x, y, PolyStar);                                  // estrella
                case 4: return InPoly(x, y, PolyDiamond);                               // rombo
                case 5:                                                                 // corazón
                    {
                        float u = x * 1.4f, v = y * 1.3f + 0.15f;
                        float a = u * u + v * v - 1f;
                        return a * a * a - u * u * v * v * v <= 0f;
                    }
                case 6: return (Mathf.Abs(x) <= 0.25f && Mathf.Abs(y) <= 0.78f) ||
                               (Mathf.Abs(y) <= 0.25f && Mathf.Abs(x) <= 0.78f);        // cruz
                case 7: return Circ(x, y, 0f, 0f, 0.85f) && !Circ(x, y, 0.4f, 0.15f, 0.7f); // luna
                case 8: return InPoly(x, y, PolyHex);                                   // hexágono
                default: { float d = x * x + y * y; return d >= 0.16f && d <= 0.68f; }   // anillo
            }
        }

        static Art GenerateArt()
        {
            var a = new Art();
            a.rounded = SaveSprite("rounded", Paint(64, 64, (x, y) =>
            {
                const float r = 14f;
                float dx = Mathf.Max(Mathf.Max(r - x, x - (64f - r)), 0f);
                float dy = Mathf.Max(Mathf.Max(r - y, y - (64f - r)), 0f);
                return dx * dx + dy * dy <= r * r;
            }), new Vector4(18, 18, 18, 18));

            a.skull = SaveSprite("skull", Paint(256, 256, SkullShape), Vector4.zero);

            a.symbols = new Sprite[10];
            for (int i = 0; i < 10; i++)
            {
                int idx = i;
                a.symbols[i] = SaveSprite("symbol_" + i, Paint(128, 128, (x, y) => SymbolShape(idx, x / 64f - 1f, y / 64f - 1f)), Vector4.zero);
            }
            return a;
        }

        static Sprite SaveSprite(string name, Texture2D tex, Vector4 border)
        {
            string path = ArtDir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.spriteBorder = border;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------ ANIMACIONES
        static readonly Type RT = typeof(RectTransform);
        static readonly Type GO = typeof(GameObject);
        static readonly Type IMG = typeof(Image);
        const string SX = "m_LocalScale.x", SY = "m_LocalScale.y", RZ = "localEulerAnglesRaw.z", ACT = "m_IsActive";

        static Keyframe K(float t, float v) { return new Keyframe(t, v); }

        static void Smooth(AnimationClip c, string path, Type type, string prop, params Keyframe[] keys)
        {
            var cur = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) cur.SmoothTangents(i, 0f);
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(path, type, prop), cur);
        }

        static void Step(AnimationClip c, string path, Type type, string prop, params Keyframe[] keys)
        {
            var cur = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(cur, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(cur, i, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(path, type, prop), cur);
        }

        static void FrontColor(AnimationClip c, params KeyValuePair<float, Color>[] keys)
        {
            var r = new Keyframe[keys.Length]; var g = new Keyframe[keys.Length]; var b = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                r[i] = K(keys[i].Key, keys[i].Value.r);
                g[i] = K(keys[i].Key, keys[i].Value.g);
                b[i] = K(keys[i].Key, keys[i].Value.b);
            }
            Smooth(c, "Front", IMG, "m_Color.r", r);
            Smooth(c, "Front", IMG, "m_Color.g", g);
            Smooth(c, "Front", IMG, "m_Color.b", b);
        }

        static KeyValuePair<float, Color> CK(float t, Color col) { return new KeyValuePair<float, Color>(t, col); }

        /// <summary>Valores base: cada clip declara todas las propiedades para que al cambiar de estado nada quede "pegado".</summary>
        static void CardBase(AnimationClip c, float len)
        {
            Smooth(c, "", RT, SX, K(0, 1), K(len, 1));
            Smooth(c, "", RT, SY, K(0, 1), K(len, 1));
            Smooth(c, "", RT, RZ, K(0, 0), K(len, 0));
            Step(c, "Back", GO, ACT, K(0, 1), K(len, 1));
            Step(c, "Front", GO, ACT, K(0, 0), K(len, 0));
            FrontColor(c, CK(0, Palette.Crema), CK(len, Palette.Crema));
        }

        static AnimationClip Finish(AnimationClip c, string name, bool loop)
        {
            c.name = name;
            var s = AnimationUtility.GetAnimationClipSettings(c);
            s.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(c, s);
            string path = AnimDir + "/" + name + ".anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(c, path);
            return c;
        }

        static AnimationClip NewClip() { return new AnimationClip { frameRate = 60f }; }

        static AnimatorState AddState(AnimatorStateMachine sm, string name, Motion m)
        {
            AnimatorState s = sm.AddState(name);
            s.motion = m;
            return s;
        }

        static void Trans(AnimatorState from, AnimatorState to, string trigger)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.duration = 0f;
            t.hasFixedDuration = true;
            if (trigger == null) { t.hasExitTime = true; t.exitTime = 1f; }
            else { t.hasExitTime = false; t.AddCondition(AnimatorConditionMode.If, 0, trigger); }
        }

        static AnimatorController NewController(string name)
        {
            string path = AnimDir + "/" + name + ".controller";
            AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        static Anims GenerateAnimations()
        {
            var result = new Anims();
            Color mint = Palette.Menta, salmon = Palette.Salmon, cream = Palette.Crema;

            // ---------- Carta ----------
            var idle = NewClip(); CardBase(idle, 1f);
            Finish(idle, "Card_Idle", true);

            var flip = NewClip(); CardBase(flip, 0.3f);
            Smooth(flip, "", RT, SX, K(0, 1), K(0.15f, 0), K(0.3f, 1));
            Step(flip, "Back", GO, ACT, K(0, 1), K(0.15f, 0));
            Step(flip, "Front", GO, ACT, K(0, 0), K(0.15f, 1));
            Finish(flip, "Card_Flip", false);

            var revealed = NewClip(); CardBase(revealed, 1f);
            Step(revealed, "Back", GO, ACT, K(0, 0), K(1, 0));
            Step(revealed, "Front", GO, ACT, K(0, 1), K(1, 1));
            Finish(revealed, "Card_Revealed", true);

            var match = NewClip(); CardBase(match, 0.6f);
            Step(match, "Back", GO, ACT, K(0, 0), K(0.6f, 0));
            Step(match, "Front", GO, ACT, K(0, 1), K(0.6f, 1));
            Smooth(match, "", RT, SX, K(0, 1), K(0.2f, 1.25f), K(0.4f, 0.88f), K(0.6f, 0.92f));
            Smooth(match, "", RT, SY, K(0, 1), K(0.2f, 1.25f), K(0.4f, 0.88f), K(0.6f, 0.92f));
            Smooth(match, "", RT, RZ, K(0, 0), K(0.6f, 360f));
            FrontColor(match, CK(0, cream), CK(0.3f, mint), CK(0.6f, mint));
            Finish(match, "Card_Match", false);

            var matched = NewClip(); CardBase(matched, 1f);
            Step(matched, "Back", GO, ACT, K(0, 0), K(1, 0));
            Step(matched, "Front", GO, ACT, K(0, 1), K(1, 1));
            Smooth(matched, "", RT, SX, K(0, 0.92f), K(1, 0.92f));
            Smooth(matched, "", RT, SY, K(0, 0.92f), K(1, 0.92f));
            FrontColor(matched, CK(0, mint), CK(1, mint));
            Finish(matched, "Card_Matched", true);

            var mismatch = NewClip(); CardBase(mismatch, 0.5f);
            Step(mismatch, "Back", GO, ACT, K(0, 0), K(0.5f, 0));
            Step(mismatch, "Front", GO, ACT, K(0, 1), K(0.5f, 1));
            Smooth(mismatch, "", RT, RZ, K(0, 0), K(0.08f, -9), K(0.16f, 9), K(0.24f, -9), K(0.32f, 9), K(0.4f, -4), K(0.5f, 0));
            FrontColor(mismatch, CK(0, cream), CK(0.1f, salmon), CK(0.5f, salmon));
            Finish(mismatch, "Card_Mismatch", false);

            var unflip = NewClip(); CardBase(unflip, 0.3f);
            Smooth(unflip, "", RT, SX, K(0, 1), K(0.15f, 0), K(0.3f, 1));
            Step(unflip, "Front", GO, ACT, K(0, 1), K(0.15f, 0));
            Step(unflip, "Back", GO, ACT, K(0, 0), K(0.15f, 1));
            FrontColor(unflip, CK(0, salmon), CK(0.3f, cream));
            Finish(unflip, "Card_UnFlip", false);

            AnimatorController cc = NewController("Card");
            cc.AddParameter("Flip", AnimatorControllerParameterType.Trigger);
            cc.AddParameter("Match", AnimatorControllerParameterType.Trigger);
            cc.AddParameter("Mismatch", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = cc.layers[0].stateMachine;
            AnimatorState sIdle = AddState(sm, "Idle", idle);
            AnimatorState sFlip = AddState(sm, "Flip", flip);
            AnimatorState sRev = AddState(sm, "Revealed", revealed);
            AnimatorState sMatch = AddState(sm, "Match", match);
            AnimatorState sMatched = AddState(sm, "Matched", matched);
            AnimatorState sMis = AddState(sm, "Mismatch", mismatch);
            AnimatorState sUnflip = AddState(sm, "UnFlip", unflip);
            sm.defaultState = sIdle;
            Trans(sIdle, sFlip, "Flip");
            Trans(sFlip, sRev, null);
            Trans(sRev, sMatch, "Match");
            Trans(sRev, sMis, "Mismatch");
            Trans(sMatch, sMatched, null);
            Trans(sMis, sUnflip, null);
            Trans(sUnflip, sIdle, null);
            result.card = cc;

            // ---------- Calavera (mascota) ----------
            var mIdle = NewClip();
            Smooth(mIdle, "", RT, SX, K(0, 1), K(1, 1));
            Smooth(mIdle, "", RT, SY, K(0, 1), K(1, 1));
            Smooth(mIdle, "", RT, RZ, K(0, 0), K(1, 0));
            Finish(mIdle, "Mascot_Idle", true);

            var mSaludo = NewClip();
            Smooth(mSaludo, "", RT, RZ, K(0, 0), K(0.8f, 360f));
            Smooth(mSaludo, "", RT, SX, K(0, 1), K(0.25f, 1.2f), K(0.55f, 1.2f), K(0.8f, 1));
            Smooth(mSaludo, "", RT, SY, K(0, 1), K(0.25f, 1.2f), K(0.55f, 1.2f), K(0.8f, 1));
            Finish(mSaludo, "Mascot_Saludo", false);

            var mAcierto = NewClip();
            Smooth(mAcierto, "", RT, SX, K(0, 1), K(0.12f, 1.15f), K(0.3f, 0.9f), K(0.45f, 1.03f), K(0.6f, 1));
            Smooth(mAcierto, "", RT, SY, K(0, 1), K(0.12f, 0.82f), K(0.3f, 1.2f), K(0.45f, 0.95f), K(0.6f, 1));
            Smooth(mAcierto, "", RT, RZ, K(0, 0), K(0.3f, 10), K(0.45f, -6), K(0.6f, 0));
            Finish(mAcierto, "Mascot_Acierto", false);

            var mError = NewClip();
            Smooth(mError, "", RT, SX, K(0, 1), K(0.1f, 0.9f), K(0.5f, 1));
            Smooth(mError, "", RT, SY, K(0, 1), K(0.1f, 0.9f), K(0.5f, 1));
            Smooth(mError, "", RT, RZ, K(0, 0), K(0.08f, -12), K(0.16f, 12), K(0.24f, -12), K(0.32f, 12), K(0.4f, -5), K(0.5f, 0));
            Finish(mError, "Mascot_Error", false);

            AnimatorController mc = NewController("Mascot");
            mc.AddParameter("Saludo", AnimatorControllerParameterType.Trigger);
            mc.AddParameter("Acierto", AnimatorControllerParameterType.Trigger);
            mc.AddParameter("Error", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine msm = mc.layers[0].stateMachine;
            AnimatorState miIdle = AddState(msm, "Idle", mIdle);
            AnimatorState miSaludo = AddState(msm, "Saludo", mSaludo);
            AnimatorState miAcierto = AddState(msm, "Acierto", mAcierto);
            AnimatorState miError = AddState(msm, "Error", mError);
            msm.defaultState = miIdle;
            foreach (var pair in new[] { new KeyValuePair<AnimatorState, string>(miSaludo, "Saludo"),
                                         new KeyValuePair<AnimatorState, string>(miAcierto, "Acierto"),
                                         new KeyValuePair<AnimatorState, string>(miError, "Error") })
            {
                AnimatorStateTransition t = msm.AddAnyStateTransition(pair.Key);
                t.duration = 0f; t.hasExitTime = false; t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, pair.Value);
                Trans(pair.Key, miIdle, null);
            }
            result.mascot = mc;
            return result;
        }

        // ------------------------------------------------------------------ PREFAB DE CARTA
        static CardView CreateCardPrefab(Art art, AnimatorController ctrl)
        {
            var root = new GameObject("Card", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(175, 175);

            var bg = root.AddComponent<Image>();
            bg.sprite = art.rounded; bg.type = Image.Type.Sliced; bg.color = Palette.Tinta;
            var btn = root.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = bg;
            var anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            var view = root.AddComponent<CardView>();

            RectTransform back = R("Back", root.transform, Vector2.zero, Vector2.zero);
            Stretch(back, 7);
            var backImg = back.gameObject.AddComponent<Image>();
            backImg.sprite = art.rounded; backImg.type = Image.Type.Sliced; backImg.color = Palette.Magenta; backImg.raycastTarget = false;
            RectTransform skull = R("Skull", back, Vector2.zero, Vector2.zero);
            Stretch(skull, 28);
            var skullImg = skull.gameObject.AddComponent<Image>();
            skullImg.sprite = art.skull; skullImg.color = Palette.Tinta; skullImg.preserveAspect = true; skullImg.raycastTarget = false;

            RectTransform front = R("Front", root.transform, Vector2.zero, Vector2.zero);
            Stretch(front, 7);
            var frontImg = front.gameObject.AddComponent<Image>();
            frontImg.sprite = art.rounded; frontImg.type = Image.Type.Sliced; frontImg.color = Palette.Crema; frontImg.raycastTarget = false;
            RectTransform sym = R("Symbol", front, Vector2.zero, Vector2.zero);
            Stretch(sym, 30);
            var symImg = sym.gameObject.AddComponent<Image>();
            symImg.sprite = art.symbols[0]; symImg.preserveAspect = true; symImg.raycastTarget = false;
            front.gameObject.SetActive(false);

            view.symbolImage = symImg;

            string path = PrefabDir + "/Card.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CardView>();
        }

        // ------------------------------------------------------------------ AYUDAS DE UI
        static Font UiFont { get { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } }

        static RectTransform R(string name, Transform parent, Vector2 pos, Vector2 size, Vector2? anchor = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = a; rt.anchorMax = a;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static void Stretch(RectTransform rt, float offset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(offset, offset);
            rt.offsetMax = new Vector2(-offset, -offset);
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size, bool sliced = false, Vector2? anchor = null)
        {
            RectTransform rt = R(name, parent, pos, size, anchor);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            if (sliced) img.type = Image.Type.Sliced; else img.preserveAspect = true;
            return img;
        }

        static Text Txt(Transform parent, string name, string text, int size, Color color, FontStyle style, TextAnchor align, Vector2 pos, Vector2 box, Vector2? anchor = null)
        {
            RectTransform rt = R(name, parent, pos, box, anchor);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiFont; t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = align; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Button Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color fill, Color textColor, Art art, int fontSize = 44, Vector2? anchor = null)
        {
            RectTransform rt = R(name, parent, pos, size, anchor);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = art.rounded; img.type = Image.Type.Sliced; img.color = fill;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            b.colors = cb;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.35f); sh.effectDistance = new Vector2(0, -6);
            rt.gameObject.AddComponent<HoverScale>();
            Text t = Txt(rt, "Texto", label, fontSize, textColor, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            Stretch(t.rectTransform, 6);
            return b;
        }

        static Image Panel(Transform parent, string name, Art art, Color color, Vector2 pos, Vector2 size, Vector2? anchor = null)
        {
            Image p = Img(parent, name, art.rounded, color, pos, size, true, anchor);
            var sh = p.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.3f); sh.effectDistance = new Vector2(0, -10);
            return p;
        }

        static Mascot MakeMascot(Transform parent, Art art, AnimatorController ctrl, Vector2 pos, float size, Vector2? anchor = null)
        {
            RectTransform rt = R("Calavera", parent, pos, new Vector2(size, size), anchor);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = art.skull; img.color = Palette.Tinta; img.preserveAspect = true;
            var an = rt.gameObject.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            return rt.gameObject.AddComponent<Mascot>();
        }

        static Transform NewSceneWithCanvas(Art art)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Fondo;
            cam.orthographic = true;

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            RectTransform bg = R("Fondo", canvasGo.transform, Vector2.zero, Vector2.zero);
            Stretch(bg, 0);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = Palette.Fondo; bgImg.raycastTarget = false;

            Decor(canvasGo.transform, art);
            return canvasGo.transform;
        }

        static void Decor(Transform canvas, Art art)
        {
            RectTransform root = R("Decoracion", canvas, Vector2.zero, Vector2.zero);
            Stretch(root, 0);
            int[] idx = { 3, 5, 2, 7, 4, 8, 6, 9 };
            Vector2[] pos = { new Vector2(-840, 400), new Vector2(790, 420), new Vector2(-890, -130), new Vector2(870, -70),
                              new Vector2(-330, -440), new Vector2(430, -450), new Vector2(-600, 260), new Vector2(620, 240) };
            float[] size = { 150, 130, 120, 160, 110, 140, 100, 120 };
            for (int i = 0; i < idx.Length; i++)
            {
                Image d = Img(root, "Deco_" + i, art.symbols[idx[i]], new Color(1f, 1f, 1f, 0.22f), pos[i], new Vector2(size[i], size[i]));
                d.rectTransform.localEulerAngles = new Vector3(0, 0, (i * 37) % 60 - 30);
            }
        }

        static void Footer(Transform canvas, string text)
        {
            RectTransform bar = R("Pie", canvas, Vector2.zero, new Vector2(0, 56), new Vector2(0.5f, 0f));
            bar.anchorMin = new Vector2(0, 0); bar.anchorMax = new Vector2(1, 0);
            bar.pivot = new Vector2(0.5f, 0f); bar.anchoredPosition = Vector2.zero; bar.sizeDelta = new Vector2(0, 56);
            var img = bar.gameObject.AddComponent<Image>();
            img.color = Palette.Tinta; img.raycastTarget = false;
            Text t = Txt(bar, "Texto", text, 24, Palette.Crema, FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            Stretch(t.rectTransform, 0);
        }

        static void AddFader(Transform canvas)
        {
            RectTransform rt = R("Fader", canvas, Vector2.zero, Vector2.zero);
            Stretch(rt, 0);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Palette.Tinta;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            var fader = rt.gameObject.AddComponent<SceneFader>();
            fader.overlay = cg;
        }

        static void SaveScene(string name)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath(name));
        }

        static Text Chip(Transform parent, Art art, string label, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            Panel(parent, "Chip_" + label, art, Palette.Crema, pos, size, anchor);
            Transform chip = parent.Find("Chip_" + label);
            Txt(chip, "Etiqueta", label, 24, Palette.Magenta, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 32), new Vector2(size.x, 36));
            return Txt(chip, "Valor", "0", 50, Palette.Tinta, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, -18), new Vector2(size.x, 70));
        }

        // ------------------------------------------------------------------ ESCENAS
        static void BuildMenu(Art art, Anims anims)
        {
            Transform canvas = NewSceneWithCanvas(art);

            Img(canvas, "Disco", art.symbols[0], Palette.Crema, new Vector2(-450, 10), new Vector2(640, 640));
            Mascot mascot = MakeMascot(canvas, art, anims.mascot, new Vector2(-450, 10), 470);
            Txt(canvas, "Pista", "Toca la calavera para saludarla", 30, Palette.Tinta, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(-450, -350), new Vector2(700, 50));

            Text title = Txt(canvas, "Titulo", "CALAVERA\n<color=#FFF4D6>MEMORAMA</color>", 118, Palette.Tinta, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(360, 300), new Vector2(1000, 320));
            title.lineSpacing = 0.9f;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.Tinta; outline.effectDistance = new Vector2(5, -5);

            Button play = Btn(canvas, "BotonJugar", "JUGAR", new Vector2(360, 90), new Vector2(560, 100), Palette.Magenta, Palette.Crema, art, 52);
            Button diff = Btn(canvas, "BotonDificultad", "", new Vector2(360, -30), new Vector2(560, 90), Palette.Cian, Palette.Tinta, art, 32);
            Button how = Btn(canvas, "BotonInstrucciones", "INSTRUCCIONES", new Vector2(360, -140), new Vector2(560, 90), Palette.Crema, Palette.Tinta, art, 40);
            Button quit = Btn(canvas, "BotonSalir", "SALIR", new Vector2(360, -250), new Vector2(560, 90), Palette.Tinta, Palette.Crema, art, 40);

            Footer(canvas, "Proyecto escolar  ·  Desarrollo de Videojuegos  ·  Unity 2022.3 LTS");
            AddFader(canvas);

            var mc = new GameObject("MenuController").AddComponent<MenuController>();
            mc.playButton = play; mc.difficultyButton = diff; mc.instructionsButton = how; mc.quitButton = quit;
            mc.difficultyLabel = diff.GetComponentInChildren<Text>();
            SaveScene(SceneNames.Menu);
        }

        static void BuildInstructions(Art art, Anims anims)
        {
            Transform canvas = NewSceneWithCanvas(art);

            Panel(canvas, "Panel", art, Palette.Crema, new Vector2(0, 20), new Vector2(1500, 860));
            Txt(canvas, "Titulo", "CÓMO JUGAR", 90, Palette.Tinta, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 360), new Vector2(1200, 120));

            string body =
                "1.  Toca una carta para voltearla y descubrir su símbolo.\n\n" +
                "2.  Voltea una segunda carta e intenta formar una pareja.\n\n" +
                "3.  Si coinciden, la pareja se queda descubierta; si no, ambas se ocultan de nuevo.\n\n" +
                "4.  Encuentra todas las parejas con el menor número de movimientos y de tiempo.\n\n" +
                "5.  Gana hasta 3 estrellas según tu desempeño.\n\n" +
                "Atajos: [R] reinicia la partida y [Esc] regresa al menú.";
            Txt(canvas, "Texto", body, 38, Palette.Tinta, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(60, 90), new Vector2(1200, 560));

            MakeMascot(canvas, art, anims.mascot, new Vector2(-620, 330), 170);
            Button back = Btn(canvas, "BotonVolver", "VOLVER", new Vector2(0, -330), new Vector2(460, 96), Palette.Magenta, Palette.Crema, art, 48);

            Footer(canvas, "Toca la calavera para saludarla");
            AddFader(canvas);

            var ic = new GameObject("InstructionsController").AddComponent<InstructionsController>();
            ic.backButton = back;
            SaveScene(SceneNames.Instructions);
        }

        static void BuildGame(Art art, Anims anims, CardView cardPrefab)
        {
            Transform canvas = NewSceneWithCanvas(art);
            Vector2 top = new Vector2(0.5f, 1f);

            // Tablero (el tamaño lo calcula GameController según la dificultad)
            RectTransform board = R("Tablero", canvas, new Vector2(0, -85), new Vector2(1000, 800));
            var boardImg = board.gameObject.AddComponent<Image>();
            boardImg.sprite = art.rounded; boardImg.type = Image.Type.Sliced;
            boardImg.color = new Color(1f, 0.957f, 0.839f, 0.45f); boardImg.raycastTarget = false;
            var grid = board.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(175, 175);
            grid.spacing = new Vector2(18, 18);
            grid.padding = new RectOffset(30, 30, 30, 30);
            grid.childAlignment = TextAnchor.MiddleCenter;

            // HUD
            Mascot mascot = MakeMascot(canvas, art, anims.mascot, new Vector2(130, -105), 160, new Vector2(0f, 1f));
            Text moves = Chip(canvas, art, "MOVIMIENTOS", new Vector2(-370, -100), new Vector2(340, 120), top);
            Text time = Chip(canvas, art, "TIEMPO", new Vector2(0, -100), new Vector2(340, 120), top);
            Text pairs = Chip(canvas, art, "PARES", new Vector2(370, -100), new Vector2(340, 120), top);
            Button restart = Btn(canvas, "BotonReiniciar", "REINICIAR", new Vector2(-200, -60), new Vector2(320, 72), Palette.Cian, Palette.Tinta, art, 34, new Vector2(1f, 1f));
            Button menu = Btn(canvas, "BotonMenu", "MENÚ", new Vector2(-200, -145), new Vector2(320, 72), Palette.Tinta, Palette.Crema, art, 34, new Vector2(1f, 1f));
            AddFader(canvas);

            var gc = new GameObject("GameController").AddComponent<GameController>();
            gc.cardPrefab = cardPrefab; gc.board = board; gc.grid = grid; gc.symbols = art.symbols;
            gc.movesText = moves; gc.timeText = time; gc.pairsText = pairs;
            gc.restartButton = restart; gc.menuButton = menu; gc.mascot = mascot;
            SaveScene(SceneNames.Game);
        }

        static void BuildResults(Art art, Anims anims)
        {
            Transform canvas = NewSceneWithCanvas(art);

            Img(canvas, "Disco", art.symbols[0], Palette.Crema, new Vector2(-640, 0), new Vector2(520, 520));
            Mascot mascot = MakeMascot(canvas, art, anims.mascot, new Vector2(-640, 0), 400);

            Transform panel = Panel(canvas, "Panel", art, Palette.Crema, new Vector2(200, 0), new Vector2(1100, 860)).transform;
            Txt(panel, "Titulo", "¡NIVEL COMPLETADO!", 76, Palette.Tinta, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 330), new Vector2(1000, 110));
            Text message = Txt(panel, "Mensaje", "", 44, Palette.Magenta, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 245), new Vector2(1000, 60));

            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
                stars[i] = Img(panel, "Estrella_" + i, art.symbols[3], Palette.Gris, new Vector2((i - 1) * 190, 120), new Vector2(150, 150));

            string[] labels = { "MOVIMIENTOS", "TIEMPO", "PUNTUACIÓN", "MEJOR PUNTUACIÓN" };
            var values = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y = -20 - i * 78;
                Txt(panel, "Etiqueta_" + i, labels[i], 38, Palette.Tinta, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(-170, y), new Vector2(500, 60));
                values[i] = Txt(panel, "Valor_" + i, "0", 46, i >= 2 ? Palette.Magenta : Palette.Tinta, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(230, y), new Vector2(300, 60));
            }

            Button again = Btn(panel, "BotonJugarDeNuevo", "JUGAR DE NUEVO", new Vector2(-260, -365), new Vector2(480, 90), Palette.Magenta, Palette.Crema, art, 38);
            Button menu = Btn(panel, "BotonMenu", "MENÚ PRINCIPAL", new Vector2(260, -365), new Vector2(480, 90), Palette.Tinta, Palette.Crema, art, 38);
            AddFader(canvas);

            var rc = new GameObject("ResultsController").AddComponent<ResultsController>();
            rc.messageText = message; rc.movesText = values[0]; rc.timeText = values[1]; rc.scoreText = values[2]; rc.bestText = values[3];
            rc.stars = stars; rc.playAgainButton = again; rc.menuButton = menu; rc.mascot = mascot;
            SaveScene(SceneNames.Results);
        }

        // ------------------------------------------------------------------ CONFIGURACIÓN
        static void RegisterScenes()
        {
            string[] names = { SceneNames.Menu, SceneNames.Instructions, SceneNames.Game, SceneNames.Results };
            var list = new List<EditorBuildSettingsScene>();
            var ours = new HashSet<string>();
            foreach (string n in names) { list.Add(new EditorBuildSettingsScene(ScenePath(n), true)); ours.Add(ScenePath(n)); }
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (!ours.Contains(s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }
    }
}
