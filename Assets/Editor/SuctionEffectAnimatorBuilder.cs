using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// 一键把 Assets/Arts/UI/第一关特效/吸附特效/ 下每个子文件夹的 Sprite 序列
    ///   - 生成一份 10fps 非循环 AnimationClip
    ///   - 生成一份同名 AnimatorController（单 state、该 state 为 default）
    ///   - 旧错命名的 .controller 一并清掉
    /// 行为：挂到带 SpriteRenderer 的 GameObject 上后，GameObject active 时进入默认 state，
    /// 播放一次后停在最后一帧（loopTime = false），不会再触发。
    /// </summary>
    public static class SuctionEffectAnimatorBuilder
    {
        private const string RootDir = "Assets/Arts/UI/第一关特效/吸附特效";
        private const float Fps = 10f;

        private static readonly Regex TrailingIndex = new Regex(@"-(\d+)$");

        [MenuItem("Tools/GameCreate3/吸附特效 — 生成动画器与一次性动画", priority = 200)]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(RootDir))
            {
                EditorUtility.DisplayDialog("吸附特效", $"找不到目录:\n{RootDir}", "OK");
                return;
            }

            int deleted = 0;
            int folders = 0;
            int clips = 0;
            int controllers = 0;
            var summary = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                // 1) 删除根目录下旧的 .controller（任意命名都清，统一重建）
                foreach (var path in Directory
                             .GetFiles(RootDir, "*.controller", SearchOption.TopDirectoryOnly)
                             .Select(p => p.Replace('\\', '/')))
                {
                    if (AssetDatabase.DeleteAsset(path)) deleted++;
                }

                // 2) 遍历每个子文件夹，按文件夹名生成 .anim + .controller
                foreach (var folder in AssetDatabase.GetSubFolders(RootDir))
                {
                    folders++;
                    var folderName = Path.GetFileName(folder);

                    var sprites = LoadSpritesSortedByTrailingIndex(folder);
                    if (sprites.Count == 0)
                    {
                        Debug.LogWarning($"[SuctionEffect] '{folder}' 内未找到 Sprite，跳过。");
                        summary.Add($"  [skip] {folderName}");
                        continue;
                    }

                    var clipPath = $"{RootDir}/{folderName}.anim";
                    var controllerPath = $"{RootDir}/{folderName}.controller";

                    var clip = BuildOrReplaceClip(clipPath, sprites, Fps);
                    clips++;

                    BuildOrReplaceController(controllerPath, clip);
                    controllers++;

                    summary.Add($"  [ok]   {folderName}  ({sprites.Count} 帧)");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                $"[SuctionEffect] 完成。删除旧 controller: {deleted}; 处理文件夹: {folders}; " +
                $"Clip: {clips}; Controller: {controllers}\n" + string.Join("\n", summary));
            EditorUtility.DisplayDialog("吸附特效",
                $"完成。\n清理旧 controller: {deleted}\n处理文件夹: {folders}\n生成 Clip: {clips}\n生成 Controller: {controllers}\n\n" +
                string.Join("\n", summary), "OK");
        }

        private static List<Sprite> LoadSpritesSortedByTrailingIndex(string folder)
        {
            var pngGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            var list = new List<(int idx, Sprite s)>();

            foreach (var g in pngGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (dir != folder) continue; // 排除子目录里的（理论上没有，但保险）

                var name = Path.GetFileNameWithoutExtension(path);
                var m = TrailingIndex.Match(name);
                int idx = m.Success ? int.Parse(m.Groups[1].Value) : int.MaxValue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) list.Add((idx, sprite));
            }

            return list.OrderBy(t => t.idx).Select(t => t.s).ToList();
        }

        private static AnimationClip BuildOrReplaceClip(string clipPath, List<Sprite> sprites, float fps)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { frameRate = fps };
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                clip.frameRate = fps;
                clip.ClearCurves();
            }

            // 把 sprite 序列绑到 UI Image.m_Sprite（path 留空 = Animator 同 GO 上的 Image）
            var binding = EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite");
            var keyCount = sprites.Count + 1;
            var keys = new ObjectReferenceKeyframe[keyCount];
            float step = 1f / fps;
            for (int i = 0; i < sprites.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i * step, value = sprites[i] };
            }
            keys[sprites.Count] = new ObjectReferenceKeyframe { time = sprites.Count * step, value = null };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var alphaBinding = EditorCurveBinding.FloatCurve("", typeof(Image), "m_Color.a");
            var lastSpriteTime = Mathf.Max(0f, (sprites.Count - 1) * step);
            var alphaCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(lastSpriteTime, 1f),
                new Keyframe(sprites.Count * step, 0f));
            AnimationUtility.SetEditorCurve(clip, alphaBinding, alphaCurve);

            // 一次性：不循环；loopTime=false 时 Animator 会在 state 内停在最后一帧。
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            settings.stopTime = keyCount * step;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = WrapMode.Once;

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void BuildOrReplaceController(string controllerPath, AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            var ac = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm = ac.layers[0].stateMachine;

            var stateName = Path.GetFileNameWithoutExtension(controllerPath);
            var state = sm.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = true;

            sm.defaultState = state;
            EditorUtility.SetDirty(ac);
        }
    }
}
