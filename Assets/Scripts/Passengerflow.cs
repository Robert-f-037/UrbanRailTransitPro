using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

public class Passengerflow : MonoBehaviour
{
    public Dictionary<string, Dictionary<string, List<float>>> ODMatrixWeekday;
    public Dictionary<string, Dictionary<string, List<float>>> ODMatrixWeekend;
    public GameObject stations;
    public GameObject lines;
    public GameObject trains;
    public GameObject trainPrefabParent;
    public int passengerNumMax = 1600;
    public bool detailPassengerInfor;

    void Start()
    {
        if (File.Exists(Preprocess.preprocess.OutODfilePath + "/ODweekday.josn") 
            && File.Exists(Preprocess.preprocess.OutODfilePath + "/ODweekend.josn"))
        {
            ODMatrixWeekday = 
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<float>>>>
                (File.ReadAllText(Preprocess.preprocess.OutODfilePath + "/ODweekday.josn"));
            ODMatrixWeekend =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<float>>>>
                (File.ReadAllText(Preprocess.preprocess.OutODfilePath + "/ODweekend.josn"));
        }
        else
        {
            UnityEngine.Debug.Log("ODFilesNotFound");
        }
        if (File.Exists(Preprocess.preprocess.OutTrainfilePath + "/Trainweekday.josn") 
            && File.Exists(Preprocess.preprocess.OutTrainfilePath + "/Trainweekend.josn"))
        {
            GetComponent<GameManager>().TrainMatrixWeekday =
                JsonConvert.DeserializeObject<Dictionary<int, List<string[]>>>
                (File.ReadAllText(Preprocess.preprocess.OutTrainfilePath + "/Trainweekday.josn"));
            GetComponent<GameManager>().TrainMatrixWeekend =
                JsonConvert.DeserializeObject<Dictionary<int, List<string[]>>>
                (File.ReadAllText(Preprocess.preprocess.OutTrainfilePath + "/Trainweekend.josn"));
        }
        else
        {
            UnityEngine.Debug.Log("TrainFilesNotFound");
        }
    }

    void Update()
    {

    }

    public float CreatPassenger(TimeSimulation time)
    {
        if (detailPassengerInfor && GameManager.gameManager.myMode == mode.solution)
        {
            UnityEngine.Debug.Log($"{time.year}/{time.month}/{time.day} " + 
                (time.hour >= 10 ? $"{time.hour}:" : $"0{time.hour}:") +
                (time.minute >= 10 ? $"{time.minute}:" : $"0{time.minute}:") +
                (time.second >= 10 ? $"{time.second}" : $"0{time.second}"));
        }
        float travelTimeinSecond = 0;
        foreach (Station stationO in stations.GetComponentsInChildren<Station>())
        {
            foreach (Station stationD in stations.GetComponentsInChildren<Station>())
            {
                if (stationO.name != stationD.name)
                {
                    if (stationO.isOperating && stationD.isOperating)
                    {
                        int passengerNuminSecond = 0;
                        if (time.week == DayOfWeek.Saturday || time.week == DayOfWeek.Sunday)
                        {
                            if (ODMatrixWeekday.ContainsKey(stationO.name) && ODMatrixWeekday[stationO.name].ContainsKey(stationD.name))
                            {
                                float lamda = ODMatrixWeekend[stationO.name][stationD.name][time.hour * 60 + time.minute] / 60f;
                                if (lamda > 0)
                                {
                                    passengerNuminSecond = Poisson(lamda);
                                }
                            }
                        }
                        else
                        {
                            if (ODMatrixWeekday.ContainsKey(stationO.name) && ODMatrixWeekday[stationO.name].ContainsKey(stationD.name))
                            {
                                float lamda = ODMatrixWeekday[stationO.name][stationD.name][time.hour * 60 + time.minute] / 60f;
                                if (lamda > 0)
                                {
                                    passengerNuminSecond = Poisson(lamda);
                                }
                            }
                        }
                        if (GameManager.gameManager.myMode == mode.simulation)
                        {
                            for (int i = 0; i < passengerNuminSecond; i++)
                            {
                                Passenger passenger = new Passenger(stationO, stationD, time);
                                travelTimeinSecond += (float)(passenger.path.timeD - time);
                                stationO.passengers[passenger.path.lines[0]].Add(passenger);
                                foreach (Train train in passenger.path.trains)
                                {
                                    train.passengers.Add(passenger);
                                }
                                if (detailPassengerInfor)
                                {
                                    string linesInfor = "";
                                    for (int j = 0; j < passenger.path.lines.Count; j++)
                                    {
                                        if (j == passenger.path.lines.Count - 1)
                                        {
                                            linesInfor += passenger.path.lines[j].name + ", ";
                                        }
                                        else
                                        {
                                            linesInfor += passenger.path.lines[j].name + "->";
                                        }
                                    }
                                    string stationsInfor = "";
                                    for (int j = 0; j < passenger.path.stations.Count; j++)
                                    {
                                        if (j == passenger.path.stations.Count - 1)
                                        {
                                            stationsInfor += passenger.path.stations[j].name;
                                        }
                                        else
                                        {
                                            stationsInfor += passenger.path.stations[j].name + "->";
                                        }
                                    }
                                    UnityEngine.Debug.Log($"From {stationO.name} to {stationD.name} a person, cost {passenger.path.timeD - time}s, "
                                        + linesInfor + stationsInfor);
                                }
                            }
                        }
                        else if (GameManager.gameManager.myMode == mode.solution)
                        {
                            if (passengerNuminSecond != 0)
                            {
                                Passenger passenger = new Passenger(stationO, stationD, time);
                                travelTimeinSecond += passengerNuminSecond * (passenger.path.timeD - time);
                                foreach (Train train in passenger.path.trains)
                                {
                                    train.passengers.Add(passenger);
                                }
                                if (detailPassengerInfor)
                                {
                                    string linesInfor = "";
                                    for (int j = 0; j < passenger.path.lines.Count; j++)
                                    {
                                        if (j == passenger.path.lines.Count - 1)
                                        {
                                            linesInfor += passenger.path.lines[j].name + ", ";
                                        }
                                        else
                                        {
                                            linesInfor += passenger.path.lines[j].name + "->";
                                        }
                                    }
                                    string stationsInfor = "";
                                    for (int j = 0; j < passenger.path.stations.Count; j++)
                                    {
                                        if (j == passenger.path.stations.Count - 1)
                                        {
                                            stationsInfor += passenger.path.stations[j].name;
                                        }
                                        else
                                        {
                                            stationsInfor += passenger.path.stations[j].name + "->";
                                        }
                                    }
                                    UnityEngine.Debug.Log($"From {stationO.name} to {stationD.name} {passengerNuminSecond}persons, per person {passenger.path.timeD - time}s, "
                                        + linesInfor + stationsInfor);
                                }
                            }
                        }
                    }
                }
            }
        }
        return travelTimeinSecond;
    }

    public int Poisson(float lamda)
    {
        int n = 0;
        double randomValue = new System.Random().NextDouble();
        double probabilitySum = 0;
        while (randomValue >= probabilitySum)
        {
            probabilitySum += Math.Pow(lamda, n) * Math.Pow(Math.E, -lamda) / Factorial(n);
            n++;
        }
        return n - 1;
    }

    public double Factorial(int n)
    {
        double result = 1;
        if (n >= 0)
        {
            for (int i = 1; i <= n; i++)
            {
                result *= i;
            }
        }
        else
        {
            UnityEngine.Debug.Log("FactorialError");
        }
        return result;
    }
}

public class Passenger
{
    public Station stationO;
    public Station stationD;
    public Path path;

    public Passenger(Station stationO, Station stationD, TimeSimulation timeO)
    {
        this.stationO = stationO;
        this.stationD = stationD;
        List<Station> stations = new List<Station>();
        stations.Add(stationO);
        List<Path> paths = new List<Path>();
        paths.Add(new Path(stations, new List<Line>(), new List<Train>(), timeO, new TimeSimulation(timeO).AddSeconds(stationO.goInTime), 
            new Dictionary<Train, TimeSimulation>(), new Dictionary<Train, TimeSimulation>(), new Dictionary<Station, TimeSimulation>()));
        //TimeSimulation arriveTimeDBest = new TimeSimulation(timeO.year + 1, timeO.month, timeO.day, timeO.hour, timeO.minute, timeO.second);
        TimeSimulation arriveTimeDBest = new TimeSimulation(timeO).AddSeconds(24 * 3600);
        //Stopwatch stopwatchFindpath = Stopwatch.StartNew();
        this.path = FindPath(paths, arriveTimeDBest);
        //stopwatchFindpath.Stop();
        //UnityEngine.Debug.Log($"Findpath:{stopwatchFindpath.ElapsedMilliseconds}ms");
    }

    public Path FindPath(List<Path> paths, TimeSimulation arriveTimeDBest)
    {
        if (paths.Count == 1 && paths[0].stations[paths[0].stations.Count - 1].name == stationD.name)
        {
            return paths[0];
        }
        else
        {
            List<Path> newPaths = new List<Path>();
            foreach (Path path in paths)
            {
                TimeSimulation time = path.timeD;
                Station currentStation = path.stations[path.stations.Count - 1];
                if (currentStation.name != stationD.name)
                {
                    foreach (Line line in currentStation.linesIsOperating)
                    {
                        if (!path.lines.Contains(line))
                        {
                            if (line.stationsIsOperating.Contains(stationD))
                            {
                                if (line.stations.IndexOf(stationD) > line.stations.IndexOf(currentStation))
                                {
                                    foreach (Train train in line.trains)
                                    {
                                        if (train.passengers.Count < train.capacity)
                                        {
                                            if (train.departTime.ContainsKey(currentStation) && train.arriveTime.ContainsKey(stationD))
                                            {
                                                if (train.departTime[currentStation] > time)
                                                {
                                                    TimeSimulation DPlusTime = new TimeSimulation(train.arriveTime[stationD]).AddSeconds(stationD.goOutTime);
                                                    if (DPlusTime < arriveTimeDBest)
                                                    {
                                                        Path newpath = new Path(path);
                                                        newpath.stations.Add(stationD);
                                                        newpath.lines.Add(line);
                                                        newpath.trains.Add(train);
                                                        newpath.timeD = DPlusTime;
                                                        if (train.arriveTime[currentStation] > time)
                                                        {
                                                            newpath.goInTrainTime.Add(train, new TimeSimulation(train.arriveTime[currentStation]));
                                                        }
                                                        else
                                                        {
                                                            newpath.goInTrainTime.Add(train, new TimeSimulation(time));
                                                        }
                                                        newpath.goOutTrainTime.Add(train, new TimeSimulation(train.arriveTime[stationD]));
                                                        arriveTimeDBest = newpath.timeD;
                                                        newPaths.Add(newpath);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            foreach (Station tranferStation in line.stationsIsOperating)
                            {
                                if (tranferStation.ableTransfer && !path.stations.Contains(tranferStation) && tranferStation.name != stationD.name)
                                {
                                    if (line.stations.IndexOf(tranferStation) > line.stations.IndexOf(currentStation))
                                    {
                                        foreach (Train train in line.trains)
                                        {
                                            if (train.passengers.Count < train.capacity)
                                            {
                                                if (train.departTime.ContainsKey(currentStation) && train.arriveTime.ContainsKey(tranferStation))
                                                {
                                                    if (train.departTime[currentStation] > time)
                                                    {
                                                        TimeSimulation transferPlusTime = new TimeSimulation(train.arriveTime[tranferStation]).AddSeconds(tranferStation.transferTime);
                                                        if (transferPlusTime < arriveTimeDBest)
                                                        {
                                                            Path newpath = new Path(path);
                                                            newpath.stations.Add(tranferStation);
                                                            newpath.lines.Add(line);
                                                            newpath.trains.Add(train);
                                                            newpath.timeD = transferPlusTime;
                                                            if (train.arriveTime[currentStation] > time)
                                                            {
                                                                newpath.goInTrainTime.Add(train, new TimeSimulation(train.arriveTime[currentStation]));
                                                            }
                                                            else
                                                            {
                                                                newpath.goInTrainTime.Add(train, new TimeSimulation(time));
                                                            }
                                                            newpath.goOutTrainTime.Add(train, new TimeSimulation(train.arriveTime[tranferStation]));
                                                            newpath.transferTime.Add(tranferStation, new TimeSimulation(transferPlusTime));
                                                            newPaths.Add(newpath);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Path newpath = new Path(path);
                    if (newpath.timeD <= arriveTimeDBest)
                    {
                        newPaths.Add(newpath);
                    }
                }
            }
            if (newPaths.Count == 0)
            {
                UnityEngine.Debug.LogError("FindPath = 0");
            }
            return FindPath(newPaths, arriveTimeDBest);
        }
    }
}

public class Path
{
    public List<Station> stations;
    public List<Line> lines;
    public List<Train> trains;
    public TimeSimulation timeO;
    public TimeSimulation timeD;
    public Dictionary<Train, TimeSimulation> goInTrainTime;
    public Dictionary<Train, TimeSimulation> goOutTrainTime;
    public Dictionary<Station, TimeSimulation> transferTime;

    public Path(List<Station> stations, List<Line> lines, List<Train> trains, TimeSimulation timeO, TimeSimulation timeD,
        Dictionary<Train, TimeSimulation> goInTrainTime, Dictionary<Train, TimeSimulation> goOutTrainTime, 
        Dictionary<Station, TimeSimulation> transferTime)
    {
        this.stations = new List<Station>(stations);
        this.lines = new List<Line>(lines);
        this.trains = new List<Train>(trains);
        this.goInTrainTime = new Dictionary<Train, TimeSimulation>(goInTrainTime);
        this.goOutTrainTime = new Dictionary<Train, TimeSimulation>(goOutTrainTime);
        this.transferTime = new Dictionary<Station, TimeSimulation>(transferTime);
        this.timeO = timeO;
        this.timeD = timeD;
    }

    public Path(Path path)
    {
        this.stations = new List<Station>(path.stations);
        this.lines = new List<Line>(path.lines);
        this.trains = new List<Train>(path.trains);
        this.goInTrainTime = new Dictionary<Train, TimeSimulation>(path.goInTrainTime);
        this.goOutTrainTime = new Dictionary<Train, TimeSimulation>(path.goOutTrainTime);
        this.transferTime = new Dictionary<Station, TimeSimulation>(path.transferTime);
        this.timeO = path.timeO;
        this.timeD = path.timeD;
    }
}