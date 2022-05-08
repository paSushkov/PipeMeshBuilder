using PipeBuilder.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Menus
{
    public abstract class PipeEditMenu
    {
        protected string name;
        protected PipeBuilder pipeBuilder;
        protected PipeBuilderEditorSettings settings;

        public PipeEditMenu(string name, PipeBuilder pipeBuilder, PipeBuilderEditorSettings settings)
        {
            this.name = name;
            this.pipeBuilder = pipeBuilder;
            this.settings = settings;
        }

        public string Name => name;

        public abstract void OnEnable();
        public abstract void OnDisable();

        public abstract void DrawInspector();
        public abstract void DrawScene();
        
        protected static void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            var r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}