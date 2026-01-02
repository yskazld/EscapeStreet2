using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ObjectAssets;
using ObjectAssets.Condition;
using Stage.Object;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// OrderButton 用の SEQ 差し替えツール。
/// - 任意ステージの SEQ フォルダを指定
/// - R/G の点灯順を入力
/// - 選択中の親配下にある Step0..StepN/Reset オブジェクト(ObjectBase)へ条件とアクションを自動セット
/// </summary>
public class OrderButtonSeqAutoAssignWindow : EditorWindow
{
    private string _stageName = "Stage7";
    private string _sequenceText = "R,R,G,G,R,L";

    [MenuItem("Tools/OrderButtons/Assign SEQ")]
    private static void Open()
    {
        GetWindow<OrderButtonSeqAutoAssignWindow>("OrderButton SEQ").Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("OrderButton SEQ 自動セット", EditorStyles.boldLabel);
        _stageName = EditorGUILayout.TextField("Stage フォルダ名", _stageName);
        _sequenceText = EditorGUILayout.TextField("点灯順 (R/G をカンマ区切り)", _sequenceText);

        EditorGUILayout.Space();
        if (GUILayout.Button("選択中に適用"))
        {
            Apply();
        }
    }

    private void Apply()
    {
        var selections = Selection.gameObjects;
        if (selections == null || selections.Length == 0)
        {
            EditorUtility.DisplayDialog("OrderButton SEQ", "何かの親オブジェクトを選択してください。", "OK");
            return;
        }

        var folder = $"Assets/Resources/ObjectData/{_stageName}/SEQ";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("OrderButton SEQ", $"フォルダが見つかりません: {folder}", "OK");
            return;
        }

        var seq = ParseSequence(_sequenceText);
        if (seq == null || seq.Count == 0)
        {
            EditorUtility.DisplayDialog("OrderButton SEQ", "点灯順は R/G をカンマまたは連続文字で入力してください。例: R,R,G,G,R,L", "OK");
            return;
        }

        var lookup = BuildAssetLookup(folder);
        if (lookup.KeyMap.Count == 0 && lookup.NameMap.Count == 0)
        {
            EditorUtility.DisplayDialog("OrderButton SEQ", "SEQ フォルダ内のアセットを読み込めませんでした。", "OK");
            return;
        }

        var updatedCount = 0;
        foreach (var root in selections)
        {
            foreach (var ob in root.GetComponentsInChildren<ObjectBase>(true))
            {
                if (TryAssign(ob, seq, lookup))
                {
                    updatedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("OrderButton SEQ", $"{updatedCount} 個の ObjectBase に適用しました。", "OK");
    }

    private static List<ColorCode> ParseSequence(string text)
    {
        var cleaned = text.Replace(",", "").Replace(" ", "").ToUpperInvariant();
        var list = new List<ColorCode>();
        foreach (var ch in cleaned)
        {
            if (ch == 'R')
            {
                list.Add(ColorCode.Red);
            }
            else if (ch == 'G')
            {
                list.Add(ColorCode.Green);
            }
        }
        return list;
    }

    private class AssetLookup
    {
        public Dictionary<string, Object> KeyMap = new Dictionary<string, Object>();
        public Dictionary<string, Object> NameMap = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);
    }

    private static AssetLookup BuildAssetLookup(string folder)
    {
        var lookup = new AssetLookup();
        var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null)
            {
                continue;
            }

            var key = BuildKey(asset);
            if (!string.IsNullOrEmpty(key) && !lookup.KeyMap.ContainsKey(key))
            {
                lookup.KeyMap.Add(key, asset);
            }

            if (!lookup.NameMap.ContainsKey(asset.name))
            {
                lookup.NameMap.Add(asset.name, asset);
            }
        }
        return lookup;
    }

    private static string BuildKey(Object asset)
    {
        if (asset == null)
        {
            return null;
        }

        var so = new SerializedObject(asset);
        var flagKind = GetInt(so, "FlagKind", -1);
        if (flagKind < 0)
        {
            return null;
        }

        if (asset is ObjectConditionFlag)
        {
            var comp = GetInt(so, "Comparison", -1);
            var num = GetInt(so, "Num", -1);
            return $"C:{flagKind}:{comp}:{num}";
        }

        if (asset is ObjectAssetFlagControl)
        {
            var setNum = GetInt(so, "SetNum", int.MinValue);
            return $"S:{flagKind}:{setNum}";
        }

        return null;
    }

    private static int GetInt(SerializedObject so, string prop, int fallback)
    {
        var p = so.FindProperty(prop);
        return p != null ? p.intValue : fallback;
    }

    private static bool TryAssign(ObjectBase ob, List<ColorCode> seq, AssetLookup lookup)
    {
        var name = ob.name.ToLowerInvariant();
        bool isStep = TryParseStepIndex(name, out var stepIndex);
        bool isReset = name.Contains("reset");
        if (!isStep && !isReset)
        {
            return false;
        }

        var so = new SerializedObject(ob);
        if (isStep)
        {
            if (stepIndex < 0 || stepIndex >= seq.Count)
            {
                return false;
            }

            var colorCurrent = seq[stepIndex];
            var colorNext = stepIndex + 1 < seq.Count ? seq[stepIndex + 1] : ColorCode.Finish;
            var nextStepIndex = Mathf.Min(stepIndex + 1, seq.Count); // 最終は seq.Count をセット（存在しなければ 0 にフォールバック）

            var cond = new List<Object>();
            cond.Add(GetSeqStep(lookup, stepIndex));
            cond.Add(GetNextColorCondition(lookup, colorCurrent));

            var acts = new List<Object>();
            acts.Add(GetSetNextColor(lookup, colorNext));
            var setSeq = GetSetSeqStep(lookup, nextStepIndex, allowClosest:true) ?? GetSetSeqStep(lookup, 0, allowClosest:true);
            if (setSeq != null) acts.Add(setSeq);

            return WriteArrays(so, cond, acts);
        }

        // Reset
        var buttonColor = GuessButtonColor(name);
        var opposite = buttonColor == ColorCode.Red ? ColorCode.Green : ColorCode.Red;
        var initial = seq[0];
        var under = GetSeqUnder(lookup, seq.Count);

        var condReset = new List<Object>();
        if (under != null) condReset.Add(under);
        condReset.Add(GetNextColorCondition(lookup, opposite));

        var actsReset = new List<Object>();
        actsReset.Add(GetSetNextColor(lookup, initial));
        var setStep0 = GetSetSeqStep(lookup, 0);
        if (setStep0 != null) actsReset.Add(setStep0);
        var buzzer = FindBuzzer(lookup);
        if (buzzer != null) actsReset.Add(buzzer);

        return WriteArrays(so, condReset, actsReset);
    }

    private static bool TryParseStepIndex(string name, out int step)
    {
        var m = Regex.Match(name, "step(\\d+)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var v))
        {
            step = v;
            return true;
        }
        step = -1;
        return false;
    }

    private static ColorCode GuessButtonColor(string name)
    {
        if (name.Contains("right") || name.Contains("red"))
        {
            return ColorCode.Red;
        }
        if (name.Contains("left") || name.Contains("green"))
        {
            return ColorCode.Green;
        }
        return ColorCode.Red;
    }

    private static bool WriteArrays(SerializedObject so, List<Object> cond, List<Object> acts)
    {
        var changed = false;
        changed |= WriteArray(so, "_clicktConditionList", cond);
        changed |= WriteArray(so, "_objectDataList", acts);
        if (changed)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(so.targetObject);
        }
        return changed;
    }

    private static bool WriteArray(SerializedObject so, string propName, List<Object> values)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || !prop.isArray)
        {
            return false;
        }
        prop.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        return true;
    }

    private static Object GetSeqStep(AssetLookup lookup, int step)
    {
        lookup.KeyMap.TryGetValue(KeyCondition(147, 0, step), out var obj);
        return obj;
    }

    private static Object GetSeqUnder(AssetLookup lookup, int stepCount)
    {
        lookup.KeyMap.TryGetValue(KeyCondition(147, 2, stepCount), out var obj);
        return obj;
    }

    private static Object GetNextColorCondition(AssetLookup lookup, ColorCode color)
    {
        var num = ColorToNum(color);
        if (lookup.KeyMap.TryGetValue(KeyCondition(148, 0, num), out var obj))
        {
            return obj;
        }

        // 名前フォールバック
        var token = color == ColorCode.Red ? "Red" :
                    color == ColorCode.Green ? "Green" : "Finish";
        if (!string.IsNullOrEmpty(token))
        {
            if (lookup.NameMap.TryGetValue($"NextColor_{token}={num}", out var byName))
            {
                return byName;
            }
            var any = lookup.NameMap.Values.FirstOrDefault(o => o is ObjectConditionFlag && o.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
            if (any != null) return any;
        }

        return null;
    }

    private static Object GetSetNextColor(AssetLookup lookup, ColorCode color)
    {
        var num = ColorToNum(color);
        if (lookup.KeyMap.TryGetValue(KeySet(94000000, num), out var direct))
        {
            return direct;
        }

        // フォールバック: 名前で色を探す（Red/Green/Finish）
        string token = color switch
        {
            ColorCode.Red => "Red",
            ColorCode.Green => "Green",
            ColorCode.Finish => "Finish",
            _ => null
        };

        if (!string.IsNullOrEmpty(token))
        {
            if (lookup.NameMap.TryGetValue($"Set_NextColor_{token}={num}", out var byName))
            {
                return byName;
            }

            var found = lookup.NameMap.Values.FirstOrDefault(o =>
                o is ObjectAssetFlagControl &&
                o.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
            if (found != null)
            {
                return found;
            }
        }

        // さらに見つからない場合は Set_NextColor の何か一つを返す
        var any = lookup.NameMap.Values.FirstOrDefault(o => o is ObjectAssetFlagControl oa && oa.name.Contains("NextColor", StringComparison.OrdinalIgnoreCase));
        return any;
    }

    private static Object GetSetSeqStep(AssetLookup lookup, int step, bool allowClosest = false)
    {
        if (lookup.KeyMap.TryGetValue(KeySet(93000000, step), out var obj))
        {
            return obj;
        }

        // 名前で直接探す
        var nameKey = $"Set_SEQ_STEP={step}";
        if (lookup.NameMap.TryGetValue(nameKey, out var byName))
        {
            return byName;
        }

        if (!allowClosest)
        {
            return null;
        }

        // 近い SetNum を探す（例: 最大が 6 の場合、7 でも 6 を返す）
        var candidates = lookup.KeyMap
            .Where(kv => kv.Key.StartsWith("S:93000000:"))
            .Select(kv => (kv.Value, SetNum: ParseSetNum(kv.Key)))
            .Where(t => t.SetNum >= 0)
            .OrderBy(t => Math.Abs(t.SetNum - step))
            .ToList();
        if (candidates.Count > 0)
        {
            return candidates.First().Value;
        }

        // さらに何もなければ 93000000 を含む何か一つ
        var any = lookup.NameMap.Values.FirstOrDefault(o =>
            o is ObjectAssetFlagControl && o.name.IndexOf("SEQ_STEP", StringComparison.OrdinalIgnoreCase) >= 0);
        return any;
    }

    private static int ParseSetNum(string key)
    {
        // key format: S:{flag}:{setnum}
        var parts = key.Split(':');
        if (parts.Length == 3 && int.TryParse(parts[2], out var v))
        {
            return v;
        }
        return -1;
    }

    private static Object FindBuzzer(AssetLookup lookup)
    {
        // name で拾う（ObjectAssetSound なので key が無い）
        foreach (var kv in lookup.NameMap)
        {
            if (kv.Key.IndexOf("buzzer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return kv.Value;
            }
        }
        return null;
    }

    private static int ColorToNum(ColorCode c)
    {
        return c switch
        {
            ColorCode.Red => 0,
            ColorCode.Green => 1,
            ColorCode.Finish => 2,
            _ => 0
        };
    }

    private static string KeyCondition(int flagKind, int comp, int num) => $"C:{flagKind}:{comp}:{num}";
    private static string KeySet(int flagKind, int setNum) => $"S:{flagKind}:{setNum}";

    private enum ColorCode
    {
        Red,
        Green,
        Finish
    }
}
