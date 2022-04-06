/***********************************************************************************************************************
* 3D Search Algorithm                                                                                                  *
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

[ExecuteInEditMode]   // run script in edit mode

public class Search3D : MonoBehaviour {
    [Header("Attach Robot Model")] // Model to check spawns for
    public GameObject robotModel = null;

    [Header("Settings - Spawn Generator")] // Variables for Spawn Generator 
    public Vector3 limitLow = new Vector3(-10, -10, -10);   // Search perimeter lower limit
    public Vector3 limitHigh = new Vector3(10, 10, 10);     // Search perimeter upper limit
    public float stepSizeXZ = 1f;       // Step size in XZ plane
    public float stepSizeY = 1f;        // Step size in Y direction
    public float checkHeight = 1.5f;        // Max height above Terrain
    public float maxWaterHeight = 100f;     // Max height of any water plane
    public string[] ignoredTags = {"Robot", "Terrain"}; // Tags that are not an obstacle
    public string waterTag = "Water";   // Water Tag
    public string terrainTag = "Terrain";   // Terrain Tag

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
    public bool destroySpawnLists = false;  // Destroy existing results

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
    public int multiRunTestNr = 0;      // Test number, used for save folder name
    public enum MultiRunEnvironment {   // Environment in which the test is performed
        trainingEnvironment,
        lowComplexEnvironment,
        highComplexEnvironment,
        benchmarkEnvironment
    }
    public MultiRunEnvironment multiRunEnvironmentName;
    public int N = 1;       // Nr. of test runs
    public float minStepSize = 0.1f;    // Linear step size increment lower limit
    public float maxStepSize = 1f;      // Linear step size increment upper limit
    public bool runMultipleTests = false;   // Execute multiple test runs



    // Lists to save spawn locations (valid, invalid, or in the air)
    private List<Vector3> validSpawns = new List<Vector3>();
    private List<Vector3> invalidSpawns = new List<Vector3>();
    private List<Vector3> airSpawns = new List<Vector3>();
    
    // Variables for Spawn Check loop
    private Vector3 positionToCheck;    // Current Position to Check
    private bool tagInList = false;     // Compare hit Tag to Ignore-Tags
    private bool rayHitClear = false;   // Check if Ray hit is clear
    private bool rayHitLength = false;   // Check if Ray hit is clear
    private bool waterHit = false;      // Check if water above Spawn

    // Time variables
    private DateTime startTime;
    private DateTime endTime;

    // Counters
    private int nrOfInvalidSpawns = 0;
    private int nrOfValidSpawns = 0;
    private int nrOfAirSpawns = 0;

    // Start is called before the first frame update
    void Start(){
        
    }

    // Update is called once per frame
    void Update(){

        // Run Test when Bool is set
        if (runTest){
            runTest = false;        // Reset runTest
            Vector3 initialPos = robotModel.transform.position;     // Save initial robot position
            robotModel.transform.GetChild(0).transform.position = robotModel.transform.position;    // Set mesh object to robot position
            
            DeleteSpawnLists();     // Delete Old Spawn Lists
            nrOfInvalidSpawns = 0;  // Reset Counters
            nrOfValidSpawns = 0;
            nrOfAirSpawns = 0;

            startTime = DateTime.Now;
            RunSpawnTests(limitLow, limitHigh, stepSizeXZ, stepSizeY);   // Run Test Method
            endTime = DateTime.Now;

            robotModel.transform.position = initialPos;         // Return Robot to initial pos 
            robotModel.GetComponentInChildren<CollisionCheck>().RunPhysics();

        }

        // Execute functions when bools are set
        // Run multiple tests
        if (runMultipleTests) {
            runMultipleTests = false;
            multipleRuns();
        }
        
        // Write to file
        if (writeToFile) {
            writeToFile = false;
            WriteToFile();
        }
        
        // Destroy 
        if (destroySpawnLists){
            destroySpawnLists = false;  // Reset destroySpawnLists
            DeleteSpawnLists();         // Delete old Indicators (Spawn Lists)
        }
    }

    // Run spawn check
    TimeSpan RunSpawnTests(Vector3 lowLimit, Vector3 highLimit, float stepSizePlane, float stepSizeHeight) {

        DateTime runStartTime = DateTime.Now;       // Start time

        FeasibilityCheck feasibilityComponent = robotModel.GetComponentInChildren<FeasibilityCheck>();  // Get feasibility check component from robot

        // Itarate through 3 Dimensions
        for (float xPos = lowLimit.x; xPos <= highLimit.x; xPos = xPos + stepSizePlane) {
            for (float yPos = lowLimit.y; yPos <= highLimit.y; yPos = yPos + stepSizeHeight) {
                for (float zPos = lowLimit.z; zPos <= highLimit.z; zPos = zPos + stepSizePlane) {

                    positionToCheck = new Vector3(xPos, yPos, zPos);        // Current position to check
                    
                    if (feasibilityComponent.AerialTest(positionToCheck, checkHeight, ignoredTags)) {       // Aerial Check (height above ground)
                        if (!feasibilityComponent.WaterCheck(positionToCheck, maxWaterHeight, waterTag)) {  // Water Check (below water plane?)

                            robotModel.transform.position = positionToCheck;    // Move robot to current check position
                            if (!feasibilityComponent.CollisionCheck()) {       // if NOT colliding
                                validSpawns.Add(positionToCheck);   // Add to valid
                                nrOfValidSpawns++;                  // Count valid
                            }
                            else {
                                invalidSpawns.Add(positionToCheck); // Add to invalid
                                nrOfInvalidSpawns++;                // Count invalid
                            }
                        }
                        else {
                            invalidSpawns.Add(positionToCheck);     // Add to invalid
                            nrOfInvalidSpawns++;                    // Count invalid
                        }
                    }
                    else {
                        airSpawns.Add(positionToCheck);             // Add to air
                        nrOfAirSpawns++;                            // Count air
                    }
                }
            }
        }

        DateTime runEndTime = DateTime.Now; // End time

        return runEndTime - runStartTime;   // return run time
    }
    
    // Perform multiple runs
    void multipleRuns() {
        float stepSizeIncrement = (maxStepSize - minStepSize) / (N - 1);    // Increment steps from minStepSize to maxStepSize

        TimeSpan runTime;   // Run time to return

        // Create new directory with selected test nr and environment name
        string environmentFolder = "";
        switch (multiRunEnvironmentName) {
            case MultiRunEnvironment.highComplexEnvironment:
                environmentFolder = "HighComplexEnvironment";
                break;;
            case MultiRunEnvironment.trainingEnvironment:
                environmentFolder = "TrainingEnvironment";
                break;
            case MultiRunEnvironment.lowComplexEnvironment:
                environmentFolder = "LowComplexEnvironment";
                break;
            case MultiRunEnvironment.benchmarkEnvironment:
                environmentFolder = "BenchmarkEnvironment";
                break;
        }
        string path = "Assets/Results/3D_Search/" + environmentFolder + "/MultiRun_Test_" + multiRunTestNr;
        Directory.CreateDirectory(path);

        StreamWriter multiRunDataWriter = new StreamWriter(path + "/multiRunResults.txt", false); // File writer for multi run results

        // Run all iterations
        for (float step = minStepSize; step <= maxStepSize; step = step + stepSizeIncrement) {
            DeleteSpawnLists();     // clear spawn lists
            runTime = RunSpawnTests(limitLow, limitHigh, step, step);   // run test, store time for run

            // Write to file: Stepsize | Nr. of checks | Nr. of valid | Nr. of invalid | Nr. of air spawns | Time for specific run
            multiRunDataWriter.WriteLine("Step Size: " + Mathf.Round(step*100)/100 + " | Nr. of Tests = " + (Mathf.Round(((limitHigh.x - limitLow.x) / step)) + 1) * (Mathf.Round(((limitHigh.y - limitLow.y) / step)) + 1) * (Mathf.Round(((limitHigh.z - limitLow.z) / step)) + 1) + " | Nr. of VALID spawns = " + validSpawns.Count + " | Nr. of INVALID spawns = " + invalidSpawns.Count + " | Nr. of AIR spawns = " + airSpawns.Count + " | Time Elapsed = " + runTime);
        }
        multiRunDataWriter.Close(); // Close file writer
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
            case Environment.benchmarkEnvironment:
                environmentFolder = "BenchmarkEnvironment";
                break;
        }
        string path = "Assets/Results/3D_Search/" + environmentFolder + "/Test_" + testNr;
        Directory.CreateDirectory(path);

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

        // Write Air Spawn Locations to Air_Locations.txt
        StreamWriter airWriter = new StreamWriter(path + "/Air_Locations.txt", false);
        foreach (Vector3 location in airSpawns) {
            airWriter.WriteLine(location.x + ", " + location.y + ", " + location.z);
        }
        airWriter.Close();

        // Write Stats file with general information
        StreamWriter statsWriter = new StreamWriter(path + "/Stats.txt", false);    // Start file writer

        statsWriter.WriteLine("Area Searched From: " + limitLow + "meter to: " + limitHigh + "meter");  
        statsWriter.WriteLine("Step-size in X-Z Dimension: " + stepSizeXZ + " meter");
        statsWriter.WriteLine("Step-size in Y Dimension: " + stepSizeY + " meter");
        statsWriter.WriteLine("Number of Checked Locations: " + (Mathf.Round(((limitHigh.x - limitLow.x) / stepSizeXZ)) + 1) * (Mathf.Round(((limitHigh.y - limitLow.y) / stepSizeY)) + 1)  * (Mathf.Round(((limitHigh.z - limitLow.z) / stepSizeXZ)) + 1));
        statsWriter.WriteLine("Nr. of VALID Spawns = " + nrOfValidSpawns);
        statsWriter.WriteLine("Nr. of INVALID Spawns = " + nrOfInvalidSpawns);
        statsWriter.WriteLine("Nr. of AIR Spawns = " + nrOfAirSpawns);
        TimeSpan difference = endTime - startTime;
        statsWriter.WriteLine("Time for Calculations = " + difference);

        statsWriter.Close();    // Close file writer
    }

    // Clear all spawn lists 
    void DeleteSpawnLists()
    {
        validSpawns.Clear();
        invalidSpawns.Clear();
        airSpawns.Clear();
    }

    // Draw Gizmos (markers) of spawns
    private void OnDrawGizmos(){

        // Draw search perimeter
        if (drawSearchArea) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3((limitHigh.x - limitLow.x) / 2 + limitLow.x, (limitHigh.y - limitLow.y) / 2 + limitLow.y, (limitHigh.z - limitLow.z) / 2 + limitLow.z), new Vector3((limitHigh.x - limitLow.x), (limitHigh.y - limitLow.y), (limitHigh.z - limitLow.z)));
        }

        // Draw invalid spawns (red)
        if (drawInvalid)
        {
            Gizmos.color = new Color(1f, 0f, 0f, gizmoAlpha);
            foreach (Vector3 indicatorPos in invalidSpawns)
            {
                if (gizmoSphere)
                {
                    if (gizmoWire)
                    {
                        Gizmos.DrawWireSphere(indicatorPos, gizmoSize);
                    }
                    else
                    {
                        Gizmos.DrawSphere(indicatorPos, gizmoSize);
                    }

                }
                else
                {
                    if (gizmoWire)
                    {
                        Gizmos.DrawWireCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                    else
                    {
                        Gizmos.DrawCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                }
            }
        }

        // Draw valid spawns (green)
        if (drawValid)
        {
            Gizmos.color = new Color(0f, 1f, 0f, gizmoAlpha);
            foreach (Vector3 indicatorPos in validSpawns)
            {
                if (gizmoSphere)
                {
                    if (gizmoWire)
                    {
                        Gizmos.DrawWireSphere(indicatorPos, gizmoSize);
                    }
                    else
                    {
                        Gizmos.DrawSphere(indicatorPos, gizmoSize);
                    }

                }
                else
                {
                    if (gizmoWire)
                    {
                        Gizmos.DrawWireCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                    else
                    {
                        Gizmos.DrawCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                }
            }
        }

        // Draw aerial spawns (blue)
        if (drawAir)
        {
            Gizmos.color = new Color(0f, 0f, 1f, gizmoAlpha);
            foreach (Vector3 indicatorPos in airSpawns)
            {
                if (gizmoSphere)
                {
                    if (gizmoWire)
                    {
                        Gizmos.DrawWireSphere(indicatorPos, gizmoSize);
                    }
                    else
                    {
                        Gizmos.DrawSphere(indicatorPos, gizmoSize);
                    }

                }
                else
                {
                    if (gizmoWire)
                    {
                        Gizmos.DrawWireCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                    else
                    {
                        Gizmos.DrawCube(indicatorPos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                    }
                }
            }
        }

    }
}

