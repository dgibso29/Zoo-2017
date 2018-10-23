using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuestData {

    /// <summary>
    /// Name of guest
    /// </summary>
    [SerializeField]
    string name;
    /// <summary>
    /// Numerical guest ID
    /// </summary>
    [SerializeField]
    int id;

    public Vector3 position;


    public GuestData(Vector3 position)
    {
        id = ZooInfo.totalGuestsSpawned;
        ZooInfo.totalGuestsSpawned++;
        name = "Guest " + id;
        this.position = position;
    }

    public void SetDataOnLoad(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    public int GuestID
    {
        get { return id; }
    }

    public string GuestName
    {
        get { return name; }
        set { name = value; }
    }

}
