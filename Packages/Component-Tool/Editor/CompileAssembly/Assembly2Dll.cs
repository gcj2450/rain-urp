using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Player;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public static class Assembly2Dll
    {
        public static void CompileAssembly2Dll(BuildTarget target)
        {
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

            string dllOutputPath = DirectoryUtil.DllOutputPath(target);

            var group = BuildPipeline.GetBuildTargetGroup(target);

            ScriptCompilationSettings scriptCompilationSettings = new ScriptCompilationSettings();
            scriptCompilationSettings.group = group;
            scriptCompilationSettings.target = target;

            ScriptCompilationResult scriptCompilationResult = PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, dllOutputPath);

            Debug.Log($"platform: {target} compile assembly 2 dll finish!!! ");
        }

        public static void CompileAssembly2Dll_ActiveBuildTarget()
        {
            CompileAssembly2Dll(EditorUserBuildSettings.activeBuildTarget);
        }

        public static void CompileAssembly2Dll_Win64()
        {
            CompileAssembly2Dll(BuildTarget.StandaloneWindows64);
        }

        public static void CompileAssembly2Dll_Android()
        {
            CompileAssembly2Dll(BuildTarget.Android);
        }

        public static void CompileAssembly2Dll_IOS()
        {
            CompileAssembly2Dll(BuildTarget.iOS);
        }

        public static void CompileAssembly2Dll_WebGL()
        {
            CompileAssembly2Dll(BuildTarget.WebGL);
        }

        public static void CompileAssembly2Dll_All()
        {
            string dllAssemblyPath = DirectoryUtil.DllOutputAssemblyPath();

            if (Directory.Exists(dllAssemblyPath))
            {
                Directory.Delete(dllAssemblyPath, true);
            }

            CompileAssembly2Dll_Win64();
            CompileAssembly2Dll_Android();
            CompileAssembly2Dll_IOS();
            CompileAssembly2Dll_WebGL();
        }

        //[MenuItem("Components-Tool/±‡“Î≥Ã–ÚºØ/AllPlatform")]
        public static void CompileAssembly()
        {
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

            CompileAssembly2Dll_All();
        }

    }
}

