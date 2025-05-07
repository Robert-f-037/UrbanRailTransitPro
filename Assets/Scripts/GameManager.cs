using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public mode myMode = mode.solution;
    public TimeSimulation startTime;
    public TimeSimulation endTime;
    public TimeSimulation currentTime;
    private string timeFormat = "yyyy/M/d HH:mm:ss";
    public float timeSpeed = 1f;
    public float travelTime = 0f;
    public Dictionary<int, List<string[]>> TrainMatrixWeekday;
    public Dictionary<int, List<string[]>> TrainMatrixWeekend;
    public InputField timeText;
    public Text populationText;
    public Image colorImage;
    public GameObject backGround;
    public GameObject trainPanel;
    public GameObject stationPanel;
    private float offsetRatio;
    [HideInInspector]
    public int passengerNumMax;
    [HideInInspector]
    public Train trainClick;
    [HideInInspector]
    public GameObject stationChildClick;
    private Station stationClick;
    public GameObject controButton;
    private Button startButton;
    private Button stopButton;
    private Button speedButton;
    public Button exitButton;

    public static GameManager gameManager { private set; get; }
    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == "Start")
        {
            Destroy(this);
        }

        if (gameManager)
        {
            Destroy(this);
        }
        else
        {
            gameManager = this;
            DontDestroyOnLoad(this);
        }
    }

    void Start()
    {
        passengerNumMax = GetComponent<Passengerflow>().passengerNumMax;
        offsetRatio = 0.5f * GetComponent<Passengerflow>().trainPrefabParent.transform.GetChild(0).localScale.x;
        startButton = controButton.transform.Find("start").GetComponent<Button>();
        stopButton = controButton.transform.Find("stop").GetComponent<Button>();
        speedButton = controButton.transform.Find("speed").GetComponent<Button>();
        startButton.onClick.AddListener(StartButtonRun);
        stopButton.onClick.AddListener(StopButtonRun);
        speedButton.onClick.AddListener(SpeedChange);
        timeText.onValueChanged.AddListener(StartTimeChange);
        timeText.text = startTime.TextForm(true);
        exitButton.onClick.AddListener(ExitScene);
        exitButton.transform.Find("window").gameObject.SetActive(false);
        exitButton.transform.Find("window").Find("OK").GetComponent<Button>().onClick.AddListener(ExitOK);
        exitButton.transform.Find("window").Find("Cancel").GetComponent<Button>().onClick.AddListener(ExitCancel);

        startTime.AddSeconds(0);
        endTime.AddSeconds(0);
        foreach (Station station in GetComponent<Passengerflow>().stations.GetComponentsInChildren<Station>())
        {
            TimeSimulation startStationTimeWeekend = new TimeSimulation(startTime.year, startTime.month, startTime.day,
                station.operateTime.startHourWeekend, station.operateTime.startMinuteWeekend, 0);
            startStationTimeWeekend.AddSeconds(12 * 3600);
            station.operateTime.endHourWeekend = startStationTimeWeekend.hour;
            station.operateTime.endMinuteWeekend = startStationTimeWeekend.minute;
            TimeSimulation startStationTimeWeekday = new TimeSimulation(startTime.year, startTime.month, startTime.day,
                station.operateTime.startHourWeekday, station.operateTime.startMinuteWeekday, 0);
            startStationTimeWeekday.AddSeconds(12 * 3600);
            station.operateTime.endHourWeekday = startStationTimeWeekday.hour;
            station.operateTime.endMinuteWeekday = startStationTimeWeekday.minute;

            station.lines = new Dictionary<GameObject, List<Line>>();
            station.passengersInLineCount = new Dictionary<GameObject, int>();
        }
        foreach (Station station in GetComponent<Passengerflow>().stations.GetComponentsInChildren<Station>())
        {
            for (int i = 0; i < station.transform.childCount; i++)
            {
                station.lines.Add(station.transform.GetChild(i).gameObject, new List<Line>());
                station.passengersInLineCount.Add(station.transform.GetChild(i).gameObject, 0);
            }
        }
        foreach (Line line in GetComponent<Passengerflow>().lines.GetComponentsInChildren<Line>())
        {
            foreach (Station station in line.stations)
            {
                for (int i = 0; i < station.transform.childCount; i++)
                {
                    if (line.name.Contains(station.transform.GetChild(i).name))
                    {
                        station.lines[station.transform.GetChild(i).gameObject].Add(line);
                    }
                }
                station.passengers.Add(line, new List<Passenger>());
            }
        }

        if (myMode == mode.simulation)
        {
            trainPanel.SetActive(false);
            stationPanel.SetActive(false);
            colorImage.transform.Find("personMax").GetComponent<Text>().text = $"{ passengerNumMax}";
            colorImage.transform.Find("personMax2").GetComponent<Text>().text = $"{ passengerNumMax/2}";
            speedButton.transform.Find("Text (Legacy)").GetComponent<Text>().text = $"¡Á{timeSpeed}";
            speedButton.transform.Find("Text (Legacy)").GetComponent<Text>().fontSize = 28;

            TrainStartPlan();
            currentTime = new TimeSimulation(startTime);
            TrainPlaninDay(currentTime);
            StationOperate(currentTime);

            StartCoroutine(TimeUpdate());
        }
        else if (myMode == mode.solution)
        {
            TrainStartPlan();
            Stopwatch stopwatchTotal = Stopwatch.StartNew();
            for (TimeSimulation currentTime = new TimeSimulation(startTime); currentTime <= endTime; currentTime.AddSeconds(1))
            {
                TrainPlaninDay(currentTime);
                //UnityEngine.Debug.Log($"Train:{stopwatchTrain.ElapsedMilliseconds}ms");
                StationOperate(currentTime);
                //Stopwatch stopwatchPassenger = Stopwatch.StartNew();
                travelTime += GetComponent<Passengerflow>().CreatPassenger(currentTime);
                //stopwatchPassenger.Stop();
                //UnityEngine.Debug.Log($"Passenger:{stopwatchPassenger.ElapsedMilliseconds}ms");
            }
            UnityEngine.Debug.Log(travelTime);
            stopwatchTotal.Stop();
            UnityEngine.Debug.Log($"Total:{stopwatchTotal.ElapsedMilliseconds}ms");
        }
    }

    void Update()
    {
        if (myMode == mode.simulation)
        {
            Display(currentTime);
        }
    }

    IEnumerator TimeUpdate()
    {
        while (true)
        {
            if (stopButton.gameObject.activeSelf && !startButton.gameObject.activeSelf)
            {
                TrainPlaninDay(currentTime);
                StationOperate(currentTime);
                travelTime += GetComponent<Passengerflow>().CreatPassenger(currentTime);
                currentTime.AddSeconds(1);
            }
            yield return new WaitForSeconds(1f / timeSpeed);
        }
    }

    public void ExitOK()
    {
        SceneManager.LoadScene("Start");
    }

    public void ExitCancel()
    {
        exitButton.transform.Find("window").gameObject.SetActive(false);
    }


    public void ExitScene()
    {
        startButton.gameObject.SetActive(true);
        stopButton.gameObject.SetActive(false);
        exitButton.transform.Find("window").gameObject.SetActive(true);
    }

    public void StartButtonRun()
    {
        startButton.gameObject.SetActive(false);
        stopButton.gameObject.SetActive(true);
    }

    public void StopButtonRun()
    {
        startButton.gameObject.SetActive(true);
        stopButton.gameObject.SetActive(false);
    }

    public void SpeedChange()
    {
        if (timeSpeed == 16)
        {
            timeSpeed = 30;
        }
        else if (timeSpeed == 60)
        {
            timeSpeed = 1;
        }
        else
        {
            timeSpeed *= 2;
        }
        speedButton.transform.Find("Text (Legacy)").GetComponent<Text>().text = $"¡Á{timeSpeed}";
        if (timeSpeed < 10)
        {
            speedButton.transform.Find("Text (Legacy)").GetComponent<Text>().fontSize = 28;
        }
        else
        {
            speedButton.transform.Find("Text (Legacy)").GetComponent<Text>().fontSize = 20;
        }
    }

    public void StartTimeChange(string inputStartTime)
    {
        if (timeText.isFocused)
        {
            startButton.gameObject.SetActive(true);
            stopButton.gameObject.SetActive(false);
            if (DateTime.TryParseExact(inputStartTime, timeFormat,
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime startDateTime))
            {
                TimeSimulation tempstartTime = new TimeSimulation(startDateTime.Year, startDateTime.Month, startDateTime.Day, 
                    startDateTime.Hour, startDateTime.Minute, startDateTime.Second);
                tempstartTime.AddSeconds(0);
                if (tempstartTime != currentTime)
                {
                    startTime = new TimeSimulation(tempstartTime);
                    currentTime = new TimeSimulation(startTime);
                    currentTime.AddSeconds(0);

                    foreach (Line line in GetComponent<Passengerflow>().lines.GetComponentsInChildren<Line>())
                    {
                        foreach (Train train in line.trains)
                        {
                            if (train.trainObject)
                            {
                                Destroy(train.trainObject);
                            }
                            train.passengers = new List<Passenger>();
                            train.passengersInVehicle = new List<Passenger>();
                        }
                        foreach (Station station in line.stations)
                        {
                            station.passengers[line] = new List<Passenger>();
                            foreach (GameObject varObject in station.passengersInLineCount.Keys.ToList())
                            {
                                station.passengersInLineCount[varObject] = 0;
                            }
                            station.passengersCount = 0;
                        }
                    }

                    travelTime = 0f;
                }
            }
        }
    }

    public void Display(TimeSimulation time)
    {
        if (!timeText.isFocused)
        {
            timeText.text = time.TextForm(true);
        }
        foreach (Line line in GetComponent<Passengerflow>().lines.GetComponentsInChildren<Line>())
        {
            foreach (Train train in line.trains)
            {
                if (train.trainObject)
                {
                    Transform trainTransform = train.trainObject.transform;
                    float ratio = (float)train.passengersInVehicle.Count / (float)passengerNumMax;
                    if (ratio <= 0.5f)
                    {
                        trainTransform.GetChild(0).GetComponent<Renderer>().material.color = new Color(ratio * 2, 1f, 0f);
                    }
                    else if (ratio > 0.5f && ratio <= 1f)
                    {
                        trainTransform.GetChild(0).GetComponent<Renderer>().material.color = new Color(1f, 2 - 2 * ratio, 0f);
                    }
                    else
                    {
                        trainTransform.GetChild(0).GetComponent<Renderer>().material.color = new Color(1f / ratio, 0f, 0f);
                    }
                }
            }
        }
        string personsNum = "Population\n";
        bool formMarker = false;
        foreach (Station station in GetComponent<Passengerflow>().stations.GetComponentsInChildren<Station>())
        {
            personsNum += $"{station.name}:";
            for (int i = 0; i < station.transform.childCount; i++)
            {
                float ratio = (float)station.passengersInLineCount[station.transform.GetChild(i).gameObject] / (float)passengerNumMax;
                if (ratio <= 0.5f)
                {
                    station.transform.GetChild(i).GetComponent<Renderer>().material.color = new Color(ratio * 2, 1f, 0f);
                }
                else if (ratio > 0.5f && ratio <= 1f)
                {
                    station.transform.GetChild(i).GetComponent<Renderer>().material.color = new Color(1f, 2 - 2 * ratio, 0f);
                }
                else
                {
                    station.transform.GetChild(i).GetComponent<Renderer>().material.color = new Color(1f / ratio, 0f, 0f);
                }
            }
            personsNum += (formMarker ? $"{station.passengersCount}\n" : $"{station.passengersCount}\t\t");
            formMarker = !formMarker;
        }
        populationText.text = personsNum;

        if (trainClick && !trainClick.trainObject)
        {
            trainPanel.SetActive(false);
        }
        if (trainPanel.activeSelf)
        {
            trainPanel.transform.Find("line").GetComponent<Text>().text = $"A train on {trainClick.line.name}";
            if (trainClick.isDwell)
            {
                trainPanel.transform.Find("dwell").GetComponent<Text>().text = $"The train is stopping at {trainClick.line.stations[trainClick.nextStationid - 1]}.";
            }
            else
            {
                trainPanel.transform.Find("dwell").GetComponent<Text>().text = $"The train has not stopped.";
            }
            if (trainClick.nextStationid >= trainClick.line.stations.Count)
            {
                trainPanel.transform.Find("next").GetComponent<Text>().text = $"The train has arrived at the last station.";
                trainPanel.transform.Find("population").GetComponent<Text>().text = "";
            }
            else
            {
                Station nextStation = trainClick.line.stations[trainClick.nextStationid];
                trainPanel.transform.Find("next").GetComponent<Text>().text = $"The next station of the train will arrive at {nextStation.name} " +
                    $"at {trainClick.arriveTime[nextStation].TextForm(false)}.";
                trainPanel.transform.Find("population").GetComponent<Text>().text
                    = $"There are currently {trainClick.passengersInVehicle.Count} people on the train.\n" +
                    $"{trainClick.nextStationGoOn} people will get on and {trainClick.nextStationGoOff} people will get off at the next station.";
            }
        }

        if (!stationChildClick)
        {
            stationPanel.SetActive(false);
        }
        if (stationPanel.activeSelf)
        {
            stationClick = stationChildClick.transform.parent.GetComponent<Station>();
            List<Line> lines = stationClick.lines[stationChildClick];
            if (!stationClick.isOperating)
            {
                stationPanel.transform.Find("name").GetComponent<Text>().text = stationClick.name;
                stationPanel.transform.Find("line").GetComponent<Text>().text = "Not operate";
                stationPanel.transform.Find("next").GetComponent<Text>().text = "";
                stationPanel.transform.Find("population").GetComponent<Text>().text = "";
            }
            else
            {
                stationPanel.transform.Find("name").GetComponent<Text>().text = stationClick.name;
                stationPanel.transform.Find("line").GetComponent<Text>().text = $"/{stationChildClick.name}";
                string nextText = "";
                string population = $"There are a total of {stationClick.passengersCount} people on the station" + 
                    $" and {stationClick.passengersInLineCount[stationChildClick]} people on the {stationChildClick.name} of the station";
                int transferInCount = 0;
                TimeSimulation maxTime = new TimeSimulation(time);
                int transferOutCount = 0;
                int waitCount = 0;
                int exitCount = 0;
                foreach (Line line in lines)
                {
                    int nextTrainid = stationClick.NextTrainid(time, line);
                    if (nextTrainid == -1)
                    {
                        nextText += $"There is no next train on the {line.name} of the station.\n";
                    }
                    else
                    {
                        Train train = line.trains[nextTrainid];
                        nextText += $"The next train on {line.name} will arrive at {train.arriveTime[stationClick].TextForm(false)}, " +
                            $"{train.nextStationGoOn} people will get on and {train.nextStationGoOff} people will get off.\n";
                    }
                    if (stationClick.ableTransfer)
                    {
                        foreach (Passenger passenger in stationClick.passengers[line])
                        {
                            if (passenger.stationD == stationClick)
                            {
                                exitCount++;
                            }
                            else if (passenger.path.transferTime.ContainsKey(stationClick))
                            {
                                if (time < passenger.path.transferTime[stationClick])
                                {
                                    transferOutCount++;
                                }
                                else
                                {
                                    waitCount++;
                                }
                            }
                            else
                            {
                                waitCount++;
                            }
                        }
                        foreach (List<Line> lineElses in stationClick.lines.Values)
                        {
                            if (lineElses != lines)
                            {
                                foreach (Line lineElse in lineElses)
                                {
                                    foreach (Passenger passengerElse in stationClick.passengers[lineElse])
                                    {
                                        if (passengerElse.path.transferTime.ContainsKey(stationClick) && passengerElse.path.lines.Contains(line))
                                        {
                                            if (time < passengerElse.path.transferTime[stationClick])
                                            {
                                                transferInCount++;
                                                if (maxTime < passengerElse.path.transferTime[stationClick])
                                                {
                                                    maxTime = passengerElse.path.transferTime[stationClick];
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
                        foreach (Passenger passenger in stationClick.passengers[line])
                        {
                            if (passenger.stationD == stationClick)
                            {
                                exitCount++;
                            }
                            else
                            {
                                waitCount++;
                            }
                        }
                    }
                }
                stationPanel.transform.Find("next").GetComponent<Text>().text = nextText;
                if (stationClick.ableTransfer)
                {
                    stationPanel.transform.Find("population").GetComponent<Text>().text = population +
                        $", of which {exitCount} are exiting" +
                        $", {waitCount} are waiting" +
                        $", {transferOutCount} are transferring to other lines" +
                        (transferInCount > 0? 
                        $", and until {maxTime.TextForm(false)}, {transferInCount} people will transfer to the line."
                        : $", and {transferInCount} people will transfer to the line.");
                }
                else
                {
                    stationPanel.transform.Find("population").GetComponent<Text>().text = population +
                        $", of which {exitCount} are exiting" +
                        $", {waitCount} are waiting.";
                }
            }
        }
    }


    public void TrainPlaninDay(TimeSimulation time)
    {
        foreach (Line line in GetComponent<Passengerflow>().lines.GetComponentsInChildren<Line>())
        {
            if (time.week == DayOfWeek.Saturday || time.week == DayOfWeek.Sunday)
            {
                if ((time.hour == line.operateTime.startHourWeekend && time.minute == line.operateTime.startMinuteWeekend && time.second == 0) || 
                    (time == startTime && startTime.hour > line.operateTime.startHourWeekend) ||
                    (time == startTime && startTime.hour == line.operateTime.startHourWeekend && startTime.minute > line.operateTime.startMinuteWeekend) ||
                    (time == startTime && startTime.hour == line.operateTime.startHourWeekend && startTime.minute == line.operateTime.startMinuteWeekend) && time.second > 0
                    )
                {
                    foreach (Train train in line.trains)
                    {
                        if (train.week == "weekend")
                        {
                            foreach (Station station in train.arriveTime.Keys.ToList())
                            {
                                if (train.arriveTime[station].hour < train.line.operateTime.startHourWeekend ||
                                    train.arriveTime[station].hour == train.line.operateTime.startHourWeekend &&
                                    train.arriveTime[station].minute < train.line.operateTime.startMinuteWeekend)
                                {
                                    train.arriveTime[station] = new TimeSimulation(time.year, time.month, time.day + 1,
                                    train.arriveTime[station].hour, train.arriveTime[station].minute, train.arriveTime[station].second);
                                }
                                else
                                {
                                    train.arriveTime[station] = new TimeSimulation(time.year, time.month, time.day,
                                    train.arriveTime[station].hour, train.arriveTime[station].minute, train.arriveTime[station].second);
                                }
                            }
                            foreach (Station station in train.departTime.Keys.ToList())
                            {
                                if (train.departTime[station].hour < train.line.operateTime.startHourWeekend ||
                                    train.departTime[station].hour == train.line.operateTime.startHourWeekend &&
                                    train.departTime[station].minute < train.line.operateTime.startMinuteWeekend)
                                {
                                    train.departTime[station] = new TimeSimulation(time.year, time.month, time.day + 1,
                                    train.departTime[station].hour, train.departTime[station].minute, train.departTime[station].second);
                                }
                                else
                                {
                                    train.departTime[station] = new TimeSimulation(time.year, time.month, time.day,
                                    train.departTime[station].hour, train.departTime[station].minute, train.departTime[station].second);
                                }
                            }
                        }
                        else if (train.week == "weekday")
                        {
                            foreach (Station station in train.arriveTime.Keys.ToList())
                            {
                                train.arriveTime[station] = new TimeSimulation(time.year + 1, time.month, time.day,
                                    train.arriveTime[station].hour, train.arriveTime[station].minute, train.arriveTime[station].second);
                            }
                            foreach (Station station in train.departTime.Keys.ToList())
                            {
                                train.departTime[station] = new TimeSimulation(time.year + 1, time.month, time.day,
                                    train.departTime[station].hour, train.departTime[station].minute, train.departTime[station].second);
                            }
                        }
                    }
                }
            }
            else
            {
                if ((time.hour == line.operateTime.startHourWeekday && time.minute == line.operateTime.startMinuteWeekday && time.second == 0) ||
                    (time == startTime && startTime.hour > line.operateTime.startHourWeekday) ||
                    (time == startTime && startTime.hour == line.operateTime.startHourWeekday && startTime.minute > line.operateTime.startMinuteWeekday) ||
                    (time == startTime && startTime.hour == line.operateTime.startHourWeekday && startTime.minute == line.operateTime.startMinuteWeekday) && time.second > 0)
                {
                    foreach (Train train in line.trains)
                    {
                        if (train.week == "weekend")
                        {
                            foreach (Station station in train.arriveTime.Keys.ToList())
                            {
                                train.arriveTime[station] = new TimeSimulation(time.year + 1, time.month, time.day,
                                    train.arriveTime[station].hour, train.arriveTime[station].minute, train.arriveTime[station].second);
                            }
                            foreach (Station station in train.departTime.Keys.ToList())
                            {
                                train.departTime[station] = new TimeSimulation(time.year + 1, time.month, time.day,
                                    train.departTime[station].hour, train.departTime[station].minute, train.departTime[station].second);
                            }
                        }
                        else if (train.week == "weekday")
                        {
                            foreach (Station station in train.arriveTime.Keys.ToList())
                            {
                                if (train.arriveTime[station].hour < train.line.operateTime.startHourWeekday ||
                                    train.arriveTime[station].hour == train.line.operateTime.startHourWeekday &&
                                    train.arriveTime[station].minute < train.line.operateTime.startMinuteWeekday)
                                {
                                    train.arriveTime[station] = new TimeSimulation(time.year, time.month, time.day + 1,
                                    train.arriveTime[station].hour, train.arriveTime[station].minute, train.arriveTime[station].second);
                                }
                                else
                                {
                                    train.arriveTime[station] = new TimeSimulation(time.year, time.month, time.day,
                                    train.arriveTime[station].hour, train.arriveTime[station].minute, train.arriveTime[station].second);
                                }
                            }
                            foreach (Station station in train.departTime.Keys.ToList())
                            {
                                if (train.departTime[station].hour < train.line.operateTime.startHourWeekday ||
                                    train.departTime[station].hour == train.line.operateTime.startHourWeekday &&
                                    train.departTime[station].minute < train.line.operateTime.startMinuteWeekday)
                                {
                                    train.departTime[station] = new TimeSimulation(time.year, time.month, time.day + 1,
                                    train.departTime[station].hour, train.departTime[station].minute, train.departTime[station].second);
                                }
                                else
                                {
                                    train.departTime[station] = new TimeSimulation(time.year, time.month, time.day,
                                    train.departTime[station].hour, train.departTime[station].minute, train.departTime[station].second);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public void TrainStartRun(TimeSimulation time, Line line, string week)
    {
        if (week == "weekend") 
        {
            int timeSeconds = time.hour * 3600 + time.minute * 60 + time.second;
            if (TrainMatrixWeekend.ContainsKey(timeSeconds))
            {
                foreach (string[] row in TrainMatrixWeekend[timeSeconds])
                {
                    if (line.name == row[0])
                    {
                        Station startStation = line.stations[0];
                        Station endStation = line.stations[line.stations.Count - 1];
                        if (row[1] != null)
                        {
                            foreach (Station station in line.stations)
                            {
                                if (station.name == row[1])
                                {
                                    startStation = station;
                                }
                            }
                        }
                        if (row[2] != null)
                        {
                            foreach (Station station in line.stations)
                            {
                                if (station.name == row[2])
                                {
                                    endStation = station;
                                }
                            }
                        }
                        Train train = new Train();
                        train.CreateTrain(passengerNumMax, line, startStation, endStation, time, week);
                    }
                }
            }
        }
        else if (week == "weekday")
        {
            int timeSeconds = time.hour * 3600 + time.minute * 60 + time.second;
            if (TrainMatrixWeekday.ContainsKey(timeSeconds))
            {
                foreach (string[] row in TrainMatrixWeekday[timeSeconds])
                {
                    if (line.name == row[0])
                    {
                        Station startStation = line.stations[0];
                        Station endStation = line.stations[line.stations.Count - 1];
                        if (row[1] != null)
                        {
                            foreach (Station station in line.stations)
                            {
                                if (station.name == row[1])
                                {
                                    startStation = station;
                                }
                            }
                        }
                        if (row[2] != null)
                        {
                            foreach (Station station in line.stations)
                            {
                                if (station.name == row[2])
                                {
                                    endStation = station;
                                }
                            }
                        }
                        Train train = new Train();
                        train.CreateTrain(passengerNumMax, line, startStation, endStation, time, week);
                    }
                }
            }
        }
    }

    public void TrainStartPlan()
    {
        foreach (Line line in GetComponent<Passengerflow>().lines.GetComponentsInChildren<Line>())
        {
            TimeSimulation startLineTime = new TimeSimulation(2023, 10, 1,
                line.operateTime.startHourWeekend, line.operateTime.startMinuteWeekend, 0);
            TimeSimulation endLineTime = new TimeSimulation(2023, 10, 1,
                line.operateTime.endHourWeekend, line.operateTime.endMinuteWeekend, 0);
            if (endLineTime <= startLineTime)
            {
                endLineTime.AddSeconds(24 * 3600);
            }
            for (TimeSimulation trainTime = startLineTime; trainTime <= endLineTime; trainTime.AddSeconds(1))
            {
                TrainStartRun(trainTime, line, "weekend");
            }
            foreach (Station station in line.stations)
            {
                if (line.trains.Count > 0)
                {
                    TimeSimulation startStationTime
                        = new TimeSimulation(2023, 10, 1,
                        station.operateTime.startHourWeekend, station.operateTime.startMinuteWeekend, 0);
                    TimeSimulation endStationTime
                        = new TimeSimulation(2023, 10, 1,
                        station.operateTime.endHourWeekend, station.operateTime.endMinuteWeekend, 0);
                    if (endStationTime <= startStationTime)
                    {
                        endStationTime.AddSeconds(24 * 3600);
                    }
                    foreach (Train train in line.trains)
                    {
                        if (train.departTime.ContainsKey(station))
                        {
                            if (endStationTime < train.departTime[station])
                            {
                                endStationTime = train.departTime[station];
                            }
                        }
                    }
                    station.operateTime.endHourWeekend = endStationTime.hour;
                    station.operateTime.endMinuteWeekend = endStationTime.minute;
                }
            }

            startLineTime = new TimeSimulation(2023, 10, 2,
                    line.operateTime.startHourWeekday, line.operateTime.startMinuteWeekday, 0);
            endLineTime = new TimeSimulation(2023, 10, 2,
                line.operateTime.endHourWeekday, line.operateTime.endMinuteWeekday, 0);
            if (endLineTime <= startLineTime)
            {
                endLineTime.AddSeconds(24 * 3600);
            }
            for (TimeSimulation trainTime = startLineTime; trainTime <= endLineTime; trainTime.AddSeconds(1))
            {
                TrainStartRun(trainTime, line, "weekday");
            }
            foreach (Station station in line.stations)
            {
                if (line.trains.Count > 0)
                {
                    TimeSimulation startStationTime
                        = new TimeSimulation(2023, 10, 2,
                        station.operateTime.startHourWeekday, station.operateTime.startMinuteWeekday, 0);
                    TimeSimulation endStationTime
                        = new TimeSimulation(2023, 10, 2,
                        station.operateTime.endHourWeekday, station.operateTime.endMinuteWeekday, 0);
                    if (endStationTime <= startStationTime)
                    {
                        endStationTime.AddSeconds(24 * 3600);
                    }
                    foreach (Train train in line.trains)
                    {
                        if (train.departTime.ContainsKey(station))
                        {
                            if (endStationTime < train.departTime[station])
                            {
                                endStationTime = train.departTime[station];
                            }
                        }
                    }
                    station.operateTime.endHourWeekday = endStationTime.hour;
                    station.operateTime.endMinuteWeekday = endStationTime.minute;
                }
            }
        }
    }

    public void StationOperate(TimeSimulation time)
    {
        foreach (Station station in GetComponent<Passengerflow>().stations.GetComponentsInChildren<Station>())
        {
            station.isOperating = station.operateTime.Contains(time);
        }
        foreach (Line line in GetComponent<Passengerflow>().lines.GetComponentsInChildren<Line>())
        {
            foreach (Train train in line.trains)
            {
                if (time == train.arriveTime[train.startStation] || time == startTime && train.arriveTime[train.startStation] < startTime)
                {
                    GameObject trainObject = Instantiate(GetComponent<Passengerflow>().trainPrefabParent);
                    trainObject.transform.parent = GetComponent<Passengerflow>().trains.transform;
                    train.trainObject = trainObject;
                }
            }
            foreach (Train train in line.trains)
            {
                if (train.trainObject)
                {
                    train.nextStationid = train.NextStationid(time);
                    Station currentStation = line.stations[train.nextStationid - 1];
                    if (time < train.departTime[currentStation])
                    {
                        train.isDwell = true;
                        foreach (Passenger passenger in currentStation.passengers[line])
                        {
                            if (passenger.path.goInTrainTime.ContainsKey(train) && passenger.path.goInTrainTime[train] == time)
                            {
                                train.passengersInVehicle.Add(passenger);
                            }
                        }
                        foreach (Passenger passenger in train.passengersInVehicle)
                        {
                            if (passenger.path.goInTrainTime.ContainsKey(train) && passenger.path.goOutTrainTime[train] == time)
                            {
                                currentStation.passengers[line].Add(passenger);
                            }
                        }
                        currentStation.passengers[line].RemoveAll(passenger => passenger.path.goInTrainTime.ContainsKey(train) && passenger.path.goInTrainTime[train] == time);
                        train.passengersInVehicle.RemoveAll(passenger => passenger.path.goInTrainTime.ContainsKey(train) && passenger.path.goOutTrainTime[train] == time);
                        train.passengers.RemoveAll(passenger => passenger.path.goInTrainTime.ContainsKey(train) && passenger.path.goOutTrainTime[train] == time);
                        //foreach (Passenger passengerInVehicle in train.passengersInVehicle)
                        //{
                        //    if (passengerInVehicle.path.stations.Contains(currentStation))
                        //    {
                        //        currentStation.passengers.Add(passengerInVehicle);
                        //        train.passengers.Remove(passengerInVehicle);
                        //    }
                        //}
                        //train.passengersInVehicle.RemoveAll(passenger => currentStation.passengers.Contains(passenger));
                        //foreach (Passenger passenger in currentStation.passengers)
                        //{
                        //    if (passenger.path.trains.Contains(train) && train.passengers.Contains(passenger))
                        //    {
                        //        train.passengersInVehicle.Add(passenger);
                        //    }
                        //}
                        //currentStation.passengers.RemoveAll(passenger => train.passengersInVehicle.Contains(passenger));
                    }
                    else
                    {
                        train.isDwell = false;
                    }
                    if (time > train.departTime[train.endStation])
                    {
                        Destroy(train.trainObject);
                    }
                    else
                    {
                        if (myMode == mode.simulation && train.trainObject)
                        {
                            Station nextStation;
                            if (currentStation != train.endStation)
                            {
                                nextStation = line.stations[train.nextStationid];

                                train.nextStationGoOff = 0;
                                foreach (Passenger passenger in train.passengersInVehicle)
                                {
                                    if (passenger.path.stations.Contains(nextStation))
                                    {
                                        train.nextStationGoOff++;
                                    }
                                }
                                train.nextStationGoOn = 0;
                                foreach (Passenger passenger in nextStation.passengers[train.line])
                                {
                                    if (passenger.path.trains.Contains(train))
                                    {
                                        train.nextStationGoOn++;
                                    }
                                }
                            }
                            else
                            {
                                nextStation = line.stations[train.nextStationid - 2];
                            }
                            Vector3 nextStationPos = nextStation.transform.position;
                            for (int i = 0; i < nextStation.transform.childCount; i++)
                            {
                                if (train.line.name.Contains(nextStation.transform.GetChild(i).name))
                                {
                                    nextStationPos = nextStation.transform.GetChild(i).position;
                                    break;
                                }
                            }
                            Vector3 currentStationPos = currentStation.transform.position;
                            for (int i = 0; i < currentStation.transform.childCount; i++)
                            {
                                if (train.line.name.Contains(currentStation.transform.GetChild(i).name))
                                {
                                    currentStationPos = currentStation.transform.GetChild(i).position;
                                    break;
                                }
                            }
                            Vector3 link;
                            if (currentStation != train.endStation)
                            {
                                link = nextStationPos - currentStationPos;
                            }
                            else
                            {
                                link = currentStationPos - nextStationPos;
                            }
                            Vector3 offset = offsetRatio * Vector3.Cross(link, new Vector3(0f, 0f, 1f)).normalized;

                            Transform trainTransform = train.trainObject.transform;
                            if (currentStation == train.endStation)
                            {
                                trainTransform.position = currentStationPos + offset + trainTransform.parent.position;
                            }
                            else
                            {
                                if (time > train.departTime[currentStation])
                                {
                                    float ratio = (float)(time - train.departTime[currentStation])
                                        / (float)(train.arriveTime[nextStation] - train.departTime[currentStation]);
                                    trainTransform.position = currentStationPos + ratio * link
                                        + offset + trainTransform.parent.position;
                                    trainTransform.LookAt(trainTransform.position + link);
                                }
                                else
                                {
                                    trainTransform.position = currentStationPos
                                        + offset + trainTransform.parent.position;
                                    trainTransform.LookAt(trainTransform.position + link);
                                }
                            }
                        }
                    }
                }
            }

            line.stationsIsOperating = new List<Station>();
            line.isOperating = line.operateTime.Contains(time) && line.trains.Count > 0;
            for (int i = 0; i < line.stations.Count; i++)
            {
                if (line.isOperating && line.stations[i].isOperating)
                {
                    line.stationsIsOperating.Add(line.stations[i]);
                }
                else
                {

                }
            }
        }
        foreach (Station station in GetComponent<Passengerflow>().stations.GetComponentsInChildren<Station>())
        {
            station.linesIsOperating = new List<Line>();
            foreach (List<Line> lines in station.lines.Values)
            {
                foreach (Line line in lines)
                {
                    if (station.isOperating && line.isOperating)
                    {
                        station.linesIsOperating.Add(line);
                    }
                    else
                    {

                    }
                }
            }
            station.ableTransfer = station.isTransferStation && (station.linesIsOperating.Count > 1);

            foreach (List<Line> lines in station.lines.Values)
            {
                foreach (Line line in lines)
                {
                    if (station.ableTransfer)
                    {
                        List<Passenger> transferPassenger = new List<Passenger>();
                        foreach (Passenger passenger in station.passengers[line])
                        {
                            if (passenger.path.transferTime.ContainsKey(station) && time == passenger.path.transferTime[station])
                            {
                                int i = passenger.path.lines.IndexOf(line) + 1;
                                if (i < passenger.path.lines.Count)
                                {
                                    station.passengers[passenger.path.lines[i]].Add(passenger);
                                    transferPassenger.Add(passenger);
                                }
                            }
                        }
                        station.passengers[line].RemoveAll(passenger => transferPassenger.Contains(passenger));
                    }
                    station.passengers[line].RemoveAll(passenger => time >= passenger.path.timeD);
                }
            }

            station.passengersCount = 0;
            for (int i = 0; i < station.transform.childCount; i++)
            {
                station.passengersInLineCount[station.transform.GetChild(i).gameObject] = 0;
                foreach (Line line in station.lines[station.transform.GetChild(i).gameObject])
                {
                    station.passengersInLineCount[station.transform.GetChild(i).gameObject] += station.passengers[line].Count;
                    station.passengersCount += station.passengers[line].Count;
                }
            }
        }
    }
}
[Serializable]
public enum mode { solution, simulation };
[Serializable]
public class TimeSimulation
{
    public int year;
    public int month;
    public int day;
    public int hour;
    public int minute;
    public int second;
    public DayOfWeek week { get; private set; }

    public TimeSimulation(int year, int month, int day, int hour, int minute, int second)
    {
        this.year = year;
        this.month = month;
        this.day = day;
        this.hour = hour;
        this.minute = minute;
        this.second = second;
        week = new DateTime(year, month, day, hour, minute, second).DayOfWeek;
    }

    public TimeSimulation(TimeSimulation timeSimulation)
    {
        this.year = timeSimulation.year;
        this.month = timeSimulation.month;
        this.day = timeSimulation.day;
        this.hour = timeSimulation.hour;
        this.minute = timeSimulation.minute;
        this.second = timeSimulation.second;
        this.week = timeSimulation.week;
    }

    public static bool operator >(TimeSimulation left, TimeSimulation right)
    {
        return new DateTime(left.year, left.month, left.day, left.hour, left.minute, left.second) >
            new DateTime(right.year, right.month, right.day, right.hour, right.minute, right.second);
    }
    public static bool operator >=(TimeSimulation left, TimeSimulation right)
    {
        return new DateTime(left.year, left.month, left.day, left.hour, left.minute, left.second) >=
            new DateTime(right.year, right.month, right.day, right.hour, right.minute, right.second);
    }
    public static bool operator <(TimeSimulation left, TimeSimulation right)
    {
        return new DateTime(left.year, left.month, left.day, left.hour, left.minute, left.second) <
            new DateTime(right.year, right.month, right.day, right.hour, right.minute, right.second);
    }

    public static bool operator <=(TimeSimulation left, TimeSimulation right)
    {
        return new DateTime(left.year, left.month, left.day, left.hour, left.minute, left.second) <=
            new DateTime(right.year, right.month, right.day, right.hour, right.minute, right.second);
    }

    public static bool operator ==(TimeSimulation left, TimeSimulation right)
    {
        return new DateTime(left.year, left.month, left.day, left.hour, left.minute, left.second) ==
            new DateTime(right.year, right.month, right.day, right.hour, right.minute, right.second);
    }

    public static bool operator !=(TimeSimulation left, TimeSimulation right)
    {
        return new DateTime(left.year, left.month, left.day, left.hour, left.minute, left.second) !=
            new DateTime(right.year, right.month, right.day, right.hour, right.minute, right.second);
    }

    public TimeSimulation AddSeconds(int seconds)
    {
        DateTime time = new DateTime(this.year, this.month, this.day, this.hour, this.minute, this.second);
        time = time.AddSeconds(seconds);
        this.year = time.Year;
        this.month = time.Month;
        this.day = time.Day;
        this.hour = time.Hour;
        this.minute = time.Minute;
        this.second = time.Second;
        this.week = new DateTime(year, month, day, hour, minute, second).DayOfWeek;
        return this;
    }

    public static int operator - (TimeSimulation left, TimeSimulation right)
    {
        return (int)(new DateTime(left.year, left.month, left.day, left.hour, left.minute, left.second) -
            new DateTime(right.year, right.month, right.day, right.hour, right.minute, right.second)).TotalSeconds;
    }

    public string TextForm(bool includeYear)
    {
        if (includeYear)
        {
            return $"{this.year}/{this.month}/{this.day} " + 
                (this.hour >= 10 ? $"{this.hour}:" : $"0{this.hour}:") +
                (this.minute >= 10 ? $"{this.minute}:" : $"0{this.minute}:") +
                (this.second >= 10 ? $"{this.second}" : $"0{this.second}");
        }
        else
        {
            return (this.hour >= 10 ? $"{this.hour}:" : $"0{this.hour}:") +
                (this.minute >= 10 ? $"{this.minute}:" : $"0{this.minute}:") +
                (this.second >= 10 ? $"{this.second}" : $"0{this.second}");
        }
    }
}


[Serializable]
public class OperateTime
{
    public int startHourWeekday;
    public int startMinuteWeekday;
    public int endHourWeekday;
    public int endMinuteWeekday;
    public int startHourWeekend;
    public int startMinuteWeekend;
    public int endHourWeekend;
    public int endMinuteWeekend;

    public bool Contains(TimeSimulation time)
    {
        TimeSimulation startTimeWeekday = new TimeSimulation(time.year, time.month, time.day, startHourWeekday, startMinuteWeekday, 0);
        TimeSimulation endTimeWeekday = new TimeSimulation(time.year, time.month, time.day, endHourWeekday, endMinuteWeekday, 0);
        if (endTimeWeekday <= startTimeWeekday)
        {
            endTimeWeekday.AddSeconds(24 * 3600);
        }
        TimeSimulation startTimeWeekend = new TimeSimulation(time.year, time.month, time.day, startHourWeekend, startMinuteWeekend, 0);
        TimeSimulation endTimeWeekend = new TimeSimulation(time.year, time.month, time.day, endHourWeekend, endMinuteWeekend, 0);
        if (endTimeWeekend <= startTimeWeekend)
        {
            endTimeWeekend.AddSeconds(24 * 3600);
        }
        if (time.week == DayOfWeek.Saturday || time.week == DayOfWeek.Sunday)
        {
            if (time >= startTimeWeekend && time <= endTimeWeekend)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (time >= startTimeWeekday && time <= endTimeWeekday)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //if (dateTime.Date.DayOfWeek == DayOfWeek.Monday)
        //{
        //    bool flag0 = false;
        //    bool flag1 = false;
        //    if (endHourWeekend >= 0)
        //    {
        //        DateTime startDateTime0 = new DateTime(time.year, time.month, time.day, 0, 0, 0);
        //        DateTime endDateTime0 = new DateTime(time.year, time.month, time.day, endHourWeekend, endMinuteWeekend, 0);
        //        flag0 = (dateTime >= startDateTime0 && dateTime <= endDateTime0);
        //    }
        //    if (endHourWeekday >= 0)
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekday, startMinuteWeekday, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day + 1, 0, 0, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    else
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekday, startMinuteWeekday, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day, endHourWeekday, endMinuteWeekday, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    return flag0 || flag1;
        //}
        //else if (dateTime.Date.DayOfWeek == DayOfWeek.Saturday)
        //{
        //    bool flag0 = false;
        //    bool flag1 = false;
        //    if (endHourWeekday >= 0)
        //    {
        //        DateTime startDateTime0 = new DateTime(time.year, time.month, time.day, 0, 0, 0);
        //        DateTime endDateTime0 = new DateTime(time.year, time.month, time.day, endHourWeekday, endHourWeekday, 0);
        //        flag0 = (dateTime >= startDateTime0 && dateTime <= endDateTime0);
        //    }
        //    if (endHourWeekend >= 0)
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekend, startMinuteWeekend, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day + 1, 0, 0, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    else
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekend, startMinuteWeekend, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day, endHourWeekend, endMinuteWeekend, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    return flag0 || flag1;
        //}
        //else if (dateTime.Date.DayOfWeek == DayOfWeek.Sunday)
        //{
        //    bool flag0 = false;
        //    bool flag1 = false;
        //    if (endHourWeekend >= 0)
        //    {
        //        DateTime startDateTime0 = new DateTime(time.year, time.month, time.day, 0, 0, 0);
        //        DateTime endDateTime0 = new DateTime(time.year, time.month, time.day, endHourWeekend, endMinuteWeekend, 0);
        //        flag0 = (dateTime >= startDateTime0 && dateTime <= endDateTime0);
        //    }
        //    if (endHourWeekend >= 0)
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekend, startMinuteWeekend, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day + 1, 0, 0, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    else
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekend, startMinuteWeekend, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day, endHourWeekend, endMinuteWeekend, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    return flag0 || flag1;
        //}
        //else
        //{
        //    bool flag0 = false;
        //    bool flag1 = false;
        //    if (endHourWeekday >= 0)
        //    {
        //        DateTime startDateTime0 = new DateTime(time.year, time.month, time.day, 0, 0, 0);
        //        DateTime endDateTime0 = new DateTime(time.year, time.month, time.day, endHourWeekday, endMinuteWeekday, 0);
        //        flag0 = (dateTime >= startDateTime0 && dateTime <= endDateTime0);
        //    }
        //    if (endHourWeekday >= 0)
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekday, startMinuteWeekday, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day + 1, 0, 0, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    else
        //    {
        //        DateTime startDateTime1 = new DateTime(time.year, time.month, time.day, startHourWeekday, startMinuteWeekday, 0);
        //        DateTime endDateTime1 = new DateTime(time.year, time.month, time.day, endHourWeekday, endMinuteWeekday, 0);
        //        flag1 = (dateTime >= startDateTime1 && dateTime <= endDateTime1);
        //    }
        //    return flag0 || flag1;
        //}
    }
}
