using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Save;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 選択中のフォルダ配下の ScriptableObject 名称や FlagKind/ItemKind を一括変更するツール。
///  - Tools > Items > Rename Flags in Folder
///  - フォルダ名を新しいトークンとして、ファイル名と m_Name 内の古いトークンを置換
///  - FlagKind / ItemKind を必要なアセットにセット
/// </summary>
public class RenameFlagsInFolderWindow : EditorWindow
{
    private string _folderPath;
    private string _oldToken;
    private string _newToken;

    private int _flagKindIndex = -1;
    private int _itemKindIndex = -1;

    private string[] _flagNames;
    private int[] _flagValues;
    private string[] _itemNames;
    private int[] _itemValues;

    [MenuItem("Tools/Items/Rename Flags in Folder")]
    private static void Open()
    {
        GetWindow<RenameFlagsInFolderWindow>("Rename Flags").Show();
    }

    private void OnEnable()
    {
        LoadEnums();
        TrySetSelectionFolder();
    }

    private void OnSelectionChange()
    {
        TrySetSelectionFolder();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("選択中フォルダの Flag/Item 名称＆値を一括変更", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("対象フォルダ", _folderPath ?? "(未選択)");
        if (GUILayout.Button("現在の選択を対象にする"))
        {
            TrySetSelectionFolder(force:true);
        }

        _oldToken = EditorGUILayout.TextField("置換する古いトークン", _oldToken);
        _newToken = EditorGUILayout.TextField("新しいトークン (通常はフォルダ名)", _newToken);

        EditorGUILayout.Space();
        _flagKindIndex = EditorGUILayout.Popup("FlagKind をセット", _flagKindIndex, PrependSkip(_flagNames));
        _itemKindIndex = EditorGUILayout.Popup("ItemKind をセット", _itemKindIndex, PrependSkip(_itemNames));
        EditorGUILayout.HelpBox("「(変更しない)」を選ぶとその項目は触りません。", MessageType.Info);

        EditorGUILayout.Space();
        if (GUILayout.Button("実行"))
        {
            Apply();
        }
    }

    private void LoadEnums()
    {
        (_flagNames, _flagValues) = LoadEnum(typeof(SaveData.SaveFlag));
        (_itemNames, _itemValues) = LoadEnum(typeof(SaveData.ItemKind));
    }

    private static (string[], int[]) LoadEnum(Type t)
    {
        var names = Enum.GetNames(t);
        var vals = Enum.GetValues(t).Cast<int>().ToArray();
        return (names, vals);
    }

    private string[] PrependSkip(string[] arr)
    {
        var list = new List<string> {"(変更しない)"};
        list.AddRange(arr);
        return list.ToArray();
    }

    private void TrySetSelectionFolder(bool force = false)
    {
        var sel = Selection.activeObject;
        var path = sel != null ? AssetDatabase.GetAssetPath(sel) : null;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(path))
        {
            path = Path.GetDirectoryName(path);
        }

        if (!AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        if (!force && path == _folderPath)
        {
            return;
        }

        _folderPath = path;
        var folderName = Path.GetFileName(path);
        _newToken = folderName;

        // 推定: フォルダ内の最初の asset から KEY4 のような古いトークンを推測
        var guid = AssetDatabase.FindAssets("t:ScriptableObject", new[] {_folderPath}).FirstOrDefault();
        if (!string.IsNullOrEmpty(guid))
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var file = Path.GetFileNameWithoutExtension(assetPath);
            _oldToken = GuessToken(file);
        }
    }

    private static string GuessToken(string fileName)
    {
        // 例: AFC_KEY4_Set → KEY4
        var m = Regex.Match(fileName, @"^[A-Z]+_([^_]+)");
        if (m.Success)
        {
            return m.Groups[1].Value;
        }
        return fileName;
    }

    private void Apply()
    {
        if (string.IsNullOrEmpty(_folderPath) || !AssetDatabase.IsValidFolder(_folderPath))
        {
            EditorUtility.DisplayDialog("Rename Flags", "有効なフォルダを選択してください。", "OK");
            return;
        }

        if (string.IsNullOrEmpty(_oldToken) || string.IsNullOrEmpty(_newToken))
        {
            EditorUtility.DisplayDialog("Rename Flags", "置換するトークンを入力してください。", "OK");
            return;
        }

        bool setFlag = _flagKindIndex > 0;
        bool setItem = _itemKindIndex > 0;
        int flagVal = setFlag ? _flagValues[_flagKindIndex - 1] : -1;
        int itemVal = setItem ? _itemValues[_itemKindIndex - 1] : -1;

        var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] {_folderPath});
        int renamed = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) continue;

            var so = new SerializedObject(asset);
            bool dirty = false;

            // m_Name 置換
            if (!string.IsNullOrEmpty(asset.name) && asset.name.Contains(_oldToken))
            {
                var newName = asset.name.Replace(_oldToken, _newToken);
                so.FindProperty("m_Name").stringValue = newName;
                dirty = true;
            }

            // FlagKind / ItemKind 設定（配列にも対応）
            if (setFlag && TrySetIntProperty(so, "FlagKind", flagVal))
            {
                dirty = true;
            }
            if (setItem && TrySetIntProperty(so, "ItemKind", itemVal))
            {
                dirty = true;
            }

            if (dirty)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            // ファイル名置換
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Contains(_oldToken))
            {
                var newFile = fileName.Replace(_oldToken, _newToken) + ".asset";
                var newPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, newFile).Replace("\\", "/");
                AssetDatabase.RenameAsset(path, Path.GetFileNameWithoutExtension(newFile));
                renamed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Rename Flags", $"処理完了: {renamed} ファイルをリネーム/更新しました。", "OK");
    }

    private static bool TrySetIntProperty(SerializedObject so, string propName, int value)
    {
        var p = so.FindProperty(propName);
        if (p == null)
        {
            return false;
        }

        if (p.isArray)
        {
            for (int i = 0; i < p.arraySize; i++)
            {
                var elem = p.GetArrayElementAtIndex(i);
                elem.intValue = value;
            }
            return true;
        }

        p.intValue = value;
        return true;
    }
}
