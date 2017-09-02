using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// We're adding classes here too, so what
public static class ListExtensions
{
    public static void AddMany<T>(this List<T> list, params T[] elements)
    {
        list.AddRange(elements);
    }
}



public class Rocket_Control : MonoBehaviour {

    public bool accelerating;
    public Rigidbody rb;
    private float accel;
    public float pressureAtSeaLevel;
    public float atmosphericHeight;
    private float dragCoefficient;
    private float minimumMass; // Mass at which you're out of fuel and you lose
    public float fuelDrainRate;
    public Text velocityText;
    public Text altitudeText;
    public Text gForceText;
    public Image fuelBar;
    private float startingMass;
    private float lastVelocity;
    private double gForces;
    private Vector3 gForceDirection;
    public int altitude;
    public float totalVel;
    private bool startup;
    public float gravityForce = -9.8f;
    public GameObject earth;
    public AudioClip engineSound;
    private new AudioSource audio;
    private List<int> randomizedButtonList = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
    private int randomButtonNumber = 0;
    private bool firstLaunch = true;
    public GameObject centerOfMassObject;
    public CapsuleCollider playerCollider;
    public float secondsBetweenCalculations = 0.1f;
    // I should really use Time.deltaTime for this but idk
    private Vector3 localCockpitPosition;
    private Vector3 lastPosition;
    private Vector3 lastVelocityVector;
    private bool drainingFuel = false;
    private int jettisonTimer = 0;
    Vector3 forceToApplyOnRocket = Vector3.zero;
    Vector3 dragForce = Vector3.zero;
    Vector3 thrustForce = Vector3.zero; // Just for documentation/debugging
    Vector3 gravityForceOnPlayer = Vector3.zero;
    Vector3 gravityVector = new Vector3(0, -9.8f, 0);
    float rocketNewMass;
    public Material skybox;
    //public GameObject navBall;
    private Vector3 navballRotation;
    public Camera rocketCameraMoving;

    public bool controllingRocket { get; private set; }


    // Use this for initialization
    // Awake occurs before Start, so hopefully I can initalize other things in Start and depend on this script to already have the things
    void Awake () {
        //navballRotation = navBall.transform.eulerAngles;
        controllingRocket = true;
        accelerating = false;
        accel = 33000000.0f; // Force of Saturn V launch rocket
        Physics.gravity = new Vector3(0, -9.8f, 0); // We will adjust this as we get further from the planet
        pressureAtSeaLevel = 1.0f;
        atmosphericHeight = 100000.0f; // Assume negligible air resistance for the last 300km, different zone so different formula
        dragCoefficient = 0.515f; // Saturn V coefficient is 0.515 per second
        minimumMass = 118000; // Payload weight of Saturn V, max.  Thrust time 500 seconds, so fuel drain should be about ((mass-minimumMass)/500)*.9
        fuelDrainRate = ((rb.mass - minimumMass) / 500)*.8f; // Includes some error room

        startingMass = rb.mass-minimumMass;
        lastVelocity = 0;
        startup = true;

        audio = GetComponent<AudioSource>();

        Cursor.visible = false;

        // We're initializing basically everything here instead of on some other object, so.

        // Need: list containing numbers 1-8, randomize this list, make it accessible to randombuttons
        for (int i = 0; i < randomizedButtonList.Count; i++)
        {
            int temp = (int)randomizedButtonList[i];
            int randomIndex = UnityEngine.Random.Range(i, randomizedButtonList.Count);
            randomizedButtonList[i] = randomizedButtonList[randomIndex];
            randomizedButtonList[randomIndex] = temp;
        }
        string blistdisplay = "";
        for (int i = 0; i < randomizedButtonList.Count; i++)
            blistdisplay = blistdisplay + randomizedButtonList[i] + ", ";
        //Debug.Log("Random button order: " + blistdisplay);
        // Now let's keep track of which numbers we've given out and we'll have a function to get your number


        rb.isKinematic = true;
        // Disable movement til it launches
        rb.centerOfMass = centerOfMassObject.transform.localPosition;

        lastPosition = rb.transform.position;
        lastVelocityVector = rb.velocity;

        rocketNewMass = rb.mass;
    }

    void Start()
    {
        // Put this here to ensure the player is already loaded before we take this var
        localCockpitPosition = playerCollider.transform.localPosition;
    }

    public int getRandomButtonCode()
    {
        int code = randomizedButtonList[randomButtonNumber];
        randomButtonNumber++;
        return code;
    }
	
	// Update is called once per frame
	void Update () {
        // Update navball rotation
        //navBall.transform.eulerAngles = rb.transform.eulerAngles; // Since the navball isn't moving we have to simulate it.
        
        // Send altitude data to screen
        altitude = (int)(Vector3.Distance(earth.transform.position, transform.position) - (earth.GetComponent<SphereCollider>().radius*earth.transform.localScale.y));
        //altitude = (int) transform.position.y;
        altitudeText.text = altitude + "m";

        // Skybox work
        // Increase iterations up to 20, volumetric steps up to 18, camera scroll up to ... 100? idk
        // _TintAmount from 100 to 0
        float heightRatio = (altitude / atmosphericHeight); // 0 to 1
        if (heightRatio > 1)
            heightRatio = 1;

        float camScroll = 0.01f; // Editing this causes things to get jumpy
        float iterations = 2 + (heightRatio * 18); // 2-20
        float volsteps = 1 + (heightRatio * 17); // 1-18, more gets weird
        float tintAmount = 80 - (heightRatio * 100); // 80-0
        if (tintAmount < 0) // Special rules to cut this early but still be linear
            tintAmount = 0;
        //Debug.Log(tintAmount);

        skybox.SetFloat("_CamScroll", camScroll);
        skybox.SetFloat("_Iterations", iterations);
        skybox.SetFloat("_Volsteps", volsteps);
        skybox.SetFloat("_TintAmount", tintAmount);

        RenderSettings.skybox = skybox; // We apparently need to update it

        CalculateAccelerations();
        ApplyAccelerations();

        // Move the rocket camera to match the player for parallax
        rocketCameraMoving.transform.localPosition = playerCollider.transform.localPosition; // Because the camera is part of the collider, it gives a bad localposition

    }

    private void FixedUpdate()
    {


        // Send velocity data to screen
        //Debug.Log("Initial velocity=" + rb.velocity);
        totalVel = rb.velocity.magnitude;
        lastPosition = rb.transform.position;

        //Debug.Log("Lastvel=" + lastVelocity + ", totalVel = " + totalVel + ", altitude = " + altitude);
        lastVelocityVector = rb.velocity;

        velocityText.text = totalVel + " m/s";

        float floorBoundary = (playerCollider.height + playerCollider.GetComponent<PlayerControls>().advancedSettings.stickToGroundHelperDistance);

               
    }

    // Controls start engaged and release on hitting F, on playercontroller.  Re-enaged by clicking the joystick
    public void releaseRocketControl()
    {
        controllingRocket = false;
        //Camera.main.GetComponent<Rigidbody>().isKinematic = !Camera.main.GetComponent<Rigidbody>().isKinematic;
        // Camera can stay kinematic, the collider is the one that does the work
        // But the collider needs to be toggled
        playerCollider.GetComponent<Rigidbody>().isKinematic = false;
        ////Debug.Log("Rocket control is now " + controllingRocket + ", kinematic is set to " + playerCollider.GetComponent<Rigidbody>().isKinematic);
    }

    private void CalculateAccelerations()
    {
        // Here, we will calculate thrust acceleration if accelerating, and also drag force
        // We can add each of these up into its own vector before applying them to reduce load on physics engine (not that it matters)
        // But we can then use this to calculate G's on the pilot
        // This function should be called on fixedUpdate() and will make use of Time.deltaTime
        // We will not apply any forces here, that's in applyAccelerationsOnRocket using values this function set

        // I'd like to move everything that was set to repeating things to go here instead
        // DrainFuel, CameraShake, ApplyDrag, apply g forces to player if unrestrained, and collision correction if necessary

        // We will need to set these variables from the class:
        // Vector3 forceToApplyOnRocket;
        // Vector3 dragForce;
        // Vector3 thrustForce; // Just for documentation/debugging
        // Vector3 gravityForceOnPlayer;
        // Vector3 gravityVector;
        // float rocketNewMass;
        // Need to set fuel, but too lazy to make fuel system, so that means setting mass

        // Thrust
        if (accelerating)
        {
            thrustForce = (transform.up * accel);
            //Debug.Log("Adding acceleration force in " + transform.up + " direction of " + accel + "N");
            //playerCollider.transform.localPosition = new Vector3(playerCollider.transform.localPosition.x, lastPlayerLocalY, playerCollider.transform.localPosition.z);

            // And Fuel Drain

            rocketNewMass = rb.mass - fuelDrainRate * Time.deltaTime;
        }
        else
            thrustForce = Vector3.zero; // If not thrusting... don't thrust.

        if(drainingFuel)
        {
            rocketNewMass = rb.mass - fuelDrainRate * Time.deltaTime * 2; // Just double
        }

        // Drag
        if(jettisonTimer > 0)
        {
            // Putting this here should ensure there's time to clip through the roof before I re-enable collisions
            jettisonTimer--;
            if (jettisonTimer == 0)
            {
                playerCollider.isTrigger = false;
            }
        }

        float atmosphericPressure = pressureAtSeaLevel * Mathf.Pow((float)System.Math.E, (float)(-altitude) / atmosphericHeight);
        double dragMagnitude = System.Math.Round((0.004892f * atmosphericPressure * rb.velocity.sqrMagnitude * dragCoefficient * rb.mass) * Time.deltaTime, 2);
        if(altitude > atmosphericHeight)
        {
            dragMagnitude = 0;
        }

        Vector3 dragDirection = Vector3.zero;
        //if (rb.velocity.sqrMagnitude > 2500) // > 50m/s
            dragDirection = -rb.velocity.normalized;
        //else
            //dragDirection = Vector3.zero; // No drag if below 50m/s in any situation to prevent weird issues with direction on velocity

        dragForce = (dragDirection * (float)dragMagnitude);
        //Debug.Log(dragForce + "drag force vector, Pressure: " + atmosphericPressure);
        // Now update gravity by distance
        // F = -9.8*((rocket.mass*earth.mass)/math.pow(altitude,2)
        // Because earth mass is way too big, so we use a decimal datatype which is 20x slower...

        // Altitude + earthRadius = R, x/R^2 where x is our gravitational constant, then -9.8*(x/R^2)
        // Or about 6000000 = R, 

        decimal bigGCoefficient = 3.6e13m;
        gravityForce = (float)(-9.8m * decimal.Round(bigGCoefficient / decimal.Round((decimal)Mathf.Pow(altitude + (earth.GetComponent<SphereCollider>().radius * earth.transform.localScale.y), 2))));
        //Debug.Log(gravityForce + "m/s^2 of Gravity");

        gravityVector = (transform.position - earth.transform.position).normalized * gravityForce; // Uses unity physics so we don't need deltaTime

        // We now have what we need to set up the forceToApplyOnRocket, doesn't include gravity, that's handled by Physics
        //Debug.Log(thrustForce + " + " + dragForce);
        forceToApplyOnRocket = dragForce + thrustForce; // Deltatime will need to apply, but not until we apply settings, we need this for calculations

        //Debug.Log("Total rocket force: " + forceToApplyOnRocket);

        // And the hard part, G's on the player
        // Which is no longer hard, it's the normal of the rocketforce/rocketmass (F=ma, divide out rocket mass and add player mass which is effectively 1)
        gravityForceOnPlayer = (-forceToApplyOnRocket / rocketNewMass); // I think we can apply this as an impulse with deltaTime
        //Debug.Log("Gravity on player = " + gravityForceOnPlayer);
        // If the rocket is stationary or on the ground, add gravityVector
        if(rb.velocity.sqrMagnitude < 1)
        {
            //Debug.Log(rb.velocity + " is velocity sqrmagnitude < 1, adding player grav");
            gravityForceOnPlayer += gravityVector;
        }

        gForces = (gravityForceOnPlayer.magnitude / 9.8f)*-Math.Sign(gravityForceOnPlayer.y); // The sign is relative to the global Y axis of this vector
        gForceText.text = System.Math.Round(gForces,2) + " g's";

        // I don't think these are used, but good for debugging

        lastVelocity = totalVel;
        lastPosition = rb.transform.position;
        startup = false;

        // Camera shake
        if (gForces >= 1.2)
        {
            Camera.main.SendMessage("Shake", new Vector3((float)(Time.deltaTime* 4 * (gForces / 6)), (float)(Time.deltaTime / 50 * (gForces / 6)), (float)((Time.deltaTime * 10 - (gForces / 6)) / 5)));
            // Intensity, duration, delay
            // Very customized with numbers that make it look good ... no relation to secondsBetweenCalculations at first...
            // Had to force a relation by shoving it into each one
        }


    }

    private void ApplyAccelerations()
    {
        // This should be very simple assuming calculate was run first
        // We will need to use these variables from the class:
        // Vector3 forceToApplyOnRocket;
        // Vector3 gravityForceOnPlayer;
        // Vector3 gravityVector;
        // float rocketNewMass;
        // Need to set fuel, but too lazy to make fuel system, so that means setting mass

        // Adjust mass and fuelbar
        rb.mass = rocketNewMass;
        int fuelBarHeight = (int)(((rb.mass - minimumMass) / startingMass) * 100);
        fuelBar.rectTransform.sizeDelta = new Vector2(100, fuelBarHeight);
        if (rb.mass < minimumMass)
        {
            accelerating = false;
        }

        // Set Physics gravity
        Physics.gravity = gravityVector;

        // Apply forces on rocket
        rb.AddForce(forceToApplyOnRocket * Time.deltaTime,ForceMode.Impulse);

        // Apply on player if necessary
        if (!controllingRocket && !playerCollider.GetComponent<PlayerControls>().Grounded)
        {
            playerCollider.GetComponent<Rigidbody>().AddForce(gravityForceOnPlayer * Time.deltaTime,ForceMode.Impulse);
        }


    }

    private void CameraShake()
    {
        if(gForces >= 1.2)
        {
            Camera.main.SendMessage("Shake", new Vector3((float)(secondsBetweenCalculations* 4 * (gForces / 6)), (float)(secondsBetweenCalculations/50 * (gForces / 6)), (float)((secondsBetweenCalculations *10- (gForces/6))/5)));
            // Intensity, duration, delay
            // Very customized with numbers that make it look good ... no relation to secondsBetweenCalculations at first...
            // Had to force a relation by shoving it into each one
        }

    }

    private void DrainFuel()
    {
        if(accelerating)
        {
            int oldHeight = (int)(((rb.mass - minimumMass) / startingMass) * 100);
            rb.mass -= fuelDrainRate* secondsBetweenCalculations;
            int fuelBarHeight = (int)(((rb.mass - minimumMass) / startingMass) * 100);
            fuelBar.rectTransform.sizeDelta = new Vector2(100, fuelBarHeight);
            fuelBar.rectTransform.localPosition = new Vector3(fuelBar.rectTransform.localPosition.x, fuelBar.rectTransform.localPosition.y-(oldHeight-fuelBarHeight)*2);
            if(rb.mass < minimumMass)
            {
                accelerating = false;
            }
        }
    }

    

    public IEnumerator MoveOverSpeed(GameObject objectToMove, Vector3 end, float speed)
    {
        // speed should be 1 unit per second
        while (objectToMove.transform.position != end)
        {
            objectToMove.transform.position = Vector3.MoveTowards(objectToMove.transform.position, end, speed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator MoveOverSeconds(GameObject objectToMove, Vector3 end, float seconds)
    {
        //Debug.Log("Moving to " + end);
        float elapsedTime = 0;
        Vector3 startingPos = objectToMove.transform.localPosition;
        while (elapsedTime < seconds)
        {
            objectToMove.transform.localPosition = Vector3.Lerp(startingPos, end, (elapsedTime / seconds));
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        objectToMove.transform.localPosition = end;
    }

    void Launch ()
    {
        if (rb.mass >= minimumMass) // Ensures you can't reactivate if fuel is out...
        {
            accelerating = !accelerating;
            if (accelerating)
            {
                rb.isKinematic = false; // It's only kinematic at the start and until they stop firing
                audio.clip = engineSound;
                audio.loop = true;
                audio.Play();
            }
            else
            {
                audio.Stop();
            }
        }
    }

    public void enterCockpit()
    {
        if (!controllingRocket)
        {
            playerCollider.GetComponent<Rigidbody>().isKinematic = true; // Stop applying forces, including controls
            // We actually want the player kinematic to pilot anyway so this is good
            StartCoroutine(MoveOverSeconds(playerCollider.gameObject, localCockpitPosition, 1)); // I want a static amount of time no matter how far they are to zoom in
            controllingRocket = true;
        }
    }

    public void jettisonPlayer()
    {
        playerCollider.transform.parent = null; // Should work
        playerCollider.GetComponent<Rigidbody>().isKinematic = false;
        playerCollider.isTrigger = true;
        controllingRocket = false;
        playerCollider.GetComponent<Rigidbody>().AddForce(new Vector3(10, 50, -10), ForceMode.Impulse);
        jettisonTimer = 10;
    }

    public void cutThrust()
    {
        accelerating = false;
    }

    public void startFuelDrain()
    {
        drainingFuel = true;
    }

    public void fixFuelDrain()
    {
        drainingFuel = !drainingFuel;
        accelerating = false;
    }

}
