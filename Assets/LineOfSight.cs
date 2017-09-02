using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSight : MonoBehaviour
{

    private RaycastHit vision; // Used for detecting collision
    public float rayLength; // For assigning a length to the raycast
    private GameObject lastObject; // To save the object. 


    void Start()
    {
        rayLength = 1000.0f;
    }

    GameObject setStandardShader(GameObject o)
    {
        o.GetComponent<Renderer>().material.shader = Shader.Find("Standard");
        Debug.Log("Set " + lastObject.name + " to standard shader");
        return o;
    }

    GameObject setDiffuseShader(GameObject o)
    {
        o.GetComponent<Renderer>().material.shader = Shader.Find("Self-Illumin/Outlined Diffuse");
        Debug.Log("Set " + lastObject.name + " to diffuse shader");
        return o;
    }

    void Update()
    {
        // Constantly draw the ray
        //Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * rayLength, Color.red, 0.5f);
        // Doesn't seem to work, commenting out

        // If the Raycast hits a collider...
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out vision, rayLength))
        {

            // Check for 'Interactive' tag
            if (vision.collider.tag == "Interactive")
            {
                //Debug.Log("Raycast Target: " + vision.collider.name);

                // If this is a new object, mouseExit old one and mouseEnter new one
                if (lastObject == null)
                {
                    lastObject = vision.collider.gameObject;
                    //lastObject.SendMessage("OnMouseEnter");
                    lastObject = setDiffuseShader(lastObject);
                }
                else if(lastObject != vision.collider.gameObject)
                {
                    //lastObject.SendMessage("OnMouseExit");
                    lastObject = setStandardShader(lastObject);
                    lastObject = vision.collider.gameObject;
                    //lastObject.SendMessage("OnMouseEnter");
                    lastObject = setDiffuseShader(lastObject);
                }

                lastObject = vision.collider.gameObject;

                // Then if they press E it activates
                if (Input.GetMouseButtonDown(0))
                {
                    
                    
                    lastObject.SendMessage("Activate");
                }
            }
            else
            {
                // Did not hit, need to check if lastObject isn't null, if it's not do mouseExit and null it, separately because not interactive
                if (lastObject != null)
                {
                    //lastObject.SendMessage("OnMouseExit");
                    lastObject = setStandardShader(lastObject);
                    lastObject = null;
                }
            }
        }
        else
        {
            // Did not hit, need to check if lastObject isn't null, if it's not do mouseExit and null it
            if(lastObject != null)
            {
                //lastObject.SendMessage("OnMouseExit");
                lastObject = setStandardShader(lastObject);
                lastObject = null;
            }
        }
    }
}
