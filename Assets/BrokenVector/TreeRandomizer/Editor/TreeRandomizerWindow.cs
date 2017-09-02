using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

namespace BrokenVector.TreeRandomizer
{
    public class TreeRandomizerWindow : EditorWindow
    {

        private Tree treeTemplate = null;
        private int treeCount = 5;

        [MenuItem(Constants.WINDOW_PATH), MenuItem(Constants.WINDOW_PATH_ALT)]
        private static void ShowWindow()
        {
            var window = GetWindow(typeof(TreeRandomizerWindow));
		
			#if UNITY_5_4_OR_NEWER
			window.titleContent = new GUIContent(Constants.ASSET_NAME);
			#else
			window.title = Constants.ASSET_NAME;
			#endif
            window.Show();
        }

        void OnGUI()
        {
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("You can only generate trees while not in playmode!", MessageType.Info);
                return;
            }

            treeTemplate = EditorGUILayout.ObjectField("Tree Template", treeTemplate, typeof(Tree), true) as Tree;
            treeCount = EditorGUILayout.IntSlider("Tree Count", treeCount, 1, Constants.SLIDER_TREE_COUNT_MAX);

            if (GUILayout.Button("Generate Trees"))
            {
                Generate(treeTemplate, treeCount);
            }
        }

        private void Generate(Tree treeTemplate, int treeCount)
        {
            Debug.Log("Starting generation of " + treeCount + " trees.");

            if (!AssetDatabase.Contains(treeTemplate))
            {
                Debug.LogError("Couldn't find the tree in the AssetDatabase.", treeTemplate);
                return;
            }
            string path = AssetDatabase.GetAssetPath(treeTemplate);
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            string copyFolder = dir + "/" + Constants.OUTPUT_FOLDER;

            if (!AssetDatabase.IsValidFolder(copyFolder))
                AssetDatabase.CreateFolder(dir, Constants.OUTPUT_FOLDER);

            var treeobj = new SerializedObject(treeTemplate.data);

            Material barkmat = (Material)Resources.Load("barkMaterialGenerator", typeof(Material));
            // Edited since it was failing
            if (barkmat == null)
            {
                Debug.LogError("bark material not found!");
                return;
            }

            Material leafmat = (Material)Resources.Load("leafMaterialGenerator", typeof(Material));
            if (leafmat == null)
            {
                Debug.LogError("leaf material not found");
                return;
            }

            List<Tree> generatedTrees = new List<Tree>();

            for (int i = 0; i < treeCount; i++)
            {
                string copyFile = name + " " + i + ext;
                string copyPath = copyFolder + "/" + copyFile;

                bool result = AssetDatabase.CopyAsset(path, copyPath);
                AssetDatabase.Refresh();
                if (!result)
                {
                    Debug.LogError("Couldn't copy the tree from " + path + " to " + copyPath);
                    return;
                }

                AssetDatabase.ImportAsset(copyPath);
                Tree tree = AssetDatabase.LoadAssetAtPath(copyPath, (typeof(Tree))) as Tree;

                if (tree == null)
                {
                    Debug.LogError("Couldn't load tree.");
                    return;
                }

                foreach (Material mat in tree.GetComponent<MeshRenderer>().sharedMaterials)
                {
                    DestroyImmediate(mat, true);
                }
                AssetDatabase.SaveAssets();

                //Is material order fixed?
                tree.GetComponent<MeshRenderer>().sharedMaterials = new[] { barkmat, leafmat };

                var obj = new SerializedObject(tree.data);
                int randomSeed = Random.Range(0, 9999999);
                obj.FindProperty("root.seed").intValue = randomSeed;
                obj.FindProperty("optimizedSolidMaterial").objectReferenceValue = barkmat;
                obj.FindProperty("optimizedCutoutMaterial").objectReferenceValue = leafmat;
                obj.ApplyModifiedProperties();
                MethodInfo meth = tree.data.GetType().GetMethod("UpdateMesh", new[] { typeof(Matrix4x4), typeof(Material[]).MakeByRefType() });
                object[] arguments = new object[] { tree.transform.worldToLocalMatrix, null };
                meth.Invoke(tree.data, arguments);

                AssetDatabase.DeleteAsset(copyFolder + "/" + name + " " + i + "_Textures");

                generatedTrees.Add(tree);
            }

            //To fix template becoming white
            MethodInfo templatemeth = treeTemplate.data.GetType().GetMethod("UpdateMesh", new[] { typeof(Matrix4x4), typeof(Material[]).MakeByRefType() });
            object[] templatearguments = new object[] { treeTemplate.transform.worldToLocalMatrix, null };
            templatemeth.Invoke(treeTemplate.data, templatearguments);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(treeTemplate));

            foreach (Tree tree in generatedTrees)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tree));
            }

            Debug.Log("Generated " + treeCount + " Trees!");
        }
    }
}