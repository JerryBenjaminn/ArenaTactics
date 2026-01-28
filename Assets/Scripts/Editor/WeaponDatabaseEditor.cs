using System.Collections.Generic;
using System.Linq;
using ArenaTactics.Data;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDatabase))]
public class WeaponDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Weapon Database"))
        {
            RefreshDatabase((WeaponDatabase)target);
        }
    }

    private static void RefreshDatabase(WeaponDatabase database)
    {
        if (database == null)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        List<WeaponData> weapons = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<WeaponData>(path))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.weaponTier)
            .ThenBy(asset => asset.weaponType)
            .ThenBy(asset => asset.cost)
            .ThenBy(asset => asset.weaponName)
            .ToList();

        Undo.RecordObject(database, "Refresh Weapon Database");
        database.weapons = weapons;
        EditorUtility.SetDirty(database);

        Debug.Log($"WeaponDatabase: populated with {weapons.Count} weapons.");
    }
}
