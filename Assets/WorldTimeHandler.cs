using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class TimeSetting {
    public string name;
    [Range(0, 1)]
    public float timeOccurrence = 0f;
    public Color ambientLightColor;
}

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(LightController))]
public class WorldTimeHandler : MonoBehaviour
{
    WorldController wCon;
    LightController lCon;

    public float dayLengthInMinutes;
    float dayLengthInSeconds;
    float timeScale = 1f;

    public List<TimeSetting> timeSettings = new List<TimeSetting>();

    float elapsedTime = 0f;
    int nextTimeSetting = 0;

    void Start()
    {
        wCon = GetComponent<WorldController>();
        lCon = GetComponent<LightController>();

        CheckTimeSettings();
        dayLengthInSeconds = dayLengthInMinutes * 60f;
    }

    void SortTimeSettings() {
        List<TimeSetting> tempList = new List<TimeSetting>();
        for (int sortIteration = 0; sortIteration < timeSettings.Count; sortIteration++) {
            
            float minTimeOccurrence = 2;
            int minIndex = 0;
            int curIndex = 0;

            foreach (TimeSetting ts in timeSettings) {
                if (ts.timeOccurrence < minTimeOccurrence) {
                    minTimeOccurrence = ts.timeOccurrence;
                    minIndex = curIndex;
                }
                curIndex++;
            }

            tempList.Add(timeSettings[minIndex]);
            timeSettings.RemoveAt(minIndex);
        }

        timeSettings = tempList;
    }

    void CheckTimeSettings() {
        List<float> occupiedTimes = new List<float>();
        foreach (TimeSetting ts in timeSettings) {
            if (occupiedTimes.Contains(ts.timeOccurrence)) {
                Debug.Log("Time setting '" + ts.name + "' has a duplicate time to another time setting! Removing from time settings!");
                timeSettings.Remove(ts);
            }
        }

        //If we have no time settings or just one, don't increase elapsed time, just set to existing time setting
        if (timeSettings.Count <= 1) {
            timeScale = 0;
            if (timeSettings.Count == 1) {
                UseTimeSetting(0);
            }
        } 
        //Otherwise sort the time settings by time occurrence
        else {
            SortTimeSettings();
        }
    }

    void Update()
    {
        if (timeScale == 0)
            return;

        elapsedTime = (elapsedTime + (Time.deltaTime * timeScale)) % dayLengthInSeconds;

        //Restart day
        if (nextTimeSetting == 0) {
            if (elapsedTime < timeSettings[timeSettings.Count-1].timeOccurrence * dayLengthInSeconds) {
                UseTimeSetting(nextTimeSetting);
                nextTimeSetting = (nextTimeSetting + 1) % timeSettings.Count;
            }
        }

        //Continue to next time setting
        if (elapsedTime > timeSettings[nextTimeSetting].timeOccurrence * dayLengthInSeconds) {
            UseTimeSetting(nextTimeSetting);
            nextTimeSetting = (nextTimeSetting + 1) % timeSettings.Count;
        }
    }

    void UseTimeSetting(int timeSettingIndex) {
        
    }
}
