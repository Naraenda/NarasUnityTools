using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

[InitializeOnLoad]
public class DynamicBoneHierarchyHelper {
    static DynamicBoneHierarchyHelper() {
        _bone_icon  = (Texture)EditorGUIUtility.Load("Assets/Nara/Editor/Resources/bone.png");
        _bone_gicon = (Texture)EditorGUIUtility.Load("Assets/Nara/Editor/Resources/bone_gray.png");
        _bone_bicon = (Texture)EditorGUIUtility.Load("Assets/Nara/Editor/Resources/bone_blue.png");

        EditorApplication.update += UpdateHelper;
        EditorApplication.hierarchyWindowItemOnGUI += DrawHelper;
    }

    static Texture _bone_icon;
    static Texture _bone_gicon;
    static Texture _bone_bicon;

    static List<int> has_db;
    static List<int> has_child_db;
    static List<int> has_collider;

    static void UpdateHelper() {        
        var db = Object.FindObjectsOfType<VRCPhysBone>(); 
        has_db = db.Select(x => x.gameObject.GetInstanceID()).ToList();

        var col = Object.FindObjectsOfType<VRCPhysBoneCollider>();
        has_collider = col.Select(x => x.gameObject.GetInstanceID()).ToList();

        has_child_db = new List<int>();
        foreach (var component in db) {

            var c = component.gameObject.transform;
            while (c != null) {
                int instance = c.gameObject.GetInstanceID();
                
                if (has_child_db.Contains(instance))
                    break;

                has_child_db.Add(instance);

                c = c.parent;
            }
        }
    }

    static void DrawHelper(int instance, Rect selection) {
        if (has_db == null || has_child_db == null || has_collider == null)
            UpdateHelper();

        var db_rect = new Rect(selection); 
        db_rect.x += db_rect.width - 16;
        db_rect.height *= 1.2f;
        db_rect.width = db_rect.height;

        var col_rect = new Rect(db_rect);
        col_rect.x -= 5;

        if (has_collider.Contains(instance)) {
            GUI.Label(col_rect, _bone_bicon); 
        }
        
        if (has_db.Contains(instance)) {
            GUI.Label(db_rect, _bone_icon); 
        } else if (has_child_db.Contains(instance)) {
            GUI.Label(db_rect, _bone_gicon); 
        }
    }
}
