
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if WINDOWS_UWP
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Input;
using Windows.Graphics.Holographic;
using Windows.Perception.People;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using Windows.Perception;
#endif
public class DebugRenderHand : MonoBehaviour
{
    List<InputDevice> hands = new List<InputDevice>();

    [SerializeField]
    LayerMask graspableObjectMask;

    // TODO: we should auto generate this...
    // TODO we could also move to [handedness]
    [SerializeField]
    GameObject[] rightHandObjects = new GameObject[26];

    [SerializeField]
    GameObject[] leftHandObjects = new GameObject[26];

#if WINDOWS_UWP
    JointPose[] jointPoses = new JointPose[26];

    private static readonly HandJointKind[] jointIndices = new HandJointKind[]
    {
        HandJointKind.Palm,
        HandJointKind.Wrist,
        HandJointKind.ThumbMetacarpal,
        HandJointKind.ThumbProximal,
        HandJointKind.ThumbDistal,
        HandJointKind.ThumbTip,
        HandJointKind.IndexMetacarpal,
        HandJointKind.IndexProximal,
        HandJointKind.IndexIntermediate,
        HandJointKind.IndexDistal,
        HandJointKind.IndexTip,
        HandJointKind.MiddleMetacarpal,
        HandJointKind.MiddleProximal,
        HandJointKind.MiddleIntermediate,
        HandJointKind.MiddleDistal,
        HandJointKind.MiddleTip,
        HandJointKind.RingMetacarpal,
        HandJointKind.RingProximal,
        HandJointKind.RingIntermediate,
        HandJointKind.RingDistal,
        HandJointKind.RingTip,
        HandJointKind.LittleMetacarpal,
        HandJointKind.LittleProximal,
        HandJointKind.LittleIntermediate,
        HandJointKind.LittleDistal,
        HandJointKind.LittleTip
    };

    private SpatialInteractionManager SpatialInteractionManager
    {
        get
        {
            if (spatialInteractionManager == null)
            {
                UnityEngine.WSA.Application.InvokeOnUIThread(() =>
                {
                    spatialInteractionManager = SpatialInteractionManager.GetForCurrentView();
                }, true);
            }

            return spatialInteractionManager;
        }
    }

    private SpatialInteractionManager spatialInteractionManager = null;


    private HolographicSpace MyHolographicSpace
    {
        get
        {
            if (holographicSpace == null)
            {
                UnityEngine.WSA.Application.InvokeOnUIThread(() =>
                {
                    holographicSpace = HolographicSpace.CreateForCoreWindow(Windows.UI.Core.CoreWindow.GetForCurrentThread());
                }, true);
            }

            return holographicSpace;
        }
    }

    private HolographicSpace holographicSpace = null;
#endif // WINDOWS_UWP

#if WINDOWS_UWP
    public static UnityEngine.Vector3 SystemVector3ToUnity(System.Numerics.Vector3 vector)
    {
        return new UnityEngine.Vector3(vector.X, vector.Y, -vector.Z);
    }

    public static UnityEngine.Quaternion SystemQuaternionToUnity(System.Numerics.Quaternion quaternion)
    {
        return new UnityEngine.Quaternion(-quaternion.X, -quaternion.Y, quaternion.Z, quaternion.W);
    }

#endif

    // TODO: which is the smoothest experience?  FixedUpdate for physics, or LateUpdate/Prerender for more accurate onscreen representation
    // (or if we move away from physics, then see if LateUpdate is just better.
    private void FixedUpdate()
    {
#if WINDOWS_UWP
        PerceptionTimestamp perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(
            DateTimeOffset.Now + TimeSpan.FromSeconds(Time.fixedDeltaTime * 3.0f));

        if (!UpdateAtTime(perceptionTimestamp))
        {
            // prediction failed, fall back to current timestamp
            UpdateAtTime(PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now));
        }
#endif

    }
#if WINDOWS_UWP
    int HandednessToIndex(SpatialInteractionSourceHandedness handedness)
    {
        return handedness == SpatialInteractionSourceHandedness.Right ? 1 : 0;
    }

    private void UpdateHand(HandPose handPose, SpatialInteractionSourceHandedness handedness)
    {
        var handIdx = HandednessToIndex(handedness);
        var jointArray = handedness == SpatialInteractionSourceHandedness.Right ? rightHandObjects : leftHandObjects;
        var coordSys = SpatialCoordinateSystemToUnityUtilities.UnitySpatialCoordinateSystem;
        if (handPose.TryGetJoints(coordSys, jointIndices, jointPoses))
        {
            handVelocity[handIdx] = jointArray[0].transform.position - SystemVector3ToUnity(jointPoses[0].Position);
            for (int i = 0; i < jointPoses.Length; ++i)
            {
                jointArray[i].SetActive(true);
                var rb = jointArray[i].GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.MovePosition(SystemVector3ToUnity(jointPoses[i].Position));
                    rb.MoveRotation(SystemQuaternionToUnity(jointPoses[i].Orientation).normalized);
                }
                else
                {
                    jointArray[i].transform.SetPositionAndRotation(
                        SystemVector3ToUnity(jointPoses[i].Position),
                        SystemQuaternionToUnity(jointPoses[i].Orientation));
                }
            }

        }
    }

    private bool UpdateAtTime(PerceptionTimestamp timestamp)
    {
        IReadOnlyList<SpatialInteractionSourceState> sources = SpatialInteractionManager?.GetDetectedSourcesAtTimestamp(timestamp);

        bool leftHandFound = false;
        bool rightHandFound = false;

        foreach (var source in sources)
        {
            if (source.Source.Kind == SpatialInteractionSourceKind.Hand)
            {
                var handPose = source.TryGetHandPose();
                if (handPose != null)
                {
                    int handIdx = HandednessToIndex(source.Source.Handedness);

                    if (source.Source.Handedness == SpatialInteractionSourceHandedness.Right)
                    {
                        rightHandFound = true;
                    }
                    else if (source.Source.Handedness == SpatialInteractionSourceHandedness.Left)
                    {
                        leftHandFound = true;
                    }

                    UpdateHand(handPose, source.Source.Handedness);
                }
            }
        }

        if (!leftHandFound)
        {
            foreach (var go in leftHandObjects)
            {
                go.SetActive(false);
            }
        }

        if (!rightHandFound)
        {
            foreach (var go in rightHandObjects)
            {
                go.SetActive(false);
            }
        }

        return rightHandFound || leftHandFound;
    }
#endif
   
}
