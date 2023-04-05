 using UnityEngine;
 using System;
 using System.IO;
 public static class AppHelper
 {
    public static string webplayerQuitURL = "http://google.com";
    public static DateTime exporting_end_time = DateTime.MinValue;
    public static TimeSpan exporting_remaining_time;
    public static DateTime training_end_time=DateTime.MinValue;
     public static TimeSpan training_remaining_time;
     public static float training_time;
    public static float threshold_volume=75f;
    public static string early_stopping;
    public static string file_path;

     public static void Quit()
     {
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.Exit(0);
        }
         else if (Application.platform == RuntimePlatform.WebGLPlayer)
         {
            Application.OpenURL(webplayerQuitURL);
         }
         else
         {
            Application.Quit();
         }
     }

     public static bool StartTimer(string flag)
     {
        // for stopping environment exporting fbx
        if (flag == "exporting")
        {
            // shut down environment 3 minutes from now
            if (exporting_end_time==DateTime.MinValue)
            {
                exporting_end_time = DateTime.Now.AddSeconds(10);
            }
            exporting_remaining_time = exporting_end_time - DateTime.Now;
            if  (exporting_remaining_time.TotalSeconds<=0)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
        // for training with a time limit
        else
        {   // Set the end time to 3 minutes from now
            if (training_end_time==DateTime.MinValue)
            {
                training_end_time = DateTime.Now.AddMinutes(training_time);
            }
            training_remaining_time = training_end_time - DateTime.Now;
            if  (training_remaining_time.TotalSeconds<=0)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

     }

     

     public static void LogStatus(string type)
     {
        string filename = Path.GetFileNameWithoutExtension(file_path);
        string line = "";
        string path = "";

        if (type=="fbx")
        {
            line = $"FBX {filename} exported on {DateTime.Now.ToString("HH:mm:ss tt")}";
            path = Path.Combine(Application.dataPath, "log/fbx_export_log.txt");
        }
        if (type == "instructions")
        {
            line = $"INSTRUCTION {filename} exported on {DateTime.Now.ToString("HH:mm:ss tt")}";
            path = Path.Combine(Application.dataPath, "log/instruction_export_log.txt");
        }
        if (!File.Exists(path))
        {
            // Write the string to a file.
            StreamWriter file = new StreamWriter(path);
            file.WriteLine(line);
            file.Close();
        }
        else 
        {
            using (StreamWriter w = File.AppendText(path))
            {
                w.WriteLine(line);

            }
        }
     }
 }