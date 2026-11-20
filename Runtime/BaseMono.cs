// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using UnityEngine;

	public abstract class BaseMono : MonoBehaviour
	{
	
	}
}


#if UNITY_EDITOR

namespace Smidgenomics.Unity.FirstPerson.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEngine;
	using UnityEditor;

	[CustomEditor(typeof(BaseMono), true)]
	internal class _BaseMono : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();

			foreach (var (k, g) in _fieldGroups)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUI.indentLevel++;
				g.properties[0].isExpanded = EditorGUILayout.Foldout(g.properties[0].isExpanded, k, true);
				if (g.properties[0].isExpanded)
				{
					DrawProps(g.properties);
				}
				EditorGUI.indentLevel--;
				EditorGUILayout.EndVertical();
			}
			
			DrawProps(_defaultGroup.properties);

			serializedObject.ApplyModifiedProperties();
		}

		private sealed class FieldGroup
		{
			public FieldGroup(string n) => label = n;
			public string label;
			public readonly List<SerializedProperty> properties = new();
		}

		private readonly FieldGroup _defaultGroup = new(string.Empty);
		private readonly Dictionary<string, FieldGroup> _fieldGroups = new();

		private void DrawProps(List<SerializedProperty> props)
		{
			foreach (var p in props)
			{
				EditorGUILayout.PropertyField(p);
			}
		}

		protected virtual void OnEnable()
		{
			_fieldGroups.Clear();
			_defaultGroup.properties.Clear();
			foreach (var f in target.GetType().FindInspectorFields<object>())
			{
				var g = f.GetCustomAttribute<FieldGroupAttribute>();
				var prop = serializedObject.FindProperty(f.Name);
				if (g == null)
				{
					_defaultGroup.properties.Add(prop);
				}
				else
				{
					if (g.groupName == null)
					{
						continue;
					}
					
					if (!_fieldGroups.TryGetValue(g.groupName, out var fg))
					{
						fg = new FieldGroup(g.groupName);
						_fieldGroups[g.groupName] = fg;
					}
					fg.properties.Add(prop);
				}

			}
		}
	}
}


#endif