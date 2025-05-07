using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Station : MonoBehaviour
{
    public Dictionary<Line, List<Passenger>> passengers;
    public Dictionary<GameObject, List<Line>> lines;
    public Dictionary<GameObject, int> passengersInLineCount;
    public int passengersCount;
    public List<Line> linesIsOperating;
    public bool isTransferStation;
    public bool ableTransfer;
    public OperateTime operateTime;
    public bool isOperating;
    public int goInTime;
    public int goOutTime;
    public int transferTime;

    public Station()
    {
        this.passengers = new Dictionary<Line, List <Passenger>>();
    }

    void Start()
    {

    }

    void Update()
    {
    }

    public int NextTrainid(TimeSimulation time, Line line)
    {
        TimeSimulation minTime = new TimeSimulation(time).AddSeconds(24 * 3600);
        int mini = -1;
        for(int i = 0; i < line.trains.Count; i++)
        {
            if (line.trains[i].arriveTime.ContainsKey(this))
            {
                if (minTime > line.trains[i].arriveTime[this] && time < line.trains[i].arriveTime[this])
                {
                    minTime = line.trains[i].arriveTime[this];
                    mini = i;
                }
            }
        }
        return mini;
    }
}
