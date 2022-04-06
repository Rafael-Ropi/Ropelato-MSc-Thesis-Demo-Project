/***********************************************************************************************************************
* Obstacle Generator for Benchmark Environment                                                                         *
* By Rafael F. Ropelato                                                                                                *
* University of Maryland - Institute for Systems Research                                                              *
* Part of MSc. Thesis "Generating Feasible Spawn Locations for Autonomous Robot Simulations in Complex Environments"   *
*                                                                                                                      *
************************************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]

public class GenerateColliders : MonoBehaviour
{
    [Header("Obstacle Object")]
    public GameObject colliderObjectPrefab = null;

    [Header("Nr. of Objects to Spawn")]
    public int numX = 1;
    public int numY = 1;
    public int numZ = 1;

    [Header("Spacing of Objects")]
    public float spacing = 1.5f;

    [Header("Spawn/Delete Obstales")]
    public bool spawnObjects = false;
    public bool deleteChilderen = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (spawnObjects) {
            spawnObjects = false;

            for(int i = 0; i < numX; i++) {
                for (int j = 0; j < numY; j++) {
                    for (int k = 0; k < numZ; k++) {
                        GameObject box = Instantiate(colliderObjectPrefab, transform.position + new Vector3(i * spacing, j * spacing, k * spacing), Quaternion.identity);
                        box.transform.parent = transform;
                    }                    
                }               
            }
        }
        
        if (deleteChilderen) {
            deleteChilderen = false;

            while (transform.childCount > 0) {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
    }
}
