using DilmerGames.Core.Singletons;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

public class UnityEventResolver : UnityEvent<Transform>{}
// this to communicate with the ARPlacementManager
// to re-create the placement of the object
// UnityEvent<Transform> is the type of the event
// Transform is the type of the parameter
 
public class ARCloudAnchorManager : Singleton<ARCloudAnchorManager>
{
    [SerializeField]

    // to know how accurate is the anchor if I have a good capture of the anchor
    private Camera arCamera = null;

    [SerializeField]
    // to wait 10 second before trying to make a new call 
    // it may have call from Google API so we wait 10 seconds
    // or it can be 5 seconds

    private float resolveAnchorPassedTimeout = 10.0f;
   // to create anchor   
    private ARAnchorManager arAnchorManager = null;
    // the anchor going to be queueing
    // only allow one anchor at a time
    // but it can be changed to have multiple anchors
    // it is a reference as soon as the placement manager create a character then update the reference, and use this value to host it to the cloud
    private ARAnchor pendingHostAnchor = null;

 // the reference of the cloud anchor from calling google api
    private ARCloudAnchor cloudAnchor = null;
// this is the id of the anchor to be resolved
    private string anchorToResolve;
// it is the reference to determine to host an anchor or resolve it
    private bool anchorUpdateInProgress = false;

    private bool anchorResolveInProgress = false;
    // a timer to check when resolveAnchorPassedTimeout is passed, then make a new call, so it is to keep track of time
    private float safeToResolvePassed = 0;
// it mentioned before
    private UnityEventResolver resolver = null;

    private void Awake() 
    {// it is the method right now creating a new one of those resolver
    //  UnityEventResolver can also be called AnchorCreatedEvent in this context
    // there is a listener, going to be the transform calling the AR Placemennt Manager using a singleton and then recreate the placement based on transform 

        resolver = new UnityEventResolver();   
        resolver.AddListener((t) => ARPlacementManager.Instance.ReCreatePlacement(t));
    }
// camera pose 
// arcore provide the faature that quality of feature map is accurate 
// when the camera rotate around the anchor then the map is created to make sure it is in high quality, the below code is copied from another project 
// we get a pose and it is the position of the camera and the rotation of the camera 
    private Pose GetCameraPose()
    {
        return new Pose(arCamera.transform.position,
            arCamera.transform.rotation);
    }
 
#region Anchor Cycle
    // this is to add a reference 
    public void QueueAnchor(ARAnchor arAnchor)
    { // pass the reference to the anchor 
        pendingHostAnchor = arAnchor;
    }
// this is  to communicate with Google 
// to check is that completed that's what this one is going to be doing
    public void HostAnchor() // this is the button HOST
    {
        ARDebugManager.Instance.LogInfo($"HostAnchor executing");
// this comes from Google.XR.ARCoreExtension, advise you to hold 30 seconds before to host it 
// it is good to make the experience more intuitive by providing the UI to accommodate the 30 seconds, maybe 10 seconds is good enough
        FeatureMapQuality quality =
            arAnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose());

        // we are using the pendingHostAnchor as the anchor
        // for anchor to live up to 365 days, we type 365

        cloudAnchor = arAnchorManager.HostCloudAnchor(pendingHostAnchor, 1);
    

        // we should get a reference

        // if there is error

        if(cloudAnchor == null)
        {
            ARDebugManager.Instance.LogError("Unable to host cloud anchor");
        }
        else

    
        {
            anchorUpdateInProgress = true;
        }
    }
    

    // this is to map the area and then resolve it. 
    // then know the completion of the result   
    public void Resolve()
    {
        ARDebugManager.Instance.LogInfo("Resolve executing");
    // passing the anchor id to resolve
    
        cloudAnchor = arAnchorManager.ResolveCloudAnchorId(anchorToResolve);

        if(cloudAnchor == null)
        {
            ARDebugManager.Instance.LogError($"Failed to resolve cloud achor id {cloudAnchor.cloudAnchorId}");
        }
        else
        {
            anchorResolveInProgress = true;
        }
    }


// this is to check, then we save the anchor  
    private void CheckHostingProgress()
    {

        // to have a state for checking 

        CloudAnchorState cloudAnchorState = cloudAnchor.cloudAnchorState;
        if(cloudAnchorState == CloudAnchorState.Success)
        {
            // host successfully
            // 
            ARDebugManager.Instance.LogError("Anchor successfully hosted");
            // it is not in progress, so it is false
            anchorUpdateInProgress = false;
            // to ache which anchor to be resolved next
            // keep track of cloud anchors added
            anchorToResolve = cloudAnchor.cloudAnchorId;
             
        }

         // if it is not successful, then put text with the state for trouble shooting
        
        else if(cloudAnchorState != CloudAnchorState.TaskInProgress)
        {
            ARDebugManager.Instance.LogError($"Fail to host anchor with state: {cloudAnchorState}");

// include this as a whole 

            anchorUpdateInProgress = false;
        }
    }

    private void CheckResolveProgress()
    {
        CloudAnchorState cloudAnchorState = cloudAnchor.cloudAnchorState;
        
        ARDebugManager.Instance.LogInfo($"ResolveCloudAnchor state {cloudAnchorState}");

        if (cloudAnchorState == CloudAnchorState.Success)
        {
            ARDebugManager.Instance.LogInfo($"CloudAnchorId: {cloudAnchor.cloudAnchorId} resolved");

            resolver.Invoke(cloudAnchor.transform);

            anchorResolveInProgress = false;
        }
        else if (cloudAnchorState != CloudAnchorState.TaskInProgress)
        {
            ARDebugManager.Instance.LogError($"Fail to resolve Cloud Anchor with state: {cloudAnchorState}");

            anchorResolveInProgress = false;
        }
    }

#endregion

    void Update()
    {
        // check progress of new anchors created

        // if the host result is true
        // we are going to host sth 

        // we are not hosting but resolving at that point 
        if(anchorUpdateInProgress)
        {
            CheckHostingProgress();
            return;
        }
        // if in progress, then need to resolve it and check for the timer to save to result is less than or equal to 0, that means the timer is going from a high number to a long number if this is less than or equal to zero 
        if(anchorResolveInProgress && safeToResolvePassed <= 0)
        {
            // check evey (resolveAnchorPassedTimeout)

            // going to make a new call 
            safeToResolvePassed = resolveAnchorPassedTimeout;
            // as long as the Id t resolve is not null or empty, then check for the result progress
            //  
            if(!string.IsNullOrEmpty(anchorToResolve))
            {
                ARDebugManager.Instance.LogInfo($"Resolving AnchorId: {anchorToResolve}");
                CheckResolveProgress();
            }
        }
        else

        // if it is not true 
        // decrementing the timer
        // save to resolved passed to minus the timer 
        //grab the time delta  multiply it by 1 
        {
            safeToResolvePassed -= Time.deltaTime * 1.0f;
        }
    }
}
