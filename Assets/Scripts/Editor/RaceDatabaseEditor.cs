using System.Collections.Generic;
using System.Linq;
using ArenaTactics.Data;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RaceDatabase))]
public class RaceDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Race Database"))
        {
            RefreshDatabase((RaceDatabase)target);
        }
    }

    private static void RefreshDatabase(RaceDatabase database)
    {
        if (database == null)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:RaceData");
        List<RaceData> races = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<RaceData>(path))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.raceName)
            .ToList();

        Undo.RecordObject(database, "Refresh Race Database");
        database.races = races;
        EditorUtility.SetDirty(database);

        Debug.Log($"RaceDatabase: populated with {races.Count} races.");
    }
}
