using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
/// <summary>
///  Responsible for the instantiation/deletion of the placement indicator and its configurations.
///  Last update: 25/10/2021
/// </summary>
namespace com.TFTEstherZC.SharedARModuleV2
{
    [RequireComponent(typeof(PhotonView))]
    public class PlacementManager : MonoBehaviourPun
    {
        #region Definition
        [Header("Current AR manager")]
        public ARPlaneManager aRPlaneManager;
        public ARRaycastManager aRRaycastManager;
        [Header("Indicator Reference")]
        public GameObject placement;
        public Material placementGuide;
        public Material placementUser;

        public static PlacementManager Instance;

        private List<ARRaycastHit> hits;
        private GameObject actualIndicator;
        private ARPlane actualPlane;
        private bool enablePlacement;
        private Vector3 placementPosition;
        private Quaternion initialRotation;
        #endregion

        #region Initialization
        void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
        }

        void Start()
        {
            enablePlacement = true;
            hits = new List<ARRaycastHit>();
        }
        #endregion

        #region Plane detection and Placement instantiation
        void Update()
        {
            if (Input.touchCount > 0 && enablePlacement)
            {
                CheckDetectedPlane(Input.GetTouch(0).position);
            }
        }

        void CheckDetectedPlane(Vector2 touchPosition)
        {
            if (aRRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon) && !actualIndicator)
            {
                var actualHit = hits[0];
                if (IsPlaneEnable(actualHit))
                {
                    UpdatePlacement(actualHit.pose.position);
                    actualIndicator = Instantiate(placement, placementPosition, placement.transform.rotation);
                    if (actualIndicator)
                    {
                        actualIndicator.transform.RotateAround(actualIndicator.transform.position, Vector3.up, RoomController.Instance.CurrentHorizontalRotationOfCamera());
                        initialRotation = actualIndicator.transform.rotation;
                        aRPlaneManager.trackables.TryGetTrackable(actualHit.trackableId, out actualPlane);
                        UpdatePlacementProperties(true, null);
                    }
                }
            }
        }
        void UpdatePlacement(Vector3 hitPosition)
        {
            placementPosition = UpdatePosition(hitPosition);
            if (!RoomController.Instance.sharedPhysicalSpace || RoomController.Instance.IsGuide())
            {
                placement.transform.GetComponent<Renderer>().material = placementGuide;
            }
            else
            {
                placement.transform.GetComponent<Renderer>().material = placementUser;
            }
        }
        Vector3 UpdatePosition(Vector3 hitPosition)
        {
            hitPosition.y += 0.01f;
            return hitPosition;
        }

        bool IsPlaneEnable(ARRaycastHit hit)
        {
            ARPlane touchPlane;
            return aRPlaneManager.trackables.TryGetTrackable(hit.trackableId, out touchPlane) ? touchPlane.gameObject.activeSelf : false;
        }
        #endregion

        #region Plane settings
        public GameObject ActualPlacementIndicator()
        {
            return actualIndicator;
        }
        public void EnablePlaneDetection()
        {
            aRPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }
        public void DisablePlaneDetection()
        {
            aRPlaneManager.requestedDetectionMode = PlaneDetectionMode.None;
        }

        public ARPlane ActualPlacementPlane()
        {
            return actualPlane;
        }
        public void ShowOnlyPlacementPlane()
        {
            if (actualPlane)
            {
                actualPlane.gameObject.SetActive(true);
                foreach (ARPlane plane in aRPlaneManager.trackables)
                {
                    if (!plane.trackableId.Equals(actualPlane.trackableId))
                    {
                        plane.gameObject.SetActive(false);
                    }
                }
            }
        }
        public void HideAllDetectedPlanes()
        {
            foreach (ARPlane plane in aRPlaneManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
        public void ShowAllDetectedPlanes()
        {
            foreach (ARPlane plane in aRPlaneManager.trackables)
            {
                CheckPlaneAlignment(plane);
            }
        }
        void CheckPlaneAlignment(ARPlane plane)
        {
            if (!plane.alignment.Equals(PlaneAlignment.Vertical))
            {
                plane.gameObject.SetActive(true);
            }
        }
        #endregion

        #region Indicator settings
        public void UpdatePlacementProperties(bool placementInstantiate, object placementChild)
        {
            Hashtable customProperties = new Hashtable();
            customProperties.Add("PlacementInstantiate", placementInstantiate);
            customProperties.Add("PlacementChild", placementChild);
            RoomController.Instance.UpdatePlayerProperties(customProperties);
        }
        public void EnablePlacementInstantiated()
        {
            enablePlacement = true;
        }
        public void DisablePlacementInstantiated()
        {
            enablePlacement = false;
        }
        public void RemovePlacementIndicator()
        {
            if (actualIndicator)
            {
                RemoveChild();
                Destroy(actualIndicator);
                actualIndicator = null;
                actualPlane = null;
                UpdatePlacementProperties(false, null);
            }
        }
        public float AngleOfGuideOrientation()
        {
            if (actualIndicator)
            {
                Quaternion actualRotation = (Quaternion) actualIndicator.GetComponent<GuideOrientation>().AngleOfOrientation();
                return  (actualRotation.eulerAngles.y - initialRotation.eulerAngles.y);
            }
            else
            {
                return 0;
            }
        }
        public bool ExistsAllPlacementIndicators()
        {
            List<Player> players = RoomController.Instance.RoomPlayers();
            foreach (Player player in players)
            {
                bool existsPlacement = (bool)player.CustomProperties["PlacementInstantiate"];
                if (!existsPlacement)
                {
                    return false;
                }
            }
            return true;
        }
        public void UpdatePlacementScale(Vector3 updateScale)
        {
            this.photonView.RPC("UpdatePlacementScaleRPC", RpcTarget.Others, updateScale);
        }
        [PunRPC]
        void UpdatePlacementScaleRPC(Vector3 updateScale)
        {
            if (actualIndicator)
            {
                actualIndicator.transform.localScale = updateScale;
            }
        }
        #endregion

        #region Child settings
        public bool IsChild(string prefab)
        {
            return ExistsChild() && actualIndicator.transform.GetChild(0).name.Equals(prefab);
        }
        public bool ExistsChild()
        {
            return (actualIndicator) ? actualIndicator.transform.childCount > 0 : false;
        }
        public void RemoveChild()
        {
            if (ExistsChild())
            {
                Transform child = actualIndicator.transform.GetChild(0);
                RoomController.Instance.DeleteSpecificRoomPrefab(child.name);
                this.photonView.RPC("ChildRemovedRPC", RpcTarget.All);
            }
        }
        [PunRPC]
        void ChildRemovedRPC()
        {
            if (!ExistsChild())
            {
                UpdatePlacementProperties(true, null);
            }
        }
        public void DropChild()
        {
            if (actualIndicator && actualIndicator.transform.childCount == 1)
            {
                actualIndicator.transform.GetChild(0).parent = null;
                UpdatePlacementProperties(true, null);
            }
        }
        public bool ConfirmAllDropChildByName(string name)
        {
            List<Player> players = RoomController.Instance.RoomPlayers();
            foreach (Player player in players)
            {
                string child = (string)player.CustomProperties["PlacementChild"];
                if (!string.IsNullOrEmpty(child) && child.Equals(name))
                {
                    return false;
                }
            }
            return true;
        }
        public bool ConfirmAllDropChild()
        {
            List<Player> players = RoomController.Instance.RoomPlayers();
            foreach (Player player in players)
            {
                string child = (string)player.CustomProperties["PlacementChild"];
                if (!string.IsNullOrEmpty(child))
                {
                    return false;
                }
            }
            return true;
        }
        public bool ConfirmAllChild(string name)
        {
            List<Player> players = RoomController.Instance.RoomPlayers();
            foreach (Player player in players)
            {
                string child = (string)player.CustomProperties["PlacementChild"];
                if (string.IsNullOrEmpty(child) || !child.Equals(name))
                {
                    return false;
                }
            }
            return true;
        }
        public Transform ActualChild()
        {
            return (ExistsChild()) ? actualIndicator.transform.GetChild(0) : null;
        }
        #endregion
    }
}