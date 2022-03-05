using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

[InitializeOnLoad]
public class PhysBoneHierarchyViewer {
    static PhysBoneHierarchyViewer() {
        _bone_icon  = (Texture)EditorGUIUtility.Load("Packages/com.naraenda.nut/Editor/Resources/bone.png");
        _bone_gicon = (Texture)EditorGUIUtility.Load("Packages/com.naraenda.nut/Editor/Resources/bone_gray.png");
        _bone_bicon = (Texture)EditorGUIUtility.Load("Packages/com.naraenda.nut/Editor/Resources/bone_blue.png");

        EditorApplication.update += OnUpdate;
        EditorApplication.hierarchyWindowItemOnGUI += OnDraw;
    }

    static Texture _bone_icon;
    static Texture _bone_gicon;
    static Texture _bone_bicon;

    static List<int> has_bone;
    static List<int> has_child_bone;
    static List<int> has_collider;

    static void OnUpdate() {        
        var db = Object.FindObjectsOfType<VRCPhysBone>(); 
        has_bone = db.Select(x => x.gameObject.GetInstanceID()).ToList();

        var col = Object.FindObjectsOfType<VRCPhysBoneCollider>();
        has_collider = col.Select(x => x.gameObject.GetInstanceID()).ToList();

        has_child_bone = new List<int>();
        foreach (var component in db) {

            var c = component.gameObject.transform;
            while (c != null) {
                int instance = c.gameObject.GetInstanceID();
                
                if (has_child_bone.Contains(instance))
                    break;

                has_child_bone.Add(instance);

                c = c.parent;
            }
        }
    }

    static void OnDraw(int instance, Rect selection) {
        if (has_bone == null || has_child_bone == null || has_collider == null)
            OnUpdate();

        var rect = new Rect(selection); 
        rect.x += rect.width - 16;
        rect.height *= 1.2f;
        rect.width = rect.height;

        var col_rect = new Rect(rect);
        col_rect.x -= 5;

        if (has_collider.Contains(instance)) {
            GUI.Label(col_rect, _bone_bicon); 
        }
        
        if (has_bone.Contains(instance)) {
            GUI.Label(rect, _bone_icon); 
        } else if (has_child_bone.Contains(instance)) {
            GUI.Label(rect, _bone_gicon); 
        }
    }
}
