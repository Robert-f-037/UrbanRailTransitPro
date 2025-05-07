using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using PimDeWitte.UnityMainThreadDispatcher;

public class Preprocess : MonoBehaviour
{
#if UNITY_EDITOR
    public List<TextAsset> ODfiles;
    public List<TextAsset> Trainfiles;
#endif
    public List<string> ODfilesPath = new List<string>();
    public List<string> TrainfilesPath = new List<string>();
    public string OutODfilePath;
    public string OutTrainfilePath;
    public bool wannaPreprocess = false;
    Dictionary<string, Dictionary<string, List<float>>> ODMatrixWeekday = new Dictionary<string, Dictionary<string, List<float>>>();
    Dictionary<string, Dictionary<string, List<float>>> ODMatrixWeekend = new Dictionary<string, Dictionary<string, List<float>>>();
    Dictionary<int, List<string[]>> TrainMatrixWeekday = new Dictionary<int, List<string[]>>();
    Dictionary<int, List<string[]>> TrainMatrixWeekend = new Dictionary<int, List<string[]>>();
    private int ODlistCount = 1440;
    public float processValue;

    public static Preprocess preprocess { private set; get; }
    private void Awake()
    {
        if (preprocess)
        {
            Destroy(this);
        }
        else
        {
            preprocess = this;
            DontDestroyOnLoad(this);
        }
    }

    void Start()
    {
    }

    public void PreprocessRun()
    {

#if UNITY_EDITOR
        foreach (TextAsset ODfile in ODfiles)
        {
            ODfilesPath.Add(AssetDatabase.GetAssetPath(ODfile));
        }
        foreach (TextAsset Trainfile in Trainfiles)
        {
            TrainfilesPath.Add(AssetDatabase.GetAssetPath(Trainfile));
        }
#endif

        int dataCount = 0;
        int dataprocessCount = 0;
        List<List<List<string>>> ODcsvMatrixTotal = new List<List<List<string>>>();
        List<List<List<string>>> TraincsvMatrixTotal = new List<List<List<string>>>();

        if (ODfilesPath.Count != 0)
        {
            foreach (string ODfile in ODfilesPath)
            {
                List<List<string>> csvMatrixMonth = CsvReader.ReadCsv(ODfile);
                ODcsvMatrixTotal.Add(csvMatrixMonth);
                dataCount += csvMatrixMonth.Count;
                if (csvMatrixMonth.Count == 0)
                {
                    Debug.Log("ODfiles format is wrong!");
                }
            }
        }
        if (TrainfilesPath.Count != 0)
        {
            foreach (string TrainfilePath in TrainfilesPath)
            {
                List<List<string>> csvMatrix = CsvReader.ReadCsv(TrainfilePath);
                TraincsvMatrixTotal.Add(csvMatrix);
                dataCount += csvMatrix.Count;
                if (csvMatrix.Count == 0)
                {
                    Debug.Log("Trainfile format is wrong!");
                }
            }
        }

        if (ODcsvMatrixTotal.Count != 0)
        {
            foreach (List<List<string>> csvMatrixMonth in ODcsvMatrixTotal)
            {
                foreach (List<string> row in csvMatrixMonth)
                {
                    if (DateTime.TryParseExact(row[0], "yyyy/MM/d", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    {
                        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            if (ODMatrixWeekend.ContainsKey(row[3]))
                            {
                                if (ODMatrixWeekend[row[3]].ContainsKey(row[4]))
                                {
                                    if (ODMatrixWeekend[row[3]][row[4]].Count == ODlistCount * 2)
                                    {
                                        if (row[2] != "")
                                        {
                                            int tempTime = int.Parse(row[1]) * 60 + int.Parse(row[2]);
                                            ODMatrixWeekend[row[3]][row[4]][tempTime] += int.Parse(row[5]);
                                            ODMatrixWeekend[row[3]][row[4]][tempTime + ODlistCount] += 1f;
                                        }
                                        else
                                        {
                                            for (int tempTime = int.Parse(row[1]) * 60; tempTime < int.Parse(row[1]) * 60 + 60; tempTime++)
                                            {
                                                ODMatrixWeekend[row[3]][row[4]][tempTime] += int.Parse(row[5]) / 60;
                                                ODMatrixWeekend[row[3]][row[4]][tempTime + ODlistCount] += 1f;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log("Set ODvalue failed");
                                    }
                                }
                                else
                                {
                                    List<float> tempHoursValue = new List<float>(new float[ODlistCount * 2]);
                                    if (row[2] != "")
                                    {
                                        int tempTime = int.Parse(row[1]) * 60 + int.Parse(row[2]);
                                        tempHoursValue[tempTime] = int.Parse(row[5]);
                                        tempHoursValue[tempTime + ODlistCount] = 1f;
                                    }
                                    else
                                    {
                                        for (int tempTime = int.Parse(row[1]) * 60; tempTime < int.Parse(row[1]) * 60 + 60; tempTime++)
                                        {
                                            tempHoursValue[tempTime] = int.Parse(row[5]) / 60;
                                            tempHoursValue[tempTime + ODlistCount] = 1f;
                                        }
                                    }
                                    ODMatrixWeekend[row[3]].Add(row[4], tempHoursValue);
                                }
                            }
                            else
                            {
                                List<float> tempHoursValue = new List<float>(new float[ODlistCount * 2]);
                                if (row[2] != "")
                                {
                                    int tempTime = int.Parse(row[1]) * 60 + int.Parse(row[2]);
                                    tempHoursValue[tempTime] = int.Parse(row[5]);
                                    tempHoursValue[tempTime + ODlistCount] = 1f;
                                }
                                else
                                {
                                    for (int tempTime = int.Parse(row[1]) * 60; tempTime < int.Parse(row[1]) * 60 + 60; tempTime++)
                                    {
                                        tempHoursValue[tempTime] = int.Parse(row[5]) / 60;
                                        tempHoursValue[tempTime + ODlistCount] = 1f;
                                    }
                                }
                                Dictionary<string, List<float>> tempDValue = new Dictionary<string, List<float>>();
                                tempDValue.Add(row[4], tempHoursValue);
                                ODMatrixWeekend.Add(row[3], tempDValue);
                            }
                        }
                        else
                        {
                            if (ODMatrixWeekday.ContainsKey(row[3]))
                            {
                                if (ODMatrixWeekday[row[3]].ContainsKey(row[4]))
                                {
                                    if (ODMatrixWeekday[row[3]][row[4]].Count == ODlistCount * 2)
                                    {
                                        if (row[2] != "")
                                        {
                                            int tempTime = int.Parse(row[1]) * 60 + int.Parse(row[2]);
                                            ODMatrixWeekday[row[3]][row[4]][tempTime] += int.Parse(row[5]);
                                            ODMatrixWeekday[row[3]][row[4]][tempTime + ODlistCount] += 1f;
                                        }
                                        else
                                        {
                                            for (int tempTime = int.Parse(row[1]) * 60; tempTime < int.Parse(row[1]) * 60 + 60; tempTime++)
                                            {
                                                ODMatrixWeekday[row[3]][row[4]][tempTime] += int.Parse(row[5]) / 60;
                                                ODMatrixWeekday[row[3]][row[4]][tempTime + ODlistCount] += 1f;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log("Set ODvalue failed");
                                    }
                                }
                                else
                                {
                                    List<float> tempHoursValue = new List<float>(new float[ODlistCount * 2]);
                                    if (row[2] != "")
                                    {
                                        int tempTime = int.Parse(row[1]) * 60 + int.Parse(row[2]);
                                        tempHoursValue[tempTime] = int.Parse(row[5]);
                                        tempHoursValue[tempTime + ODlistCount] = 1f;
                                    }
                                    else
                                    {
                                        for (int tempTime = int.Parse(row[1]) * 60; tempTime < int.Parse(row[1]) * 60 + 60; tempTime++)
                                        {
                                            tempHoursValue[tempTime] = int.Parse(row[5]) / 60;
                                            tempHoursValue[tempTime + ODlistCount] = 1f;
                                        }
                                    }
                                    ODMatrixWeekday[row[3]].Add(row[4], tempHoursValue);
                                }
                            }
                            else
                            {
                                List<float> tempHoursValue = new List<float>(new float[ODlistCount * 2]);
                                if (row[2] != "")
                                {
                                    int tempTime = int.Parse(row[1]) * 60 + int.Parse(row[2]);
                                    tempHoursValue[tempTime] = int.Parse(row[5]);
                                    tempHoursValue[tempTime + ODlistCount] = 1f;
                                }
                                else
                                {
                                    for (int tempTime = int.Parse(row[1]) * 60; tempTime < int.Parse(row[1]) * 60 + 60; tempTime++)
                                    {
                                        tempHoursValue[tempTime] = int.Parse(row[5]) / 60;
                                        tempHoursValue[tempTime + ODlistCount] = 1f;
                                    }
                                }
                                Dictionary<string, List<float>> tempDValue = new Dictionary<string, List<float>>();
                                tempDValue.Add(row[4], tempHoursValue);
                                ODMatrixWeekday.Add(row[3], tempDValue);
                            }
                        }
                    }

                    dataprocessCount++;
                    processValue = (float)dataprocessCount / (float)dataCount;
                    UnityMainThreadDispatcher.Instance().Enqueue(() => Camera.main.GetComponent<StartController>().processValueChange("Data preprocessing "));
                }
            }

            foreach (var Ovar in ODMatrixWeekday)
            {
                foreach (var Dvar in ODMatrixWeekday[Ovar.Key])
                {
                    for (int i = 0; i < ODlistCount; i++)
                    {
                        if (ODMatrixWeekday[Ovar.Key][Dvar.Key][i + ODlistCount] == 0)
                        {
                            ODMatrixWeekday[Ovar.Key][Dvar.Key][i] = 0f;
                        }
                        else
                        {
                            ODMatrixWeekday[Ovar.Key][Dvar.Key][i] /= ODMatrixWeekday[Ovar.Key][Dvar.Key][i + ODlistCount];
                        }
                    }
                }
            }
            foreach (var Ovar in ODMatrixWeekend)
            {
                foreach (var Dvar in ODMatrixWeekend[Ovar.Key])
                {
                    for (int i = 0; i < ODlistCount; i++)
                    {
                        if (ODMatrixWeekend[Ovar.Key][Dvar.Key][i + ODlistCount] == 0)
                        {
                            ODMatrixWeekend[Ovar.Key][Dvar.Key][i] = 0f;
                        }
                        else
                        {
                            ODMatrixWeekend[Ovar.Key][Dvar.Key][i] /= ODMatrixWeekend[Ovar.Key][Dvar.Key][i + ODlistCount];
                        }
                    }
                }
            }
            string jsonODWeekday = JsonConvert.SerializeObject(ODMatrixWeekday);
            System.IO.File.WriteAllText(OutODfilePath + "/ODweekday.josn", jsonODWeekday);
            string jsonODWeekend = JsonConvert.SerializeObject(ODMatrixWeekend);
            System.IO.File.WriteAllText(OutODfilePath + "/ODweekend.josn", jsonODWeekend);
            //Debug.Log("Finished");
        }

        if (TraincsvMatrixTotal.Count != 0)
        {
            foreach (List<List<string>> csvMatrix in TraincsvMatrixTotal)
            {
                foreach (List<string> row in csvMatrix)
                {
                    int timeSeconds = int.Parse(row[4]) * 60 * 60 + int.Parse(row[5]) * 60 + int.Parse(row[6]);
                    if (row[3] == "weekday")
                    {
                        if (TrainMatrixWeekday.ContainsKey(timeSeconds))
                        {
                            TrainMatrixWeekday[timeSeconds].Add(new string[3] { row[0], row[1], row[2] });
                        }
                        else
                        {
                            TrainMatrixWeekday.Add(timeSeconds, new List<string[]> { new string[3] { row[0], row[1], row[2] } });
                        }
                    }
                    else if (row[3] == "weekend")
                    {
                        if (TrainMatrixWeekend.ContainsKey(timeSeconds))
                        {
                            TrainMatrixWeekend[timeSeconds].Add(new string[3] { row[0], row[1], row[2] });
                        }
                        else
                        {
                            TrainMatrixWeekend.Add(timeSeconds, new List<string[]> { new string[3] { row[0], row[1], row[2] } });
                        }
                    }

                    dataprocessCount++;
                    processValue = (float)dataprocessCount / (float)dataCount;
                    UnityMainThreadDispatcher.Instance().Enqueue(() => Camera.main.GetComponent<StartController>().processValueChange("Data preprocessing "));
                }
            }
            string jsonTrainWeekday = JsonConvert.SerializeObject(TrainMatrixWeekday);
            System.IO.File.WriteAllText(OutTrainfilePath + "/Trainweekday.josn", jsonTrainWeekday);
            string jsonTrainWeekend = JsonConvert.SerializeObject(TrainMatrixWeekend);
            System.IO.File.WriteAllText(OutTrainfilePath + "/Trainweekend.josn", jsonTrainWeekend);
        }
    }

    void Update()
    {
        if (ODfilesPath.Count > 0)
        {
            OutODfilePath = System.IO.Path.GetDirectoryName(ODfilesPath[0]);
        }
        if (TrainfilesPath.Count > 0)
        {
            OutTrainfilePath = System.IO.Path.GetDirectoryName(TrainfilesPath[0]);
        }

        //if (wannaPreprocess)
        //{
        //    PreprocessRun();
        //    wannaPreprocess = false;
        //}
    }

    private static List<List<string>> ExtractDataForMonths(List<List<string>> matrix, int dateColumnIndex, params int[] months)
    {
        List<List<string>> extractedData = new List<List<string>>();

        foreach (List<string> row in matrix)
        {
            if (row.Count > dateColumnIndex && DateTime.TryParseExact(row[dateColumnIndex], "yyyy-MM-d", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                if (months.Contains(date.Month))
                {
                    extractedData.Add(row);
                }
            }
            else
            {
                Console.WriteLine($"Invalid date format or missing date in row: {string.Join(",", row)}");
            }
        }

        return extractedData;
    }
}

public class CsvReader
{
    public static List<List<string>> ReadCsv(string filePath, char delimiter = ',')
    {
        List<List<string>> matrix = new List<List<string>>();

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    List<string> row = ParseCsvLine(line, delimiter);
                    matrix.Add(row);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading CSV file: " + ex.Message);
        }

        return matrix;
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        List<string> values = new List<string>();
        bool insideQuotes = false;
        string currentValue = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == delimiter && !insideQuotes)
            {
                values.Add(currentValue);
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }

        values.Add(currentValue);

        return values;
    }
}