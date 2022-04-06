/***********************************************************************************************************************
* Random Search Algorithm                                                                                              *
* By Rafael F. Ropelato                                                                                                *
* University of Maryland - Institute for Systems Research                                                              *
* Part of MSc. Thesis "Generating Feasible Spawn Locations for Autonomous Robot Simulations in Complex Environments"   *
*                                                                                                                      *
************************************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

[ExecuteInEditMode] // run script in edit mode

public class RandomSearch : MonoBehaviour
{
    [Header("Attach Robot Model")]  // Model to check spawns for
    public GameObject robotModel = null;
    
    [Header("Settings - Spawn Generator")] // Variables for Spawn Generator 
    public Vector3 limitLow = new Vector3(-10, -10, -10);   // Search perimeter lower limit
    public Vector3 limitHigh = new Vector3(10, 10, 10);     // Search perimeter higher limit
    public float checkHeight = 1f;          // Max height above Terrain
    public float maxWaterHeight = 100f;     // Max height of any water plane
    public string[] ignoredTags = { "Robot", "Terrain" };   // Tags that are not an obstacle
    public string waterTag = "Water";       // Water Tag
    public string terrainTag = "Terrain";   // Terrain Tag

    [Header("Search Parameters")] // Algorithm properties
    public int nrOfSpawnsToFind = 50;    // Nr of spawns to find
    public bool performDistanceCheck = true; // perform minimum distance check between assessed locations
    public float minDistance = 0.5f;      // minimum distance between checked locations
    public int limitFailedAttempts = 10000;     // Maximum consecutive failed checks (to avoid infinite search)   

    [Header("Select - Gizmos Drawn")] // Bools to draw Gizmos
    public bool drawSearchArea = false;
    public bool drawInvalid = false;
    public bool drawValid = false;
    public bool drawAir = false;

    [Header("Settings - Gizmos")] // Gizmo settings
    public float gizmoSize = 0.25f;
    public float gizmoAlpha = 1f;
    public bool gizmoSphere = false;
    public bool gizmoWire = false;  // Less lag

    [Header("Run Single Test Commands")] // Bools to run commands 
    public bool runTest = false;    // Execute test
    public bool destroySpawnList = false;  // Destroy existing results

    [Header("Write Results")] // Bools to run commands 
    public int testNr = 0;      // Test number, used for save folder name
    public enum Environment {   // Environment in which the test is performed
        trainingEnvironment,
        lowComplexEnvironment,
        highComplexEnvironment,
        benchmarkEnvironment
    }
    public Environment environmentName;
    public bool writeToFile = false;    // Start writing to file

    [Header("Run Multiple Searches")] // Bools to run commands 
    public int testNrMultirun = 0;      // Test number, used for save folder name
    public enum MultirunEnvironment {   // Environment in which the test is performed
        trainingEnvironment,
        lowComplexEnvironment,
        highComplexEnvironment,
        benchmarkEnvironment
    }
    public MultirunEnvironment multirunEnvironmentName;
    public int nrOfRuns = 1;       // Nr. of test runs
    public bool startMultirun = false;   // Execute multiple test runs

    // Counters
    private int nrOfValidSpawns = 0;
    private int nrOfInvalidSpawns = 0;
    private int nrOfAirSpawns = 0;
    private int nrOfCheckedPositions = 0;
    private int nrOfFailedAttempts;

    // Lists to save spawn locations (valid, invalid, or in the air)
    private List<Vector3> validSpawns = new List<Vector3>();
    private List<Vector3> invalidSpawns = new List<Vector3>();
    private List<Vector3> airSpawns = new List<Vector3>();
    private List<Vector3> checkedPositions = new List<Vector3>();
    
    // Result variables
    private List<float> foundTimes = new List<float>();
    private DateTime startTime = DateTime.Now;
    private DateTime runEndTime = DateTime.Now;
    private DateTime endTime = DateTime.Now;
    private DateTime currentTime = DateTime.Now;
    private TimeSpan timePassed;
    private TimeSpan runTime;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Run single test
        if (runTest) {
            runTest = false;        // Reset runTest
            Vector3 initialPos = robotModel.transform.position;     // Save Initial Robot Pos
            robotModel.transform.GetChild(0).transform.position = robotModel.transform.position;    // Set Mesh Object To Robot Position

            DeleteSpawnLists();     // Delete Old Spawn Lists
            
            startTime = DateTime.Now;
            checkedPositions.Add(limitHigh + new Vector3(0, minDistance, 0));   // Add position to already checked positions
            RunSpawnTests(limitLow, limitHigh, nrOfSpawnsToFind);   // Run Test Method
            endTime = DateTime.Now;

            robotModel.transform.position = initialPos; // Return Robot to Initial Position
        }

        // Run multiple test runs
        if (startMultirun) {
            string writeString = "";        // Empty write string for results

            startMultirun = false;

            // Create new directory with selected test nr and environment name
            string environmentFolder = "";
            switch (multirunEnvironmentName) {
                case MultirunEnvironment.highComplexEnvironment:
                    environmentFolder = "HighComplexEnvironment";
                    break;
                case MultirunEnvironment.trainingEnvironment:
                    environmentFolder = "TrainingEnvironment";
                    break;
                case MultirunEnvironment.lowComplexEnvironment:
                    environmentFolder = "LowComplexEnvironment";
                    break;
            }
            string path = "Assets/Results/Random_Search/" + environmentFolder + "/Test_" + testNrMultirun;
            Directory.CreateDirectory(path); // returns a DirectoryInfo object


            Vector3 initialPos = robotModel.transform.position;     // Save Initial Robot Pos
            robotModel.transform.GetChild(0).transform.position = robotModel.transform.position;    // Set Mesh Object To Robot Position

            // Clear existing results file
            StreamWriter timeWriter1 = new StreamWriter(path + "/Time_Stamps.txt", false);
            timeWriter1.Write("");
            timeWriter1.Close();

            StreamWriter timeWriter = new StreamWriter(path + "/Time_Stamps.txt");  // Writer for all time stamps
            StreamWriter runTimeWriter = new StreamWriter(path + "/RunTime.txt", false);    // Writer for run times

            // Run number of test runs
            for (int i = 0; i < nrOfRuns; i++) {
                DeleteSpawnLists();     // Delete Old Spawn Lists
                checkedPositions.Add(limitHigh + new Vector3(0, minDistance, 0));   // add checked position

                startTime = DateTime.Now;   // Start time
                RunSpawnTests(limitLow, limitHigh, nrOfSpawnsToFind);   // Run Test Method
                runEndTime = DateTime.Now;  // End time
                runTime = runEndTime - startTime;   // Run time

                // Write complete run time to file
                runTimeWriter.WriteLine(60f * runTime.Minutes + runTime.Seconds + 0.001f * runTime.Milliseconds);
                
                // Write all iime stamps to file
                foreach (float time in foundTimes) {
                    writeString = writeString + time + ";";

                }
                timeWriter.WriteLine(writeString);

                writeString = "";   // Reset time stamps string


            }
            runTimeWriter.Close();  // Close file writers
            timeWriter.Close();
            endTime = DateTime.Now;

        }

        // Destroy 
        if (destroySpawnList) {
            destroySpawnList = false;
            DeleteSpawnLists();
        }

        // Write to file
        if (writeToFile) {
            writeToFile = false;
            WriteToFile();
        }
    }

    // Run random search
    void RunSpawnTests(Vector3 lowLimit, Vector3 highLimit, int N) {
        int locationsFound = 0; // Nr of locations found
        Vector3 positionToCheck;    // current position to check

        int indexL = 0; // left index for binary list search
        int indexR = 0; // right index for binary list search

        // Counters
        nrOfValidSpawns = 0;    
        nrOfInvalidSpawns = 0;
        nrOfAirSpawns = 0;
        nrOfCheckedPositions = 0;
        nrOfFailedAttempts = 0;

        bool tooClose = false;  // too close to already checked
        
        FeasibilityCheck feasibilityComponent = robotModel.GetComponentInChildren<FeasibilityCheck>();  // Get feasibility check component from robot

        while (locationsFound < N & nrOfFailedAttempts < limitFailedAttempts) {     // Until required nr. of locations found OR max failed attempts

            // Current pos to check
            positionToCheck = new Vector3(UnityEngine.Random.Range(lowLimit.x, highLimit.x), UnityEngine.Random.Range(lowLimit.y, highLimit.y), UnityEngine.Random.Range(lowLimit.z, highLimit.z));

            // Binary search already checked locations
            if (performDistanceCheck) {
                indexL = BinarySearchX(checkedPositions, positionToCheck - new Vector3(minDistance, 0, 0));
                indexR = BinarySearchX(checkedPositions, positionToCheck + new Vector3(minDistance, 0, 0));
                tooClose = false;

                // Check if current pos too close to any already checked pos
                for (int i = indexL; i <= indexR; i++) {
                    if (Vector3.Distance(checkedPositions[i], positionToCheck) < minDistance) {
                        tooClose = true;
                        nrOfFailedAttempts++;
                        nrOfCheckedPositions++;
                    }
                }
            }


            

            if (!tooClose || !performDistanceCheck) {
                if (feasibilityComponent.AerialTest(positionToCheck, checkHeight, ignoredTags)) {       // Aerial check
                    if (!feasibilityComponent.WaterCheck(positionToCheck, maxWaterHeight, waterTag)) {  // Water Check

                        robotModel.transform.position = positionToCheck;
                        if (!feasibilityComponent.CollisionCheck()) {       // Collision Check
                            validSpawns.Add(positionToCheck);   // Add to valid
                            if (performDistanceCheck) {
                                insertSortedX(checkedPositions, positionToCheck);   // Insert to sorted to checked list
                            }
                            nrOfValidSpawns++;  // count valid
                            locationsFound++;   // count found
                            nrOfFailedAttempts = 0; // reset nr of failed attempts
                            nrOfCheckedPositions++; // count checked position

                            currentTime = DateTime.Now; // current time (for time stamps)
                            timePassed = currentTime - startTime;   // time stamp for results
                            foundTimes.Add(3600f * timePassed.Hours + 60f * timePassed.Minutes + timePassed.Seconds + 0.001f * timePassed.Milliseconds);
                        }
                        else {
                            invalidSpawns.Add(positionToCheck); // Add to invalid
                            if (performDistanceCheck) {
                                insertSortedX(checkedPositions, positionToCheck);   // Insert to sorted to checked list
                            }
                            nrOfInvalidSpawns++;    // count invalid
                            nrOfFailedAttempts++;   // count failed attempt
                            nrOfCheckedPositions++; // count checked position
                        }
                    }
                    else {
                        invalidSpawns.Add(positionToCheck); // Add to invalid
                        if (performDistanceCheck) {
                            insertSortedX(checkedPositions, positionToCheck);   // Insert to sorted to checked list
                        }
                        nrOfInvalidSpawns++;    // count invalid
                        nrOfFailedAttempts++;   // count failed attempt
                        nrOfCheckedPositions++; // count checked position
                    }
                }
                else {
                    airSpawns.Add(positionToCheck); // Add to air
                    if (performDistanceCheck) {
                        insertSortedX(checkedPositions, positionToCheck);   // Insert to sorted to checked list
                    }
                    nrOfAirSpawns++;        // count air
                    nrOfFailedAttempts++;   // count failed attempt
                    nrOfCheckedPositions++; // count checked position
                }
            }
        }
    }

    // Write results of single test to file
    void WriteToFile() {
        
        // Create new directory with selected test nr and environment name
        string environmentFolder = "";
        switch (environmentName) {
            case Environment.highComplexEnvironment:
                environmentFolder = "HighComplexEnvironment";
                break;
            case Environment.trainingEnvironment:
                environmentFolder = "TrainingEnvironment";
                break;
            case Environment.lowComplexEnvironment:
                environmentFolder = "LowComplexEnvironment";
                break;
        }
        string path = "Assets/Results/Random_Search/" + environmentFolder + "/Test_" + testNr;
        Directory.CreateDirectory(path); // returns a DirectoryInfo object

        // Write Valid Spawn Locations
        StreamWriter validWriter = new StreamWriter(path + "/Valid_Locations.txt", false);
        foreach (Vector3 location in validSpawns) {
            validWriter.WriteLine(location.x + ", " + location.y + ", " + location.z);
        }
        validWriter.Close();

        // Write Invalid Spawn Locations
        StreamWriter invalidWriter = new StreamWriter(path + "/Invalid_Locations.txt", false);
        foreach (Vector3 location in invalidSpawns) {
            invalidWriter.WriteLine(location.x + ", " + location.y + ", " + location.z);
        }
        invalidWriter.Close();

        // Write Air Spawn Locations
        StreamWriter airWriter = new StreamWriter(path + "/Air_Locations.txt", false);
        foreach (Vector3 location in airSpawns) {
            airWriter.WriteLine(location.x + ", " + location.y + ", " + location.z);
        }
        airWriter.Close();

        // Write Time Stamps
        StreamWriter timeWriter = new StreamWriter(path + "/Time_Stamps.txt", false);
        foreach (float time in foundTimes) {
            timeWriter.WriteLine(time);
        }
        timeWriter.Close();

        // Write Stats
        StreamWriter statsWriter = new StreamWriter(path + "/Stats.txt", false);
        statsWriter.WriteLine("Area Searched From: " + limitLow + "meter to: " + limitHigh + "meter");
        statsWriter.WriteLine("Minimum Distance between Locations: " + minDistance + " meter");
        statsWriter.WriteLine("Abort after maximum of " + limitFailedAttempts + " failed attempts");
        statsWriter.WriteLine("Number of Checked Locations: " + nrOfCheckedPositions);
        statsWriter.WriteLine("");
        statsWriter.WriteLine("Nr. of VALID Spawns = " + validSpawns.Count);
        statsWriter.WriteLine("Nr. of INVALID Spawns = " + invalidSpawns.Count);
        statsWriter.WriteLine("Nr. of AIR Spawns = " + airSpawns.Count);
        statsWriter.WriteLine("");
        statsWriter.WriteLine("Spawn found: " + validSpawns.Count + " / " + nrOfSpawnsToFind);
        TimeSpan difference = endTime - startTime;
        statsWriter.WriteLine("Time for Calculations = " + difference);
        statsWriter.Close();
    }

    // Binary search algorithm
    int BinarySearchX(List<Vector3> list, Vector3 element) {
        int leftLim = 0;
        int rightLim = list.Count - 1;
        int middleElement = 0;


        while (leftLim <= rightLim) {
            middleElement = Mathf.FloorToInt((leftLim + rightLim) / 2f);
            if (list[middleElement].x < element.x) {
                leftLim = middleElement + 1;
            }
            else if (list[middleElement].x > element.x) {
                rightLim = middleElement - 1;
            }
            else {
                return middleElement;
            }
        }
        return middleElement;
    }

    // Insert Vector3 sorted by X coordinate
    void insertSortedX(List<Vector3> list, Vector3 position) {
        list.Insert(BinarySearchX(list, position), position);
    }
    
    // Delete result lists
    void DeleteSpawnLists() {
        validSpawns.Clear();
        invalidSpawns.Clear();
        airSpawns.Clear();
        checkedPositions.Clear();
        foundTimes.Clear();
    }

    // Draw Gizmos (markers) of spawns
    private void OnDrawGizmos() {
        
        // Draw search perimeter
        if (drawSearchArea) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3((limitHigh.x - limitLow.x) / 2 + limitLow.x, (limitHigh.y - limitLow.y) / 2 + limitLow.y, (limitHigh.z - limitLow.z) / 2 + limitLow.z), new Vector3((limitHigh.x - limitLow.x), (limitHigh.y - limitLow.y), (limitHigh.z - limitLow.z)));
        }

        // Draw invalid spawns (red)
        if (drawInvalid) {
            Gizmos.color = new Color(1f, 0f, 0f, gizmoAlpha);
            foreach (Vector3 indicatorPos in invalidSpawns) {
                if (gizmoSphere) {
                    if (gizmoWire) {
                        Gizmos.DrawWireSphere(indicatorPos, gizmoSize);
                    }
                    else {
                        Gizmos.DrawSphere(indicatorPos, gizmoSize);
                    }

                }
                else {
                    if (gizmoWire) {
                        Gizmos.DrawWireCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                    else {
                        Gizmos.DrawCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                }
            }
        }

        // Draw valid spawns (green)
        if (drawValid) {
            Gizmos.color = new Color(0f, 1f, 0f, gizmoAlpha);
            foreach (Vector3 indicatorPos in validSpawns) {
                if (gizmoSphere) {
                    if (gizmoWire) {
                        Gizmos.DrawWireSphere(indicatorPos, gizmoSize);
                    }
                    else {
                        Gizmos.DrawSphere(indicatorPos, gizmoSize);
                    }

                }
                else {
                    if (gizmoWire) {
                        Gizmos.DrawWireCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                    else {
                        Gizmos.DrawCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                }
            }
        }

        // Draw aerial spawns (blue)
        if (drawAir) {
            Gizmos.color = new Color(0f, 0f, 1f, gizmoAlpha);
            foreach (Vector3 indicatorPos in airSpawns) {
                if (gizmoSphere) {
                    if (gizmoWire) {
                        Gizmos.DrawWireSphere(indicatorPos, gizmoSize);
                    }
                    else {
                        Gizmos.DrawSphere(indicatorPos, gizmoSize);
                    }

                }
                else {
                    if (gizmoWire) {
                        Gizmos.DrawWireCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                    else {
                        Gizmos.DrawCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                }
            }
        }

    }
}
