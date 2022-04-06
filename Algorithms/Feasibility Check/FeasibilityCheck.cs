/***********************************************************************************************************************
* Feasibility Check                                                                                                    *
* By Rafael F. Ropelato                                                                                                *
* University of Maryland - Institute for Systems Research                                                              *
* Part of MSc. Thesis "Generating Feasible Spawn Locations for Autonomous Robot Simulations in Complex Environments"   *
*                                                                                                                      *
************************************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class FeasibilityCheck : MonoBehaviour
{
    // collision state, accessible by other algorithms
    public bool collision = false;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Aerial test function
    public bool AerialTest (Vector3 positionToCheck, float rayDistance, string[] ignoredTags) {
        bool tagInList = false;     // Check if object below test is in ignored tag list
        bool rayHitClear = true;    // Ray hit ignored objects
        bool rayHitLength = false;  // Ray hit within set length

        // Ray and hit constructor
        var ray = new Ray(positionToCheck, -Vector3.up);
        RaycastHit hit;

        // If ray hits object
        if (Physics.Raycast(ray, out hit, rayDistance, Physics.DefaultRaycastLayers)) {
            rayHitLength = true;

            // Check for ignored tasks (e.g. not to hit robot before moving it)
            foreach (string tagName in ignoredTags) {
                if (hit.transform.gameObject.tag == tagName) {
                    tagInList = true;
                    break;
                }
            }

            if (!tagInList) {
                rayHitClear = false;
            }
        }

        // Return outcome of aerial check
        if(rayHitLength & rayHitClear) {
            return true;
        } else {
            return false;
        }
    }
    
    // Perform Collision Check
    public bool CollisionCheck() {
        RunPhysics();
        return collision;
    }

    // Perform Water Check
    public bool WaterCheck(Vector3 positionToCheck, float maxWaterHeight, string waterTag) {
        bool waterHit = false;

        // RayCast upwards, check if water plane is hit
        RaycastHit[] hitAll;
        hitAll = Physics.RaycastAll(positionToCheck, Vector3.up, maxWaterHeight - positionToCheck.y, Physics.DefaultRaycastLayers);

        foreach (RaycastHit hitProbe in hitAll) {
            if (hitProbe.transform.gameObject.tag == waterTag) {
                waterHit = true;
            }
        }

        // Return outcome of water check
        return waterHit;
    }

    // Advance physics egine by 1-step
    public void RunPhysics() {
        Physics.autoSimulation = false;
        Physics.Simulate(Time.fixedDeltaTime);
        Physics.autoSimulation = true;
    }

    private void OnTriggerExit(Collider other) {
        collision = false;
    }

    private void OnTriggerEnter(Collider other) {
        collision = true;
    }

    public void OnTriggerStay(Collider other) {
        collision = true;
    }
}
