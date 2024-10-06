using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GrowthJoint : MonoBehaviour
{
    public BallJoint parentBallJoint;
    public float growthTime = 2f;
    public float growthMagnitude = 1f;
    public float flexibility = 15f;
    public float strength = 15f;

    private GameObject intermediateBall;
    private ArticulationBody intermediateArticulation;
    private ArticulationBody growthArticulationBody;
    private Vector3 initialAnchor;
    private bool growing;
    private float growthPercentage;

    void Start()
    {
        if (parentBallJoint != null && parentBallJoint.ball != null)
        {
            gameObject.name = "intermediate ball";
            gameObject.transform.parent = parentBallJoint.ball.transform;
            gameObject.transform.localPosition = Vector3.zero; // Centered with parent ball

            // Add ArticulationBody to intermediate ball
            intermediateArticulation = gameObject.AddComponent<ArticulationBody>();
            intermediateArticulation.jointType = ArticulationJointType.SphericalJoint;
            
            // Lock twist and configure swing flexibility for intermediate ball
            intermediateArticulation.twistLock = ArticulationDofLock.LockedMotion;
            intermediateArticulation.swingYLock = ArticulationDofLock.LimitedMotion;
            intermediateArticulation.swingZLock = ArticulationDofLock.LimitedMotion;
            intermediateArticulation.mass = .01f;

            // Set up the drive for flexibility
            ArticulationDrive swingDrive = new ArticulationDrive
            {
                lowerLimit = -flexibility,
                upperLimit = flexibility,
                stiffness = strength,
                forceLimit = 10000
            };
            intermediateArticulation.yDrive = swingDrive;
            intermediateArticulation.zDrive = swingDrive;

            // Step 2: Create the final growing joint as another spherical joint from the intermediate ball
            GameObject growthBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            growthBall.transform.parent = gameObject.transform;
            growthBall.transform.localPosition = Vector3.zero;

            growthArticulationBody = growthBall.AddComponent<ArticulationBody>();
            growthArticulationBody.jointType = ArticulationJointType.FixedJoint;
            growthArticulationBody.mass = .01f;

            initialAnchor = growthArticulationBody.anchorPosition;
            growing = false;
            growthPercentage = 0f;
        }
        else
        {
            Debug.LogError("GrowthJoint requires a valid parentBallJoint with a ball GameObject assigned.");
        }
    }
    public void StartGrowing()
    {
        growing = true;
        growthPercentage = 0f; // Reset growth when starting
    }

    void Update()
    {
        if (growing)
        {
            // Update growth progress
            growthPercentage += Time.deltaTime / growthTime;
            growthPercentage = Mathf.Clamp01(growthPercentage);

            // Calculate the growth offset based on the growth percentage
            Vector3 growthOffset = Vector3.up * growthMagnitude * growthPercentage;

            // Apply the growth offset directly to the local position
            growthArticulationBody.transform.localPosition = growthOffset;

            // Adjust anchor position to maintain initial alignment
            growthArticulationBody.anchorPosition = initialAnchor - growthOffset;

            // Stop growing when we reach 100%
            if (growthPercentage >= 1f)
            {
                growing = false;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GrowthJoint))]
    public class GrowthJointEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GrowthJoint growthJoint = (GrowthJoint)target;
            if (GUILayout.Button("Start Growing"))
            {
                growthJoint.StartGrowing();
            }
        }
    }
#endif
}
