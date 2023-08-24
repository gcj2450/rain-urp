using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/*使用之前需要做
 * 1. 首先需要在自己电脑上安装npm
 * 2.添加用户： npm adduser  --registry=http://vrfe.baidu-int.com/npm/
 * 根据提示设置用户名、密码、邮箱
 */

namespace Baidu.Meta.ComponentsTool.Editor
{
    public static class PackagePublish
    {
        public static string NpmPath()
        {
            // 不同平台下npm程序安装路径不一样

#if UNITY_EDITOR_OSX
        // Mac系统打包机里的npm程序路径，暂时不考虑mac系统
        return "";
#endif

#if UNITY_EDITOR_WIN
            // Windows系统打包机里的npm程序路径
            string file1 = "C:/Program Files/nodejs/npm.cmd";
            string file2 = "D:/Program Files/nodejs/npm.cmd";
            if (File.Exists(file1))
            {
                return file1;
            }
            else if (File.Exists(file2))
            {
                return file2;
            }
            else
            {
                string filePath = FindFilePath("npm.cmd", "");
                UnityEngine.Debug.Log("npm：  " + filePath);
                return filePath;
            }
#endif

            //return "不支持的Editor平台";
        }

        public static string FindFilePath(string file, string localPath)
        {
            file = Environment.ExpandEnvironmentVariables(file);
            if (!File.Exists(file))
            {
                if (Path.GetDirectoryName(file) == String.Empty)
                {
                    foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                    {
                        string path = test.Trim();
                        if (!string.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, file)))
                            return Path.GetFullPath(path);
                    }
                }

                file = Path.Combine(localPath, file);
                if (File.Exists(file)) return file;

                //return $"[{Path.GetFileName(file)}] not found.";
                return "";
            }

            return Path.GetFullPath(file);
        }

        private static void ProcessCommand(string command, string argument, string workDir)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = argument;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            if (workDir != null)
            {
                process.StartInfo.WorkingDirectory = workDir;
            }
            process.OutputDataReceived += (sender, e) =>
            {
                UnityEngine.Debug.Log(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                UnityEngine.Debug.Log(e.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            process.Close();
        }

        public static void NpmPublish(string packagePath)
        {
            //Thread thread02 = new Thread(() =>
            //{
            //这里就不管npm login了,先提前手工npm login吧，npm login只需要操作一次，这台电脑就在C:\Users\xxx\.npmrc里记录了用户信息
            string npmPath = NpmPath();
            if (string.IsNullOrEmpty(npmPath))
            {
                EditorUtility.DisplayDialog("错误", "当前平台不支持，请切换到windows平台发布", "OK", "Cancel");
                return;
            }
            else
            {
                UnityEngine.Debug.Log(npmPath);
                ProcessCommand(NpmPath(), "publish --registry=http://vrfe.baidu-int.com/npm/", packagePath);
                UnityEngine.Debug.Log($"NpmPublish :  finish");
            }
            //});
            ////启动
            //thread02.Start();
            
        }
    }
}

