using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Train : MonoBehaviour
{
    public List<Passenger> passengers;
    public List<Passenger> passengersInVehicle;
    public int capacity;
    public Line line;
    public Station startStation;
    public Station endStation;
    public Dictionary<Station, int> dwellTime;
    public Dictionary<Station, TimeSimulation> arriveTime;
    public Dictionary<Station, TimeSimulation> departTime;
    public int nextStationid;
    public int nextStationGoOff;
    public int nextStationGoOn;
    public bool isDwell;
    private int starti;
    private int endi;
    public string week;
    public GameObject trainObject;

    public void CreateTrain(int capacity, Line line, Station startStation, Station endStation, TimeSimulation startTime, string week)
    {
        this.passengers = new List<Passenger>();
        this.passengersInVehicle = new List<Passenger>();
        this.capacity = capacity;
        this.line = line;
        this.startStation = startStation;
        this.endStation = endStation;
        this.dwellTime = new Dictionary<Station, int>();
        this.arriveTime = new Dictionary<Station, TimeSimulation>();
        this.departTime = new Dictionary<Station, TimeSimulation>();
        this.week = week;

        starti = line.stations.IndexOf(startStation);
        endi = line.stations.IndexOf(endStation);
        int intervalTime = 0;
        for(int i = starti; i <= endi; i++)
        {
            dwellTime[line.stations[i]] = line.dwellTime[i];
            if (i == starti)
            {
                arriveTime.Add(line.stations[i], new TimeSimulation(startTime));
            }
            else
            {
                arriveTime.Add(line.stations[i], new TimeSimulation(startTime).AddSeconds(intervalTime));
            }
            intervalTime += line.dwellTime[i];
            departTime.Add(line.stations[i], new TimeSimulation(startTime).AddSeconds(intervalTime));
            if (i != endi)
            {
                intervalTime += line.runTime[i];
            }
        }
        line.trains.Add(this);
    }

    public int NextStationid(TimeSimulation time)
    {
        for (int i = starti; i <= endi; i++)
        {
            if (time < arriveTime[line.stations[i]])
            {
                if (line.stations[i] == startStation)
                {
                    Debug.LogError("NextStation is wrong");
                }
                return i;
            }
        }
        return endi + 1;
    }

    void Start()
    {
        
    }

    void Update()
    {

    }

    public override int GetHashCode()
    {
        TimeSimulation arriveStart = new TimeSimulation(arriveTime[startStation]);
        return line.name.GetHashCode() + startStation.name.GetHashCode() 
            + arriveStart.day * 24 * 3600 + arriveStart.hour * 3600 + arriveStart.minute * 60 + arriveStart.second;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        Train otherTrain = (Train)obj;

        return otherTrain.line.name == this.line.name && otherTrain.startStation.name == this.startStation.name 
            && otherTrain.arriveTime[startStation] == this.arriveTime[startStation];
    }
}
