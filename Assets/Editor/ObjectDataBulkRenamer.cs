using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ObjectAssets;
using ObjectAssets.Condition;
using Save;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ObjectData 配下で ObjectConditionFlag / ObjectAssetAddFlagControl / ObjectAssetFlagControl を
/// Stage名 + サブフォルダ名 + 種類(Add/ConditionA../Reset)の形式に一括リネームするツール。
/// </summary>
public static class ObjectDataBulkRenamer
{
	[MenuItem("Tools/ObjectData/Rename Flags In Folder")]
	public static void RenameSelectedFolder()
	{
		var targetFolder = GetSelectedFolder();
		if (string.IsNullOrEmpty(targetFolder))
		{
			EditorUtility.DisplayDialog("Rename Flags", "Project ビューでリネームしたいフォルダを 1 つ選択してください。", "OK");
			return;
		}

		if (!TryParseStageAndLeaf(targetFolder, out var stageName, out var leafFolder))
		{
			EditorUtility.DisplayDialog("Rename Flags", "選択フォルダのパスに \"StageX\" を含めてください。例: Assets/Resources/ObjectData/Stage5/ALFA_SLOT1", "OK");
			return;
		}

		var changed = false;
		AssetDatabase.StartAssetEditing();
		try
		{
			changed |= RenameFixedType<ObjectAssetAddFlagControl>(targetFolder, stageName, leafFolder, "Add");
			changed |= RenameFixedType<ObjectAssetFlagControl>(targetFolder, stageName, leafFolder, "Reset");
			changed |= RenameConditions(targetFolder, stageName, leafFolder);
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
		}

		if (changed)
		{
			AssetDatabase.SaveAssets();
			Debug.Log($"[Rename Flags] {targetFolder} を {stageName}_{leafFolder}_* 形式でリネームしました。");
		}
		else
		{
			Debug.Log("[Rename Flags] 変更はありません。");
		}
	}

	[MenuItem("Tools/ObjectData/Set FlagKind In Folder")]
	public static void SetFlagKindSelectedFolder()
	{
		var targetFolder = GetSelectedFolder();
		if (string.IsNullOrEmpty(targetFolder))
		{
			EditorUtility.DisplayDialog("Set FlagKind", "Project ビューで対象フォルダを 1 つ選択してください。", "OK");
			return;
		}

		FlagKindSetterWizard.Open(targetFolder);
	}

	[MenuItem("Tools/ObjectData/Set Number Button FlagKind In Folder")]
	public static void SetNumberButtonFlagKindSelectedFolder()
	{
		var targetFolder = GetSelectedFolder();
		if (string.IsNullOrEmpty(targetFolder))
		{
			EditorUtility.DisplayDialog("Set Number Button FlagKind", "Project ビューで対象フォルダを 1 つ選択してください。", "OK");
			return;
		}

		NumberButtonFlagKindWizard.Open(targetFolder);
	}

	private static bool SetFlagKindInFolder(string folder, SaveData.SaveFlag newFlag)
	{
		var changed = false;

		changed |= SetConditionFlagKind(folder, newFlag);
		changed |= SetAddFlagKind(folder, newFlag);
		changed |= SetResetFlagKind(folder, newFlag);

		if (changed)
		{
			AssetDatabase.SaveAssets();
		}

		return changed;
	}

	private static bool SetConditionFlagKind(string folder, SaveData.SaveFlag newFlag)
	{
		var guids = AssetDatabase.FindAssets("t:ObjectConditionFlag", new[] { folder });
		bool changed = false;
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var asset = AssetDatabase.LoadAssetAtPath<ObjectConditionFlag>(path);
			if (asset == null)
			{
				continue;
			}

			if (asset.FlagKind == newFlag)
			{
				continue;
			}

			Undo.RecordObject(asset, "Set FlagKind");
			asset.FlagKind = newFlag;
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		return changed;
	}

	private static bool SetAddFlagKind(string folder, SaveData.SaveFlag newFlag)
	{
		var guids = AssetDatabase.FindAssets("t:ObjectAssetAddFlagControl", new[] { folder });
		bool changed = false;
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var asset = AssetDatabase.LoadAssetAtPath<ObjectAssetAddFlagControl>(path);
			if (asset == null)
			{
				continue;
			}

			if (asset.FlagKind != null && asset.FlagKind.Length == 1 && asset.FlagKind[0] == newFlag)
			{
				continue;
			}

			Undo.RecordObject(asset, "Set FlagKind");
			asset.FlagKind = new[] { newFlag };
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		return changed;
	}

	private static bool SetResetFlagKind(string folder, SaveData.SaveFlag newFlag)
	{
		var guids = AssetDatabase.FindAssets("t:ObjectAssetFlagControl", new[] { folder });
		bool changed = false;
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var asset = AssetDatabase.LoadAssetAtPath<ObjectAssetFlagControl>(path);
			if (asset == null)
			{
				continue;
			}

			if (asset.FlagKind != null && asset.FlagKind.Length == 1 && asset.FlagKind[0] == newFlag)
			{
				continue;
			}

			Undo.RecordObject(asset, "Set FlagKind");
			asset.FlagKind = new[] { newFlag };
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		return changed;
	}

	private static bool RenameFixedType<T>(string folder, string stageName, string leafFolder, string suffix) where T : ScriptableObject
	{
		var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
		if (guids.Length == 0)
		{
			return false;
		}

		var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var changed = false;
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var asset = AssetDatabase.LoadAssetAtPath<T>(path);
			if (asset == null)
			{
				continue;
			}

			var baseName = $"{stageName}_{leafFolder}_{suffix}";
			var newName = MakeUnique(baseName, usedNames);
			usedNames.Add(newName);

			if (!ApplyRename(asset, path, newName))
			{
				continue;
			}

			changed = true;
		}

		return changed;
	}

	private static bool RenameConditions(string folder, string stageName, string leafFolder)
	{
		var guids = AssetDatabase.FindAssets("t:ObjectConditionFlag", new[] { folder });
		if (guids.Length == 0)
		{
			return false;
		}

		var paths = guids
			.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
			.OrderBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
			.ToArray();

		int maxUpper = paths
			.Select(path => AssetDatabase.LoadAssetAtPath<ObjectConditionFlag>(path))
			.Where(a => a != null && a.Comparison == ObjectConditionBase.COMPARISON.UPPER)
			.Select(a => a.Num)
			.DefaultIfEmpty(-1)
			.Max();

		var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var changed = false;
		for (int i = 0; i < paths.Length; i++)
		{
			var path = paths[i];
			var asset = AssetDatabase.LoadAssetAtPath<ObjectConditionFlag>(path);
			if (asset == null)
			{
				continue;
			}

			var suffix = BuildConditionSuffix(asset, maxUpper, i);
			var baseName = $"{stageName}_{leafFolder}_{suffix}";
			var newName = MakeUnique(baseName, usedNames);
			usedNames.Add(newName);

			if (!ApplyRename(asset, path, newName))
			{
				continue;
			}

			changed = true;
		}

		return changed;
	}

	private static string BuildConditionSuffix(ObjectConditionFlag asset, int maxUpper, int fallbackIndex)
	{
		switch (asset.Comparison)
		{
			case ObjectConditionBase.COMPARISON.EQUAL:
				return $"Condition{IndexToLetter(asset.Num)}";
			case ObjectConditionBase.COMPARISON.UPPER:
				var overValue = maxUpper >= 0 ? maxUpper : asset.Num;
				return $"Over{overValue}";
			default:
				return $"Condition{IndexToLetter(fallbackIndex)}";
		}
	}

	private static bool ApplyRename(UnityEngine.Object asset, string assetPath, string newName)
	{
		var currentName = Path.GetFileNameWithoutExtension(assetPath);
		if (string.Equals(currentName, newName, StringComparison.Ordinal) && string.Equals(asset.name, newName, StringComparison.Ordinal))
		{
			return false;
		}

		asset.name = newName;
		AssetDatabase.RenameAsset(assetPath, newName);
		EditorUtility.SetDirty(asset);
		return true;
	}

	private static string MakeUnique(string baseName, HashSet<string> usedNames)
	{
		if (!usedNames.Contains(baseName))
		{
			return baseName;
		}

		int index = 1;
		string candidate;
		do
		{
			index++;
			candidate = $"{baseName}_{index:D2}";
		} while (usedNames.Contains(candidate));

		return candidate;
	}

	private static string IndexToLetter(int index)
	{
		const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		if (index < letters.Length)
		{
			return letters[index].ToString();
		}

		var first = letters[index / letters.Length - 1];
		var second = letters[index % letters.Length];
		return $"{first}{second}";
	}

	private static bool TryParseStageAndLeaf(string folderPath, out string stageName, out string leafFolder)
	{
		stageName = null;
		leafFolder = null;

		var parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
		stageName = parts.FirstOrDefault(p => p.StartsWith("Stage", StringComparison.OrdinalIgnoreCase));
		leafFolder = parts.LastOrDefault();

		return !string.IsNullOrEmpty(stageName) && !string.IsNullOrEmpty(leafFolder);
	}

	private static string GetSelectedFolder()
	{
		var guid = Selection.assetGUIDs.FirstOrDefault();
		if (guid == null)
		{
			return null;
		}

		var path = AssetDatabase.GUIDToAssetPath(guid);
		return AssetDatabase.IsValidFolder(path) ? path : null;
	}

	private class FlagKindSetterWizard : ScriptableWizard
	{
		public SaveData.SaveFlag NewFlag;

		[HideInInspector] public string TargetFolder;

		public static void Open(string folder)
		{
			var window = DisplayWizard<FlagKindSetterWizard>("Set FlagKind", "適用");
			window.TargetFolder = folder;
		}

		private void OnWizardCreate()
		{
			if (string.IsNullOrEmpty(TargetFolder))
			{
				EditorUtility.DisplayDialog("Set FlagKind", "フォルダが指定されていません。やり直してください。", "OK");
				return;
			}

			var changed = SetFlagKindInFolder(TargetFolder, NewFlag);
			if (changed)
			{
				Debug.Log($"[Set FlagKind] {TargetFolder} 内の FlagKind を {NewFlag} に更新しました。");
			}
			else
			{
				Debug.Log("[Set FlagKind] 変更はありませんでした。");
			}
		}
	}

	private class NumberButtonFlagKindWizard : ScriptableWizard
	{
		public SaveData.SaveFlag NewFlag;

		[HideInInspector] public string TargetFolder;

		public static void Open(string folder)
		{
			var window = DisplayWizard<NumberButtonFlagKindWizard>("Set Number Button FlagKind", "適用");
			window.TargetFolder = folder;
		}

		private void OnWizardCreate()
		{
			if (string.IsNullOrEmpty(TargetFolder))
			{
				EditorUtility.DisplayDialog("Set Number Button FlagKind", "フォルダが指定されていません。やり直してください。", "OK");
				return;
			}

			var changed = SetFlagKindInFolder(TargetFolder, NewFlag);
			if (changed)
			{
				Debug.Log($"[Set Number Button FlagKind] {TargetFolder} 内の FlagKind を {NewFlag} に更新しました。");
			}
			else
			{
				Debug.Log("[Set Number Button FlagKind] 変更はありませんでした。");
			}
		}
	}
}
