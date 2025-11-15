using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ObjectAssets;
using ObjectAssets.Condition;
using Stage.Object;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EditorExtensions
{
	/// <summary>
	/// Adds convenience utilities to ObjectBase inspector so the click condition list
	/// can be populated from a folder of ObjectCondition assets in one step.
	/// </summary>
	[CustomEditor(typeof(ObjectBase), true)]
	public class ObjectBaseConditionListEditor : Editor
	{
		private DefaultAsset _conditionFolder;
		private DefaultAsset _spriteFolder;
		private string _statusMessage;
		private MessageType _statusMessageType = MessageType.Info;
		private const string ClickConditionUndoLabel = "Populate Click Condition List";
		private const string AutoPlayUndoLabel = "Populate AutoPlay Settings";
		private const string SymbolSpriteUndoLabel = "Assign Symbol Sprite";
		private const string ObjectDataUndoLabel = "Populate Object Data List";

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Click Condition Utilities", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Select folders for conditions and sprites to auto-fill the list.");

			_conditionFolder = (DefaultAsset)EditorGUILayout.ObjectField("Condition Folder", _conditionFolder, typeof(DefaultAsset), false);
			_spriteFolder = (DefaultAsset)EditorGUILayout.ObjectField("Sprite Folder (optional)", _spriteFolder, typeof(DefaultAsset), false);

			using (new EditorGUI.DisabledScope(_conditionFolder == null && _spriteFolder == null))
			{
				if (GUILayout.Button("Load Conditions From Folder"))
				{
					LoadConditionsIntoTargets();
				}
			}

			if (!string.IsNullOrEmpty(_statusMessage))
			{
				EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);
			}
		}

		private void LoadConditionsIntoTargets()
		{
			var hasConditionFolder = _conditionFolder != null;
			var hasSpriteFolder = _spriteFolder != null;

			if (!hasConditionFolder && !hasSpriteFolder)
			{
				SetStatus("Select at least a condition folder or a sprite folder.", MessageType.Warning);
				return;
			}

			string conditionFolderPath = null;
			if (hasConditionFolder)
			{
				conditionFolderPath = AssetDatabase.GetAssetPath(_conditionFolder);
				if (!AssetDatabase.IsValidFolder(conditionFolderPath))
				{
					SetStatus("Selected condition folder is not a valid folder.", MessageType.Error);
					return;
				}
			}

			string spriteFolderPath = null;
			if (hasSpriteFolder)
			{
				spriteFolderPath = AssetDatabase.GetAssetPath(_spriteFolder);
				if (!AssetDatabase.IsValidFolder(spriteFolderPath))
				{
					SetStatus("Selected sprite folder is not a valid folder.", MessageType.Error);
					return;
				}
			}

			var conditions = hasConditionFolder ? FindConditions(conditionFolderPath) : new List<ObjectConditionBase>();
			var conditionMap = BuildConditionMap(conditions);

			var overConditions = hasConditionFolder ? FindOverConditions(conditions) : new List<ObjectConditionBase>();
			var resetAsset = hasConditionFolder ? FindResetAsset(conditionFolderPath) : null;
			var addAsset = hasConditionFolder ? FindAddAsset(conditionFolderPath) : null;
			var spriteMap = BuildSpriteMap(spriteFolderPath, conditionFolderPath);
			var hasAnyAssignments = conditionMap.Count > 0 || overConditions.Count > 0 || resetAsset != null || addAsset != null || spriteMap.Count > 0;
			if (!hasAnyAssignments)
			{
				var targetDesc = hasConditionFolder && hasSpriteFolder
					? "the selected folders"
					: hasConditionFolder ? "the condition folder" : "the sprite folder";
				SetStatus($"No applicable assets (conditions, auto-play data, or sprites) were found in {targetDesc}.", MessageType.Warning);
				return;
			}

			var visited = new HashSet<ObjectBase>();
			var missingKeys = new HashSet<string>();
			var missingSpriteKeys = new HashSet<string>();
			var missingSpriteTargets = new HashSet<string>();
			int updatedSymbolCount = 0;
			int updatedAutoPlayCount = 0;
			int updatedObjectDataCount = 0;
			int updatedSpriteCount = 0;
			bool missingReset = false;
			bool missingOver = false;
			bool missingAddAsset = hasConditionFolder && addAsset == null;
			bool missingSpriteComponent = false;

			foreach (var targetObj in targets)
			{
				if (targetObj is ObjectBase objectBase)
				{
					var summary = ApplyConditionsToHierarchy(
						objectBase,
						conditionMap,
						overConditions,
						resetAsset,
						addAsset,
						spriteMap,
						visited,
						missingKeys,
						missingSpriteKeys,
						missingSpriteTargets);
					updatedSymbolCount += summary.SymbolAssignments;
					updatedAutoPlayCount += summary.AutoPlayAssignments;
					updatedObjectDataCount += summary.ObjectDataAssignments;
					updatedSpriteCount += summary.SpriteAssignments;
					missingReset |= summary.MissingReset;
					missingOver |= summary.MissingOver;
					missingSpriteComponent |= summary.MissingSpriteComponent;
				}
			}

			if (updatedSymbolCount == 0 && updatedAutoPlayCount == 0 && updatedObjectDataCount == 0 && updatedSpriteCount == 0)
			{
				if (missingKeys.Count > 0)
				{
					SetStatus($"No ObjectBase children matched the available conditions. Missing keys: {FormatMissingKeys(missingKeys)}", MessageType.Warning);
				}
				else
				{
					SetStatus("No ObjectBase components under the current selection matched the expected naming (e.g. SymbolA).", MessageType.Warning);
				}
				return;
			}

			var messageLines = new List<string>
			{
				$"Updated {updatedSymbolCount} symbol ObjectBase(s), {updatedAutoPlayCount} AutoPlay object(s), assigned {updatedObjectDataCount} object data entry/entries, and applied {updatedSpriteCount} sprite(s)."
			};
			var isWarning = false;

			if (conditionMap.Count > 0 && missingKeys.Count > 0)
			{
				messageLines.Add($"Missing symbol keys: {FormatMissingKeys(missingKeys)}");
				isWarning = true;
			}

			if (missingReset)
			{
				messageLines.Add("AutoPlay reset asset (name containing 'Reset') not found in the selected folder.");
				isWarning = true;
			}

			if (missingOver)
			{
				messageLines.Add("AutoPlay condition assets (name containing 'Over') not found in the selected folder.");
				isWarning = true;
			}

			if (missingAddAsset)
			{
				messageLines.Add("Add asset (name containing 'Add') not found in the selected condition folder.");
				isWarning = true;
			}

			if (missingSpriteKeys.Count > 0)
			{
				messageLines.Add($"Missing sprite entries for: {FormatMissingKeys(missingSpriteKeys)}");
				isWarning = true;
			}

			if (missingSpriteComponent && missingSpriteTargets.Count > 0)
			{
				messageLines.Add($"One or more Symbol objects are missing an `Image` component to receive sprites: {string.Join(", ", missingSpriteTargets.OrderBy(t => t))}");
				isWarning = true;
			}

			SetStatus(string.Join("\n", messageLines), isWarning ? MessageType.Warning : MessageType.Info);
		}

		private static string FormatMissingKeys(IEnumerable<string> keys)
		{
			return string.Join(", ", keys.OrderBy(k => k));
		}

		private static List<ObjectConditionBase> FindConditions(string folderPath)
		{
			var guids = AssetDatabase.FindAssets("t:ObjectConditionBase", new[] { folderPath });
			var results = new List<ObjectConditionBase>(guids.Length);

			foreach (var guid in guids)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(guid);
				var condition = AssetDatabase.LoadAssetAtPath<ObjectConditionBase>(assetPath);
				if (condition != null)
				{
					results.Add(condition);
				}
			}

			// Ensure deterministic order by path, which naturally groups SlotA/B/C... assets.
			results.Sort((a, b) =>
			{
				var pathA = AssetDatabase.GetAssetPath(a);
				var pathB = AssetDatabase.GetAssetPath(b);
				return string.CompareOrdinal(pathA, pathB);
			});

			return results;
		}

		private static Dictionary<string, ObjectConditionBase> BuildConditionMap(IEnumerable<ObjectConditionBase> conditions)
		{
			var map = new Dictionary<string, ObjectConditionBase>(StringComparer.OrdinalIgnoreCase);
			foreach (var condition in conditions)
			{
				if (condition == null)
				{
					continue;
				}

				if (TryGetConditionKey(condition.name, out var key))
				{
					map[key] = condition;
				}
			}
			return map;
		}

		private static List<ObjectConditionBase> FindOverConditions(IEnumerable<ObjectConditionBase> conditions)
		{
			return conditions
				.Where(condition => condition != null && condition.name.IndexOf("Over", StringComparison.OrdinalIgnoreCase) >= 0)
				.OrderBy(condition => AssetDatabase.GetAssetPath(condition), StringComparer.Ordinal)
				.ToList();
		}

		private static ObjectAssetBase FindResetAsset(string folderPath)
		{
			var guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<ObjectAssetBase>(path);
				if (asset != null && asset.name.IndexOf("Reset", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return asset;
				}
			}

			return null;
		}

		private static ObjectAssetBase FindAddAsset(string folderPath)
		{
			var guids = AssetDatabase.FindAssets("t:ObjectAssetBase", new[] { folderPath });
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<ObjectAssetBase>(path);
				if (asset != null && asset.name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return asset;
				}
			}

			return null;
		}

		private static Dictionary<string, Sprite> BuildSpriteMap(string explicitSpriteFolderPath, string conditionFolderPath)
		{
			var spriteFolder = !string.IsNullOrEmpty(explicitSpriteFolderPath)
				? NormalizeFolderPath(explicitSpriteFolderPath)
				: FindSpriteFolder(conditionFolderPath);
			if (string.IsNullOrEmpty(spriteFolder))
			{
				return new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
			}

			var guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
			if (guids.Length == 0)
			{
				return new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
			}

			var entries = new List<(Sprite sprite, int order, string name)>(guids.Length);
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
				if (sprite == null)
				{
					continue;
				}

				var order = TryParseLeadingNumber(sprite.name, out var parsed) ? parsed : int.MaxValue;
				entries.Add((sprite, order, sprite.name));
			}

			var sorted = entries
				.OrderBy(entry => entry.order)
				.ThenBy(entry => entry.name, StringComparer.Ordinal)
				.Select(entry => entry.sprite)
				.ToList();

			var map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < sorted.Count; i++)
			{
				var key = ((char)('A' + i)).ToString();
				map[key] = sorted[i];
			}

			return map;
		}

		private static string NormalizeFolderPath(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				return null;
			}

			var normalized = folderPath.Replace('\\', '/');
			return AssetDatabase.IsValidFolder(normalized) ? normalized : null;
		}

		private static string FindSpriteFolder(string conditionFolderPath)
		{
			if (string.IsNullOrEmpty(conditionFolderPath))
			{
				return null;
			}

			var normalized = conditionFolderPath.Replace('\\', '/');
			// If the selected folder itself is the Sprite folder, use it directly.
			if (normalized.EndsWith("/Sprite", StringComparison.OrdinalIgnoreCase) && AssetDatabase.IsValidFolder(normalized))
			{
				return normalized;
			}

			// Prefer a Sprite child folder.
			var childCandidate = $"{normalized}/Sprite";
			if (AssetDatabase.IsValidFolder(childCandidate))
			{
				return childCandidate;
			}

			var parent = Path.GetDirectoryName(normalized);
			if (string.IsNullOrEmpty(parent))
			{
				return null;
			}

			parent = parent.Replace('\\', '/');
			var candidate = $"{parent}/Sprite";
			return AssetDatabase.IsValidFolder(candidate) ? candidate : null;
		}

		private static bool TryParseLeadingNumber(string text, out int value)
		{
			value = 0;
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}

			int index = 0;
			while (index < text.Length && char.IsDigit(text[index]))
			{
				index++;
			}

			if (index == 0)
			{
				return false;
			}

			return int.TryParse(text.Substring(0, index), out value);
		}

		private static AssignmentSummary ApplyConditionsToHierarchy(
			ObjectBase root,
			IReadOnlyDictionary<string, ObjectConditionBase> conditionMap,
			IReadOnlyList<ObjectConditionBase> overConditions,
			ObjectAssetBase resetAsset,
			ObjectAssetBase addAsset,
			IReadOnlyDictionary<string, Sprite> spriteMap,
			ISet<ObjectBase> visited,
			ISet<string> missingKeys,
			ISet<string> missingSpriteKeys,
			ISet<string> missingSpriteTargets)
		{
			var summary = AssignmentSummary.Empty;
			var shouldApplyAutoPlay = resetAsset != null || (overConditions != null && overConditions.Count > 0);
			var objectBases = root.GetComponentsInChildren<ObjectBase>(true);
			foreach (var objectBase in objectBases)
			{
				if (!visited.Add(objectBase))
				{
					continue;
				}

				if (addAsset != null && ReferenceEquals(objectBase, root))
				{
					if (EnsureObjectDataContains(objectBase, addAsset))
					{
						summary = summary.AddObjectData();
					}
				}

				if (shouldApplyAutoPlay && objectBase is AutoPlayObject autoPlay)
				{
					var autoResult = ApplyAutoPlay(autoPlay, resetAsset, overConditions);
					if (autoResult.Applied)
					{
						summary = summary.AddAutoPlay(autoResult.MissingReset, autoResult.MissingOver);
					}
					else
					{
						summary = summary.WithMissingFlags(autoResult.MissingReset, autoResult.MissingOver);
					}
					continue;
				}

				if (!TryGetSymbolKey(objectBase.gameObject.name, out var key))
				{
					continue;
				}

				if (conditionMap.TryGetValue(key, out var condition) && condition != null)
				{
					if (SetClickConditionList(objectBase, condition))
					{
						summary = summary.AddSymbol();
					}
				}
				else if (conditionMap.Count > 0)
				{
					missingKeys.Add(key);
				}

				if (spriteMap.Count > 0)
				{
					if (spriteMap.TryGetValue(key, out var sprite))
					{
						if (SetSymbolSprite(objectBase, sprite))
						{
							summary = summary.AddSprite();
						}
						else
						{
							missingSpriteTargets.Add(objectBase.gameObject.name);
							summary = summary.WithMissingSpriteComponent();
						}
					}
					else
					{
						missingSpriteKeys.Add(key);
					}
				}
			}
			return summary;
		}

		private static bool SetClickConditionList(ObjectBase objectBase, ObjectConditionBase condition)
		{
			var so = new SerializedObject(objectBase);
			so.Update();

			var listProperty = so.FindProperty("_clicktConditionList");
			if (listProperty == null)
			{
				return false;
			}

			Undo.RecordObject(objectBase, ClickConditionUndoLabel);

			listProperty.arraySize = 1;
			listProperty.GetArrayElementAtIndex(0).objectReferenceValue = condition;

			so.ApplyModifiedProperties();
			EditorUtility.SetDirty(objectBase);
			return true;
		}

		private static bool EnsureObjectDataContains(ObjectBase objectBase, ObjectAssetBase asset)
		{
			var so = new SerializedObject(objectBase);
			so.Update();

			var listProperty = so.FindProperty("_objectDataList");
			if (listProperty == null)
			{
				return false;
			}

			for (int i = 0; i < listProperty.arraySize; i++)
			{
				if (listProperty.GetArrayElementAtIndex(i).objectReferenceValue == asset)
				{
					return false;
				}
			}

			Undo.RecordObject(objectBase, ObjectDataUndoLabel);
			var newIndex = listProperty.arraySize;
			listProperty.arraySize = newIndex + 1;
			listProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = asset;

			so.ApplyModifiedProperties();
			EditorUtility.SetDirty(objectBase);
			return true;
		}

		private static bool SetSymbolSprite(ObjectBase objectBase, Sprite sprite)
		{
			var image = objectBase.GetComponent<Image>();
			if (image == null)
			{
				return false;
			}

			Undo.RecordObject(image, SymbolSpriteUndoLabel);
			image.sprite = sprite;
			EditorUtility.SetDirty(image);
			return true;
		}

		private static bool TryGetConditionKey(string name, out string key)
		{
			key = null;
			if (string.IsNullOrWhiteSpace(name))
			{
				return false;
			}

			const string token = "Condition";
			var trimmed = name.Trim();
			var index = trimmed.LastIndexOf(token, StringComparison.OrdinalIgnoreCase);
			if (index < 0 || index + token.Length >= trimmed.Length)
			{
				return false;
			}

			var suffix = trimmed.Substring(index + token.Length).Trim('_', '-', ' ');
			if (string.IsNullOrEmpty(suffix))
			{
				return false;
			}

			key = suffix.ToUpperInvariant();
			return true;
		}

		private static bool TryGetSymbolKey(string name, out string key)
		{
			key = null;
			if (string.IsNullOrWhiteSpace(name))
			{
				return false;
			}

			var trimmed = name.Trim();
			const string symbolPrefix = "Symbol";
			var index = trimmed.IndexOf(symbolPrefix, StringComparison.OrdinalIgnoreCase);
			if (index >= 0)
			{
				var suffix = trimmed.Substring(index + symbolPrefix.Length).Trim();
				if (!string.IsNullOrEmpty(suffix))
				{
					key = suffix.ToUpperInvariant();
					return true;
				}
			}

			return false;
		}

		private void SetStatus(string message, MessageType type)
		{
			_statusMessage = message;
			_statusMessageType = type;
			Repaint();
		}

		private static AutoPlayAssignmentResult ApplyAutoPlay(AutoPlayObject autoPlay, ObjectAssetBase resetAsset, IReadOnlyList<ObjectConditionBase> overConditions)
		{
			var result = new AutoPlayAssignmentResult
			{
				MissingReset = resetAsset == null,
				MissingOver = overConditions == null || overConditions.Count == 0
			};

			if (resetAsset == null && (overConditions == null || overConditions.Count == 0))
			{
				return result;
			}

			var so = new SerializedObject(autoPlay);
			so.Update();

			Undo.RecordObject(autoPlay, AutoPlayUndoLabel);

			bool applied = false;

			if (resetAsset != null)
			{
				var objectDataProperty = so.FindProperty("_objectDataList");
				if (objectDataProperty != null)
				{
					objectDataProperty.arraySize = 1;
					objectDataProperty.GetArrayElementAtIndex(0).objectReferenceValue = resetAsset;
					applied = true;
				}
			}

			if (overConditions != null && overConditions.Count > 0)
			{
				var autoPlayConditionProperty = so.FindProperty("_autoPlayConsitionList");
				if (autoPlayConditionProperty != null)
				{
					autoPlayConditionProperty.arraySize = overConditions.Count;
					for (int i = 0; i < overConditions.Count; i++)
					{
						autoPlayConditionProperty.GetArrayElementAtIndex(i).objectReferenceValue = overConditions[i];
					}
					applied = true;
				}
			}

			if (applied)
			{
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(autoPlay);
				result.Applied = true;
			}

			return result;
		}

		private readonly struct AssignmentSummary
		{
			private AssignmentSummary(int symbolAssignments, int autoPlayAssignments, int objectDataAssignments, int spriteAssignments, bool missingReset, bool missingOver, bool missingSpriteComponent)
			{
				SymbolAssignments = symbolAssignments;
				AutoPlayAssignments = autoPlayAssignments;
				ObjectDataAssignments = objectDataAssignments;
				SpriteAssignments = spriteAssignments;
				MissingReset = missingReset;
				MissingOver = missingOver;
				MissingSpriteComponent = missingSpriteComponent;
			}

			public int SymbolAssignments { get; }
			public int AutoPlayAssignments { get; }
			public int ObjectDataAssignments { get; }
			public int SpriteAssignments { get; }
			public bool MissingReset { get; }
			public bool MissingOver { get; }
			public bool MissingSpriteComponent { get; }

			public static AssignmentSummary Empty => new AssignmentSummary(0, 0, 0, 0, false, false, false);

			public AssignmentSummary AddSymbol()
			{
				return new AssignmentSummary(SymbolAssignments + 1, AutoPlayAssignments, ObjectDataAssignments, SpriteAssignments, MissingReset, MissingOver, MissingSpriteComponent);
			}

			public AssignmentSummary AddAutoPlay(bool missingReset, bool missingOver)
			{
				return new AssignmentSummary(
					SymbolAssignments,
					AutoPlayAssignments + 1,
					ObjectDataAssignments,
					SpriteAssignments,
					MissingReset || missingReset,
					MissingOver || missingOver,
					MissingSpriteComponent);
			}

			public AssignmentSummary AddObjectData()
			{
				return new AssignmentSummary(
					SymbolAssignments,
					AutoPlayAssignments,
					ObjectDataAssignments + 1,
					SpriteAssignments,
					MissingReset,
					MissingOver,
					MissingSpriteComponent);
			}

			public AssignmentSummary AddSprite()
			{
				return new AssignmentSummary(SymbolAssignments, AutoPlayAssignments, ObjectDataAssignments, SpriteAssignments + 1, MissingReset, MissingOver, MissingSpriteComponent);
			}

			public AssignmentSummary WithMissingFlags(bool missingReset, bool missingOver)
			{
				return new AssignmentSummary(
					SymbolAssignments,
					AutoPlayAssignments,
					ObjectDataAssignments,
					SpriteAssignments,
					MissingReset || missingReset,
					MissingOver || missingOver,
					MissingSpriteComponent);
			}

			public AssignmentSummary WithMissingSpriteComponent()
			{
				return new AssignmentSummary(
					SymbolAssignments,
					AutoPlayAssignments,
					ObjectDataAssignments,
					SpriteAssignments,
					MissingReset,
					MissingOver,
					true);
			}
		}

		private struct AutoPlayAssignmentResult
		{
			public bool Applied { get; set; }
			public bool MissingReset { get; set; }
			public bool MissingOver { get; set; }
		}
	}
}
