using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour {

    public GameObject uiCanvas;
    public Text cashText;
    public Text zooNameUIText;
    public Text dateUIText;

    public ZooInfo zooInfo;
    public TimeManager timeManager;
    public EconomyTools economy;
    public World world;
    SaveManager saveManager;

    // Use this for initialization
    void Awake ()
    {
        saveManager = SaveManager.instance;
        Debug.Log("Game Manager Loaded");
	}
	
	// Update is called once per frame
	void Update () {

        UpdateDateUI();
        UpdateCashText();
        UpdateZooNameUI();

    }

    public void UpdateZooNameUI()
    {
        if(zooNameUIText.text != zooInfo.ZooName)
        {
            zooNameUIText.text = zooInfo.ZooName;
        }
    }

    void UpdateCashText()
    {
        cashText.text = "Cash: $" + economy.CurrentFunds;
    }

    void UpdateDateUI()
    {
        dateUIText.text = "Date: " + timeManager.GetCurrentDate;
    }

    public void SaveGame(string saveName)
    {
        saveManager.SaveGame(saveName);
    }

    public void LoadGame(string saveToLoad)
    {
        StartCoroutine(saveManager.LoadGame(saveToLoad));
        //saveManager.LoadGame(saveToLoad);
    }

}

