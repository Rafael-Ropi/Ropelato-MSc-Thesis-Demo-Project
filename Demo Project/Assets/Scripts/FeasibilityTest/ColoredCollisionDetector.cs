using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ColoredCollisionDetector : MonoBehaviour
{
    private Material greenMaterial = null;
    private Material redMaterial = null;

    private void Start()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r.sharedMaterial != null)
            {
                greenMaterial = new Material(r.sharedMaterial);
                greenMaterial.color = new Color(0f, 0.8f, 0.2f);
                redMaterial = new Material(r.sharedMaterial);
                redMaterial.color = new Color(0.8f, 0f, 0f);
                break;
            }
        }

        SetCollision(false);
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            RunPhysics();
        }
    }

    private void SetCollision(bool collision)
    {
        if (greenMaterial != null && redMaterial != null)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = collision ? redMaterial : greenMaterial;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        SetCollision(true);
    }

    private void OnTriggerExit(Collider other)
    {
        SetCollision(false);
    }
    private void OnTriggerStay(Collider other)
    {
        SetCollision(true);
    }

    public void RunPhysics()
    {
        // advance physics simulation by one frame
        Physics.autoSimulation = false;
        Physics.Simulate(Time.fixedDeltaTime);
        Physics.Simulate(Time.fixedDeltaTime);
        Physics.autoSimulation = true;
    }
}