#define LOGGER


using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;

public class Logger : MonoBehaviour
{
    static readonly object _logLocker = new object();
    static readonly object _CSVLocker = new object();

    private static List<string> listLog = new List<string>();
    private static Dictionary<string, List<string>> csvData = new Dictionary<string, List<string>>();

    //public string outputFile = "Assets/Resources/log.txt";

    public enum LogType{
        NORMAL,
        TITLE
    }

    public static void Log(string s, LogType t = LogType.NORMAL)
    {
#if LOGGER
        lock (_logLocker)
        {
            switch(t)
            {
                case LogType.NORMAL:
                    listLog.Add(s);
                    break;
                case LogType.TITLE:
                    listLog.Add("=====" + s + "=====");
                    break;
                default:
                    break;
            }
        }
#endif
    }

    public static void CSV(string key, string data)
    {
#if LOGGER
        lock (_CSVLocker)
        {
            List<string> temp;
            if(!csvData.ContainsKey(key))
            {
                temp = new List<string>();
                csvData.Add(key, temp);
            }
            else
            {
                temp = csvData[key];
            }

            temp.Add(data);
        }
#endif
    }

    public static string getPath(string file = "")
    {
        return Application.dataPath + "/WFC/Log/" + file;
    }

    //Error encore présente : InvalidOperationException: Collection was modified; enumeration operation may not execute.
    //Cela arrive quand je stoppe le play de unity, le log ne marche pas en thread donc

    private void outputLog()
    {
        //Create log string
        string textToWrite = "";
        foreach (string log in listLog)
        {
            textToWrite += log + "\r\n";
        }

        StreamWriter logWriter = File.CreateText(getPath("Log.txt"));//new StreamWriter(getLogPath());
        logWriter.Write(textToWrite);
        logWriter.Flush();
        logWriter.Close();
    }

    private void outputCSV(bool writeVertical = false)
    {
        StreamWriter CSVWriter = File.CreateText(getPath("data.csv"));

        string delimiter = ";";

        if (writeVertical)
        {
            //Create first row, based on the dictionary keys
            List<string> csvKeys = new List<string>(csvData.Keys);
            string s = string.Join(delimiter, csvKeys);
            CSVWriter.WriteLine(s);

            int nbKeys = csvKeys.Count;

            //Get max length of the columns
            int maxColumnsLength = 0;
            foreach (KeyValuePair<string, List<string>> entry in csvData)
            {
                if (entry.Value.Count > maxColumnsLength)
                {
                    maxColumnsLength = entry.Value.Count;
                }
            }

            //Write all data rows
            for (int i = 0; i < maxColumnsLength; i++)
            {
                string res = "";

                int counter = 0;

                foreach (KeyValuePair<string, List<string>> entry in csvData)
                {
                    counter++;

                    if (i < entry.Value.Count)
                    {
                        res += entry.Value[i];
                    }

                    if (counter < nbKeys)
                    {
                        res += delimiter;
                    }
                }

                CSVWriter.WriteLine(res);
            }
        }
        else //horizontal lines
        {
            foreach (KeyValuePair<string, List<string>> entry in csvData)
            {
                string res = entry.Key + delimiter;

                res += string.Join(delimiter, entry.Value);

                //for (int i = 0; i < entry.Value.Count; i++)
                //{
                //    res += entry.Value[i] + delimiter;
                //}

                
                CSVWriter.WriteLine(res);
            }
        }


        CSVWriter.Flush();
        CSVWriter.Close();

    }

#if LOGGER
    //(Need to attach this script to gameobject)
    void OnDestroy()//Now that we are quiting the application we can write our data to a file
    {
        //Create folder log
        DirectoryInfo di = Directory.CreateDirectory(getPath());

        //TextFileWriter.WriteString(textToWrite, outputFile);
        outputLog();
        outputCSV();
    }
#endif
}
