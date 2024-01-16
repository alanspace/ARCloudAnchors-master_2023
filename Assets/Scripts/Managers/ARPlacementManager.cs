using System.Collections.Generic;
using DilmerGames.Core.Singletons;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARRaycastManager))]
public class ARPlacementManager : Singleton<ARPlacementManager>
{ 
    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private GameObject placedPrefab = null;

    private GameObject placedGameObject = null;

    private ARRaycastManager arRaycastManager = null;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private ARAnchorManager arAnchorManager = null;

    void Awake() 
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        arAnchorManager = GetComponent<ARAnchorManager>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if(Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);

            if(touch.phase == TouchPhase.Began)
            {
                touchPosition = touch.position;

                bool isOverUI = touchPosition.IsPointOverUIObject();

                return isOverUI ? false : true;
            }
        }

        touchPosition = default;

        return false;
    }

    public void RemovePlacements()
    {
        Destroy(placedGameObject);
        placedGameObject = null;
    }

    void Update()
    {

        // this part is to instantiate a new object, to be the character, then going to get the reference, be a game object, to create a new anchor, which pass the hits.pos position and hit pos.rotation 
        if(!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if(placedGameObject != null)
            return;

        if(arRaycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            var hitPose = hits[0].pose;
            placedGameObject = Instantiate(placedPrefab, hitPose.position, hitPose.rotation);


            // this we have an anchor with the game object that is our character 
            // so now that I have created a character with an anchor so we need to queue it 
            // it is just a reference, a step before to host it 

            var anchor = arAnchorManager.AddAnchor(new Pose(hitPose.position, hitPose.rotation));
            placedGameObject.transform.parent = anchor.transform;

            // this won't host the anchor just add a reference to be later host it

            // this is implemented in the AR Cloud Anchor Manager, pass this to the anchor later
            
            ARCloudAnchorManager.Instance.QueueAnchor(anchor);
        }
    }

    public void ReCreatePlacement(Transform transform)
    {
        placedGameObject = Instantiate(placedPrefab, transform.position, transform.rotation);
        placedGameObject.transform.parent = transform;
    }
}