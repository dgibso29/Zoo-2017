using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalData {

    /// <summary>
    /// Used to identify animal type, life stage, and gender ("a_tigris_sumatrae_adult_M")
    /// </summary>
    string animalID;
    /// <summary>
    /// Common name of species ("Sumatran Tiger")
    /// </summary>
    string speciesName;
    /// <summary>
    /// Scientific name of species ("Panthera tigris sumatrae")
    /// </summary>
    string scientificName;
    /// <summary>
    /// Animal's shown name ("Sumatran Tiger 1")
    /// </summary>
    string animalName;
    /// <summary>
    /// ID number unique to this animal ("132")
    /// </summary>
    int uniqueID;

    // Animal Needs
    float hunger;
    float thirst;
    float happiness;
    float enrichment;
    /// <summary>
    /// Habitat suitability
    /// </summary>
    float habitat;
    float health;

    public AnimalData(string animalID, string speciesName, string scientificName, string animalName)
    {
        this.animalID = animalID;
        this.speciesName = speciesName;
        this.scientificName = scientificName;
        this.animalName = animalName;

        GenerateUniqueID();
    }

    void GenerateUniqueID()
    {
        uniqueID = ZooInfo.totalAnimalsSpawned;
        ZooInfo.totalAnimalsSpawned++;
    }

}
