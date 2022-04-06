/***********************************************************************************************************************
* 2D Search Algorithm                                                                                                  *
* By Rafael F. Ropelato                                                                                                *
* University of Maryland - Institute for Systems Research                                                              *
* Part of MSc. Thesis "Generating Feasible Spawn Locations for Autonomous Robot Simulations in Complex Environments"   *
*                                                                                                                      *
************************************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

[ExecuteInEditMode] // run script in edit mode

public class Search2D : MonoBehaviour
{
    [Header("Attach Robot Model")] // Model to check spawns for
    public GameObject robotModel = null;

    [Header("Settings - Spawn Generator")] // Variables for Spawn Generator 
    public Vector2 limitLow = new Vector2(-10, -10);        // Search perimeter lower limit
    public Vector2 limitHigh = new Vector2(10, 10);   // Search perimeter upper limit
    public float rayPlaneHeight = 10f;     // Ray max height (max search height)
    public float rayCastDepth = -10f;      // Ray max depth (max search depth)
    public float stepSizeXZ = 0.5f;           // Step size (in XZ plane)
    public float yOffset = 0.5f;            // Y Offset from terrain for feasibility check
    public string[] ignoredTags = { "Robot", "Terrain" };   // Tags that are not an obstacle
    public string waterTag = "Water";       // Water Tag
    public string terrainTag = "Terrain";   // Terrain Tag

    [Header("Select - Gizmos Drawn")] // Bools to draw Gizmos
    public bool drawSearchArea = false;
    public bool drawInvalid = false;
    public bool drawValid = false;

    [Header("Settings - Gizmos")] // Gizmo settings
    public float gizmoSize = 0.25f;
    public float gizmoAlpha = 1f;
    public bool gizmoSphere = false;
    public bool gizmoWire = false;  // Less lag

    [Header("Run Single Test Commands")] // Bools to run commands 
    public bool runTest = false;            // Execute test
    public bool destroySpawnLists = false;  // Destroy existing results

    [Header("Write Results")] // Bools to run commands 
    public int testNr = 0;      // Test number, used for save folder name
    public enum Environment {   // Environment in which the test is performed
        trainingEnvironment,
        lowComplexEnvironment,
        highComplexEnvironment
    }
    public Environment environmentName;
    public bool writeToFile = false;    // Start writing to file

    [Header("Run Multiple Searches")] // Bools to run commands 
    public int multiRunTestNr = 0;      // Test number, used for save folder name
    public enum MultiRunEnvironment {   // Environment in which the test is performed
        trainingEnvironment,
        lowComplexEnvironment,
        highComplexEnvironment
    }
    public MultiRunEnvironment multiRunEnvironmentName;
    public int N = 1;                   // Nr. of test runs
    public float minStepSize = 0.1f;    // Linear step size increment lower limit
    public float maxStepSize = 1f;      // Linear step size increment upper limit
    public bool runMultipleTests = false;   // Execute multiple test runs


    private Vector2 positionToCheck;    // Current Position to Check

    // Lists to save spawn locations (valid, or invalid)
    private List<Vector3> validSpawns = new List<Vector3>();
    private List<Vector3> invalidSpawns = new List<Vector3>();

    // Counter
    private int nrOfInvalidSpawns = 0;
    private int nrOfValidSpawns = 0;

    // Time variables
    private DateTime startTime;
    private DateTime endTime;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Run single test
        if (runTest) {
            runTest = false;

            Vector3 initialPos = robotModel.transform.position;     // Save initial robot position
            robotModel.transform.GetChild(0).transform.position = robotModel.transform.position;    // Set mesh object to robot position
            DeleteSpawnLists();     // Delete old Indicators

            // Reset Counters
            nrOfInvalidSpawns = 0;
            nrOfValidSpawns = 0;

            startTime = DateTime.Now;
            RunSpawnTests(limitLow, limitHigh, stepSizeXZ); // Run test method
            endTime = DateTime.Now;

            robotModel.transform.position = initialPos;     // Reset robot position
        }

        // Execute functions when bools are set
        // Run multiple tests
        if (runMultipleTests) {
            runMultipleTests = false;
            multipleRuns();
        }

        // Destroy 
        if (destroySpawnLists) {
            destroySpawnLists = false;
            DeleteSpawnLists();
        }

        // Write to file
        if (writeToFile) {
            writeToFile = false;
            WriteToFile();
        }
    }

    // Run spawn check
    TimeSpan RunSpawnTests(Vector2 lowLimit, Vector2 highLimit, float stepSizePlane) {
        DateTime runStartTime = DateTime.Now;       // Start time

        FeasibilityCheck feasibilityComponent = robotModel.GetComponentInChildren<FeasibilityCheck>();  // Get feasibility check component from robot

        bool waterHit = false;
        float waterHitHeight = 0f;  // height of water plane hit
        List<float> terrainHeights = new List<float>(); // heights of terrain hit (multiple hits possible)
        RaycastHit[] hitAll;    // Raycast constructor
        Vector3 positionForCollisionCheck;  // Calculated position for feasibility check
        
        // Iterate through 2 Dimensions
        for (float x_pos = lowLimit.x; x_pos <= highLimit.x; x_pos += stepSizePlane) {
            for (float y_pos = lowLimit.y; y_pos <= highLimit.y; y_pos += stepSizePlane) {

                positionToCheck = new Vector2(x_pos, y_pos);        // Current position to check
                waterHit = false;   // reset waterHit

                // perform rayCast at position to check
                hitAll = Physics.RaycastAll(new Vector3(positionToCheck.x, rayPlaneHeight, positionToCheck.y), -Vector3.up, rayPlaneHeight - rayCastDepth, Physics.DefaultRaycastLayers);
               
                // probe all raycast hits
                foreach (RaycastHit hitProbe in hitAll) {
                    if (hitProbe.transform.gameObject.tag == waterTag) {    // if water hit
                        waterHitHeight = rayPlaneHeight - hitProbe.distance;    // get water height
                        waterHit = true;            
                    } else if (hitProbe.transform.gameObject.tag == terrainTag) {   // if terrain hit
                        terrainHeights.Add(rayPlaneHeight - hitProbe.distance);     // store to terrain heights list
                    }
                }

                if (waterHit) {     // if water hit, check if water is below or above terrain
                    foreach (float terrainHeight in terrainHeights) {

                        positionForCollisionCheck = new Vector3(positionToCheck.x, terrainHeight + yOffset, positionToCheck.y); // Calculate position for feasibility check (y-offset)
                        if (terrainHeight > waterHitHeight) {   // check terrain height above water height

                            robotModel.transform.position = positionForCollisionCheck;      // move robot
                            if (!feasibilityComponent.CollisionCheck()) {       // run feasibility check
                                validSpawns.Add(positionForCollisionCheck); // add to valid locations
                                nrOfValidSpawns++;  // count valid

                            } else {
                                invalidSpawns.Add(positionForCollisionCheck);   // add to invalid locations
                                nrOfInvalidSpawns++;    // count invalid
                            }

                        } else {
                            invalidSpawns.Add(positionForCollisionCheck);   // add to invalid locations
                            nrOfInvalidSpawns++;    // count invalid
                        }

                    }
                } else {
                    foreach (float terrainHeight in terrainHeights) {       // for each terrain hit height
                        positionForCollisionCheck = new Vector3(positionToCheck.x, terrainHeight + yOffset, positionToCheck.y); // Calculate position for feasibility check (y-offset)

                        robotModel.transform.position = positionForCollisionCheck;  // move robot
                        if (!feasibilityComponent.CollisionCheck()) {   // run feasibility check
                            validSpawns.Add(positionForCollisionCheck); // add to valid locations
                            nrOfValidSpawns++;  // count valid

                        }
                        else {
                            invalidSpawns.Add(positionForCollisionCheck);   // add to valid locations
                            nrOfInvalidSpawns++;    // count invalid
                        }   
                    }
                }

                terrainHeights.Clear(); // clear terrain heights list
            }
        }

        DateTime runEndTime = DateTime.Now; // End time

        return runEndTime - runStartTime;   // Return run time
    }

    // Perform multiple test runs
    void multipleRuns() {
        float stepSizeIncrement = (maxStepSize - minStepSize) / (N - 1);    // Increment steps from minStepSize to maxStepSize

        TimeSpan runTime;   // Run time to store

        // Create new directory with selected test nr and environment name
        string environmentFolder = "";
        switch (multiRunEnvironmentName) {
            case MultiRunEnvironment.highComplexEnvironment:
                environmentFolder = "HighComplexEnvironment";
                break;
            case MultiRunEnvironment.trainingEnvironment:
                environmentFolder = "TrainingEnvironment";
                break;
            case MultiRunEnvironment.lowComplexEnvironment:
                environmentFolder = "LowComplexEnvironment";
                break;
        }
        string path = "Assets/Results/2D_Search/" + environmentFolder + "/MultiRun_Test_" + multiRunTestNr;
        Directory.CreateDirectory(path); // returns a DirectoryInfo object

        // File writer
        StreamWriter multiRunDataWriter = new StreamWriter(path + "/multiRunResults.txt", false);

        // Run all test iterations
        for (float step = minStepSize; step <= maxStepSize; step = step + stepSizeIncrement) {
            DeleteSpawnLists(); // Delete results list

            runTime = RunSpawnTests(limitLow, limitHigh, step); // run single test

            // Write to file: Stepsize | Nr. of checks | Nr. of valid | Nr. of invalid | Time for specific run
            multiRunDataWriter.WriteLine("Step Size: " + Mathf.Round(step * 100) / 100 + " | Nr. of Tests = " + (Mathf.Round(((limitHigh.x - limitLow.x) / step)) + 1) * (Mathf.Round(((limitHigh.y - limitLow.y) / step)) + 1) + " | Nr. of VALID spawns = " + validSpawns.Count + " | Nr. of INVALID spawns = " + invalidSpawns.Count + " | Time Elapsed = " + runTime);
        }

        multiRunDataWriter.Close(); // close file writer
    }

    // Write single test to file
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
        string path = "Assets/Results/2D_Search/" + environmentFolder + "/Test_" + testNr;
        Directory.CreateDirectory(path); // returns a DirectoryInfo object

        // Write Valid Spawn Locations to Valid_Locations.txt
        StreamWriter validWriter = new StreamWriter(path + "/Valid_Locations.txt", false);
        foreach (Vector3 location in validSpawns) {
            validWriter.WriteLine(location.x + ", " + location.y + ", " + location.z);
        }
        validWriter.Close();

        // Write Invalid Spawn Locations to Invalid_Locations.txt
        StreamWriter invalidWriter = new StreamWriter(path + "/Invalid_Locations.txt", false);
        foreach (Vector3 location in invalidSpawns) {
            invalidWriter.WriteLine(location.x + ", " + location.y + ", " + location.z);
        }
        invalidWriter.Close();

        // Write Stats file with general information
        StreamWriter statsWriter = new StreamWriter(path + "/Stats.txt", false);
        statsWriter.WriteLine("Area Searched From: " + limitLow + "meter to: " + limitHigh + "meter");
        statsWriter.WriteLine("Step-size: " + stepSizeXZ + " meter");
        statsWriter.WriteLine("");
        statsWriter.WriteLine("Total Number of Checked Locations: " + (Mathf.Round(((limitHigh.x - limitLow.x) / stepSizeXZ))+1) * (Mathf.Round(((limitHigh.y - limitLow.y) / stepSizeXZ))+1));
        statsWriter.WriteLine("Nr. of VALID Spawns = " + nrOfValidSpawns);
        statsWriter.WriteLine("Nr. of INVALID Spawns = " + nrOfInvalidSpawns);
        statsWriter.WriteLine("");
        TimeSpan difference = endTime - startTime;
        statsWriter.WriteLine("Time for Calculations = " + difference);

        statsWriter.Close();
    }

    // Delete spawn lists
    void DeleteSpawnLists() {

        validSpawns.Clear();
        invalidSpawns.Clear();
    }

    // Draw Gizmos (markers) of spawns
    private void OnDrawGizmos() {
        
        // Draw search perimeter (high ray limit = red | low ray limit = blue)
        if (drawSearchArea) {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(new Vector3((limitHigh.x - limitLow.x) / 2 + limitLow.x, rayPlaneHeight, (limitHigh.y - limitLow.y) / 2 + limitLow.y), new Vector3((limitHigh.x - limitLow.x), 0.1f, (limitHigh.y - limitLow.y)));

            Gizmos.color = Color.blue;
            Gizmos.DrawCube(new Vector3((limitHigh.x - limitLow.x) / 2 + limitLow.x, rayCastDepth, (limitHigh.y - limitLow.y) / 2 + limitLow.y), new Vector3((limitHigh.x - limitLow.x), 0.1f, (limitHigh.y - limitLow.y)));
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
    }
}