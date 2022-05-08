using System.Collections.Generic;
using System.IO;
using PipeBuilder.Lods;

namespace PipeBuilder.Utility
{
    public class MeshToAssetSaver
    {
        public enum Error
        {
            None,
            NonEditor,
            CanceledByUser
        }
        
        public struct SaveResult
        {
            public bool success;
            public Error error;

            public SaveResult(bool success, Error error)
            {
                this.success = success;
                this.error = error;
            }
        }
        
        public static SaveResult SaveLodMeshesToAssets(string path, List<LodElement> lodElements)
        {
#if UNITY_EDITOR
            var overrideAll = false;
            var count = lodElements.Count;
            // Check if assets already exists and ask for delete
            for (var i = 0; i < count; i++)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Pipe Mesh Builder", "Checking existing assets", (float) i / count);
                var element = lodElements[i];
                var name = element.GameObject.name;
                var fullPath = $"{path}/{name}.asset";
                var relativePath = fullPath.Substring(fullPath.IndexOf("Assets/"));
                if (File.Exists(fullPath))
                {
                    if (overrideAll)
                        UnityEditor.AssetDatabase.DeleteAsset(relativePath);
                    else
                    {
                        var answer = UnityEditor.EditorUtility.DisplayDialogComplex("Pipe Mesh Builder",
                            $"File \"{fullPath}\" already exists!\nOverride?", "Yes", "Yes, for all", "Cancel");
                        switch (answer)
                        {
                            case 0:
                                UnityEditor.AssetDatabase.DeleteAsset(relativePath);
                                break;
                            case 1:
                                UnityEditor.AssetDatabase.DeleteAsset(relativePath);
                                overrideAll = true;
                                break;
                            case 2:
                                UnityEditor.EditorUtility.ClearProgressBar();
                                return new SaveResult(false, Error.CanceledByUser);
                        }
                    }
                }
            }
            UnityEditor.EditorUtility.ClearProgressBar();
            // Create new assets
            for (var i = 0; i < count; i++)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Pipe Mesh Builder", "Saving assets", (float) i / count);
                var element = lodElements[i];
                var name = element.GameObject.name;
                var fullPath = $"{path}/{name}.asset";
                var relativePath = fullPath.Substring(fullPath.IndexOf("Assets/"));
                UnityEditor.AssetDatabase.CreateAsset(element.MeshFilter.sharedMesh, relativePath);
            }
            UnityEditor.EditorUtility.ClearProgressBar();
            return new SaveResult(true, Error.None);
#endif
            return new SaveResult(false, Error.NonEditor);

        }
    }
}