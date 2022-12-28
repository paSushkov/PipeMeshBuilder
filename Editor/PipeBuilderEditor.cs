using System.Collections.Generic;
using System.Linq;
using PipeBuilder.Editor.Menus;
using PipeBuilder.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor
{
    [CustomEditor(typeof(PipeBuilder))]
    public class PipeBuilderEditor : UnityEditor.Editor
    {
        
        private PipeBuilderEditorSettings settings = new PipeBuilderEditorSettings();
        private PipeBuilder pipeBuilder;

        private Dictionary<int, PipeEditMenu> menuItems = new Dictionary<int, PipeEditMenu>();
        private PipeEditMenu activeMenuItem;
        private PreviewEditMenu previewEditMenu;
        private int activeMenuIndex = 0;
        private string[] menuNames;

        private void OnEnable()
        {
            if (target is PipeBuilder casted)
                pipeBuilder = casted;
                
            if (!pipeBuilder)
                return;
            CheckInit();
            
            settings.Load();
            menuItems.Clear();
            menuItems.Add(0, new LineEditMenu("Edit nodes", pipeBuilder, settings));
            menuItems.Add(1, new MeshEditMenu("Edit mesh", pipeBuilder, settings));
            menuItems.Add(2, new UvEditMenu("UV", pipeBuilder, settings));
            menuItems.Add(3, new LodEditMenu("LODs", pipeBuilder, settings));
            activeMenuItem = menuItems[activeMenuIndex];
            menuNames = menuItems.Values.Select(item => item.Name).ToArray();
            previewEditMenu = new PreviewEditMenu("Preview menu", pipeBuilder, settings);
        }

        private void OnDisable()
        {
            if (Tools.hidden)
                Tools.hidden = false;
            activeMenuItem?.OnDisable();
            previewEditMenu?.OnDisable();
        }

        private void CheckInit()
        {
            if (pipeBuilder.Initialized)
            {
                pipeBuilder.RecalculateLODsInfo();
                return;
            }

            pipeBuilder.Initialize();
            EditorUtility.SetDirty(pipeBuilder);
        }
        
        public override void OnInspectorGUI()
        {
            if (!pipeBuilder)
                return;
            previewEditMenu?.DrawInspector();
            DrawMenuToggle();
            activeMenuItem?.DrawInspector();
        }
        
        private void OnSceneGUI()
        {
            if (!pipeBuilder)
                return;
            CheckTransformUpdates();
            activeMenuItem.DrawScene();
        }

        private void DrawMenuToggle()
        {
            EditorGUI.BeginChangeCheck();
            activeMenuIndex = GUILayout.SelectionGrid(activeMenuIndex,menuNames,menuNames.Length,"Button", GUILayout.Height(30f));
            if (EditorGUI.EndChangeCheck())
            {
                activeMenuItem.OnDisable();
                activeMenuItem = menuItems[activeMenuIndex];
                activeMenuItem.OnEnable();
            }
        }

        private void CheckTransformUpdates()
        {
            if (!pipeBuilder.transform.hasChanged)
                return;
            pipeBuilder.UpdateGlobalPositions();
            pipeBuilder.ControlLine.RebuildChordeNodes();
            EditorUtility.SetDirty(pipeBuilder);
            pipeBuilder.transform.hasChanged = false;
        }
    }
}