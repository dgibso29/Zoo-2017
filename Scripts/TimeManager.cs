using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TimeManager : MonoBehaviour {

    public float dayLength; // Length of each In-Game day in seconds

    private float currentDayLength; // Used to modify time per day in seconds
    private DateTime date = new DateTime(1, 1, 1);
    private string formattedDate;
    private bool gameIsRunning = true;
    private bool paused = false;

    private bool twoTimesSpeed = false;
    private bool threeTimesSpeed = false;
    private bool fourTimesSpeed = false;


    // Use this for initialization
    void Start () {
        StartCoroutine(InGameDate());
        currentDayLength = dayLength;
	}
	
	// Update is called once per frame
	void Update ()
    {
        UpdateTimeScale();

        //if (Input.GetMouseButtonDown(0))
        //{
        //    Debug.Log(currentDayLength);
        //    Debug.Log(Time.timeScale);
        //}
    }
    /// <summary>
    /// Manages the progression of time in the game.
    /// </summary>
    /// <returns></returns>
    IEnumerator InGameDate()
    {
        yield return new WaitForSecondsRealtime(currentDayLength);
        while (gameIsRunning)
        {
            while (!paused)
            {
                formattedDate = string.Format(new MyCustomDateProvider(), "{0}", date);
                yield return new WaitForSecondsRealtime(currentDayLength);
                AddDay();
            }
            while (paused)
            {
                yield return null;
            }
        }
    }

    public string GetCurrentDate
    {
       get { return formattedDate; }
    }
    public DateTime GetRawDate
    {
        get { return date; }
    }

    public void SetTimeFromSave(DateTime newDate)
    {
        date = newDate;
        formattedDate = string.Format(new MyCustomDateProvider(), "{0}", date);
    }

    public void AddDay()
    {
        date = date.AddDays(1);
    }

    public override string ToString()
    {
        formattedDate = string.Format(new MyCustomDateProvider(), "{0}", date);
        return formattedDate;
    }

    void UpdateTimeScale()
    {
        if (twoTimesSpeed)
        {
            currentDayLength = dayLength * 2;
            Time.timeScale = 2;
        }
        else if (threeTimesSpeed)
        {
            currentDayLength = dayLength * 3;
            Time.timeScale = 3;
        }
        else if (fourTimesSpeed)
        {
            currentDayLength = dayLength * 4;
            Time.timeScale = 4;
        }
        else
        {
            currentDayLength = dayLength;
            Time.timeScale = 1;
        }
    }

    public bool TwoTimesSpeed
    {
        get { return twoTimesSpeed; }
        set { TwoTimesSpeed = value; }
    }
    
    public bool ThreeTimesSpeed
    {
        get { return threeTimesSpeed; }
        set { threeTimesSpeed = value; }
    }

    public bool FourTimesSpeed
    {
        get { return fourTimesSpeed; }
        set { fourTimesSpeed = value; }
    }

    public void PauseGame()
    {
        if (!paused)
        {
            paused = true;
            Time.timeScale = 0;
        }
        else if (paused)
        {
            paused = false;
            Time.timeScale = 1;
        }
    }

}
