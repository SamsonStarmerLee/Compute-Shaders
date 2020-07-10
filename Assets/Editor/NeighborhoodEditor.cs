using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Neighborhood))]
public class NeighborhoodEditor : Editor
{
	private SerializedProperty cellsProperty;
	private SerializedProperty radialMirrorProperty;

	private void OnEnable()
	{
		cellsProperty = serializedObject.FindProperty("cells");
		radialMirrorProperty = serializedObject.FindProperty("radialMirror");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.UpdateIfRequiredOrScript();

		for (int y = 15; y >= -15; y--) // this loop is backwards since editor gui draws from top to bottom
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				for (int x = -15; x <= 15; x++)
				{
					var index = GetIndex(x, y);
					using (var check = new EditorGUI.ChangeCheckScope())
					{
						var valueProperty = cellsProperty.GetArrayElementAtIndex(index);
						var newValue = EditorGUILayout.Toggle(valueProperty.boolValue);
						if (check.changed)
						{
							valueProperty.boolValue = newValue;

							if (radialMirrorProperty.boolValue && !(x == 0 && y == 0))
							{
								var mirrorXProperty = cellsProperty.GetArrayElementAtIndex(GetIndex(-x, y));
								var mirrorYProperty = cellsProperty.GetArrayElementAtIndex(GetIndex(x, -y));
								var mirrorXYProperty = cellsProperty.GetArrayElementAtIndex(GetIndex(-x, -y));

								mirrorXProperty.boolValue = newValue;
								mirrorYProperty.boolValue = newValue;
								mirrorXYProperty.boolValue = newValue;
							}
						}
					}
				}
			}
		}

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Edit Options", EditorStyles.boldLabel);

		using (var check = new EditorGUI.ChangeCheckScope())
		{
			var newRadialMirror = EditorGUILayout.Toggle("Radial Mirror", radialMirrorProperty.boolValue);
			if (check.changed)
			{
				radialMirrorProperty.boolValue = newRadialMirror;
			}
		}

		if (GUI.changed)
			serializedObject.ApplyModifiedProperties();
	}

	private int GetIndex(int x, int y) => (x + 15) + 31 * (y + 15);
}