using System.Collections.Generic;
using System.Linq;
using ArenaTactics.Data;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArmorDatabase))]
public class ArmorDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Armor Database"))
        {
            RefreshDatabase((ArmorDatabase)target);
        }
    }

    private static void RefreshDatabase(ArmorDatabase database)
    {
        if (database == null)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ArmorData");
        List<ArmorData> armors = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<ArmorData>(path))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.tier)
            .ThenBy(asset => asset.armorType)
            .ThenBy(asset => asset.cost)
            .ThenBy(asset => asset.armorName)
            .ToList();

        Undo.RecordObject(database, "Refresh Armor Database");
        database.armors = armors;
        EditorUtility.SetDirty(database);

        Debug.Log($"ArmorDatabase: populated with {armors.Count} armors.");
    }
}
