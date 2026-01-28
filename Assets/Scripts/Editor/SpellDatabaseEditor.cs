using System.Collections.Generic;
using System.Linq;
using ArenaTactics.Data;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpellDatabase))]
public class SpellDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Spell Database"))
        {
            RefreshDatabase((SpellDatabase)target);
        }
    }

    private static void RefreshDatabase(SpellDatabase database)
    {
        if (database == null)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:SpellData");
        List<SpellData> spells = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<SpellData>(path))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.tier)
            .ThenBy(asset => asset.spellType)
            .ThenBy(asset => asset.cost)
            .ThenBy(asset => asset.spellName)
            .ToList();

        Undo.RecordObject(database, "Refresh Spell Database");
        database.spells = spells;
        EditorUtility.SetDirty(database);

        Debug.Log($"SpellDatabase: populated with {spells.Count} spells.");
    }
}
