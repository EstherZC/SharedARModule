using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
///  Responsible for the instantiation/deletion of shared prefabs and basic configurations of the room.
///  Last update: 25/10/2021
/// </summary>
namespace com.TFTEstherZC.SharedARModuleV2
{
    [RequireComponent(typeof(PhotonView))]
    public class RoomController : MonoBehaviourPunCallbacks
    {
        #region Definition
        [Header("Lobby Reference")]
        public int lobbyIndex;
        [Header("Common Physical Space Settings")]
        public bool sharedPhysicalSpace;
        
        private bool guide;
        private bool found;
        private int confirmedPlayers;
        private const string PackageSharedPrefabs = "SharedPrefabs";

        public static RoomController Instance;
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
            found = false;
            confirmedPlayers = 0;
            if (PhotonNetwork.IsMasterClient)
            {
                guide = true;
            }
            else
            {
                guide = false;
            }
            Hashtable defaultProperties = new Hashtable();
            defaultProperties.Add("PlacementInstantiate", false);
            defaultProperties.Add("PlacementChild", null);
            MyLocalPlayer().SetCustomProperties(defaultProperties);
        }
        #endregion

        #region PUN callbacks
        public override void OnLeftRoom()
        {
            PlacementManager.Instance.RemovePlacementIndicator();
            SceneManager.LoadScene(lobbyIndex);
            Destroy(gameObject);
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            CheckGuide();
        }

        #endregion

        #region Prefab instantiated
        public void InstantiatePrefabUsingIndicator(string tag, Quaternion rotation)
        {
            if (PlacementManager.Instance.ExistsAllPlacementIndicators() && PlacementManager.Instance.ConfirmAllDropChild())
            {
                Vector3 position = PlacementManager.Instance.ActualPlacementIndicator().transform.position;
                InstantiatePrefab(tag, position, rotation);
            }

        }
        void InstantiatePrefab(string tag, Vector3 position, Quaternion rotation)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                InstantiatedByMaster(tag, position, rotation);
            }
            else 
            {
                if (!guide && sharedPhysicalSpace)
                {
                    rotation = RotationWithoutGuideReferences(rotation);
                }
                object[] parameters = new object[] { tag, rotation };
                this.photonView.RPC("PreparingInstanceOfPrefabRPC", RpcTarget.MasterClient, parameters);
            }

        }
        Quaternion RotationWithoutGuideReferences(Quaternion rotation)
        {
            Quaternion guideOrientation = Quaternion.Euler(0, -PlacementManager.Instance.AngleOfGuideOrientation(), 0);
            return (rotation * Quaternion.Inverse(rotation) * guideOrientation * rotation);
        }
        void InstantiatedByMaster(string tag, Vector3 position, Quaternion initialRotation)
        {
            GameObject newPrefab = PhotonNetwork.InstantiateRoomObject(Path.Combine(PackageSharedPrefabs, tag), position, initialRotation, 0);
            if (newPrefab)
            {
                string actualPrefabName = RenameLastPrefab(tag);
                UpdateTransform(newPrefab);
                PlacementManager.Instance.UpdatePlacementProperties(true, actualPrefabName);
                if (!guide && sharedPhysicalSpace)
                {
                    initialRotation = RotationWithoutGuideReferences(initialRotation);
                }
                object[] parameters = new object[] { tag, initialRotation};
                this.photonView.RPC("ConfigureCommonPrefabRPC", RpcTarget.Others, parameters);

            }
            
        }
        void UpdateTransform(GameObject newPrefab)
        {
            newPrefab.transform.parent = PlacementManager.Instance.ActualPlacementIndicator().transform;
            newPrefab.transform.localPosition = Vector3.zero;
            newPrefab.transform.RotateAround(newPrefab.transform.position, Vector3.up, CurrentHorizontalRotationOfCamera());
        }
        [PunRPC]
        void PreparingInstanceOfPrefabRPC(object[] parameters)
        {
            string tag = (string)parameters[0];
            Vector3 position = PlacementManager.Instance.ActualPlacementIndicator().transform.position;
            Quaternion initialRotation = (Quaternion)parameters[1];
            if (!guide && sharedPhysicalSpace)
            {
                Quaternion guideOrientation = Quaternion.Euler(0, PlacementManager.Instance.AngleOfGuideOrientation(), 0);
                initialRotation = initialRotation * Quaternion.Inverse(initialRotation) * guideOrientation * initialRotation;
            }
            InstantiatedByMaster(tag, position, initialRotation);
        }
        [PunRPC]
        void ConfigureCommonPrefabRPC(object[] parameters)
        {
            string tag = (string)parameters[0];
            Quaternion initialRotation = (Quaternion)parameters[1];
            string actualPrefab = RenameLastPrefab(tag);
            UpdateActualPrefab(actualPrefab, initialRotation);
        }
        void UpdateActualPrefab(string prefabName, Quaternion initialRotation)
        {
            GameObject sharedPrefab = GameObject.Find(prefabName);
           
            if (sharedPrefab)
            {
                sharedPrefab.transform.rotation = initialRotation;
                sharedPrefab.transform.parent = PlacementManager.Instance.ActualPlacementIndicator().transform;
                sharedPrefab.transform.localPosition = Vector3.zero;
                if (!guide && sharedPhysicalSpace)
                {
                    float degrees = PlacementManager.Instance.AngleOfGuideOrientation();
                    sharedPrefab.transform.RotateAround(sharedPrefab.transform.position, Vector3.up, degrees);
                }
                sharedPrefab.transform.RotateAround(sharedPrefab.transform.position, Vector3.up, CurrentHorizontalRotationOfCamera());
                PlacementManager.Instance.UpdatePlacementProperties(true, prefabName);
            }
        }
        #endregion

        #region Camera settings
        public float CurrentHorizontalRotationOfCamera()
        {
            return Camera.current.transform.eulerAngles.y;
        }
        public Vector2 RealDirectionUsingCameraReference(Vector2 displacement)
        {
            float axisX = displacement.x;
            Vector3 myRealAxisX = axisX * Camera.current.transform.right;
            float axisZ = displacement.y;
            Vector3 myRealAxisZ = axisZ * Camera.current.transform.forward;
            return new Vector2(myRealAxisX.x + myRealAxisZ.x, myRealAxisX.z + myRealAxisZ.z);
        }
        #endregion

        #region Room settings
        public void CloseActualRoom()
        {
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        public void OpenActualRoom()
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
        }
        public void EnableSharedPhysicalSpace()
        {
            this.photonView.RPC("ChangedSharedPhysicalSpace", RpcTarget.AllBuffered, true);
        }
        public void DisableSharedPhysicalSpace()
        {
            this.photonView.RPC("ChangedSharedPhysicalSpace", RpcTarget.AllBuffered, false);
        }
        [PunRPC]
        void ChangedSharedPhysicalSpace(bool commonPhysicalSpace)
        {
            this.sharedPhysicalSpace = commonPhysicalSpace;
        }
        public void UpdatePlayerName(string name)
        {
            PhotonNetwork.LocalPlayer.NickName = name;
        }
        public void UpdatePlayerProperties(Hashtable customProperties)
        {
            Player myPlayer = MyLocalPlayer();
            myPlayer.SetCustomProperties(customProperties);
        }
        #endregion

        #region Room information
        public bool InsideRoom()
        {
            return PhotonNetwork.InRoom;
        }
        public byte MaxPlayerPerRoom()
        {
            return PhotonNetwork.CurrentRoom.MaxPlayers;
        }
        public List<Player> RoomPlayers()
        {
            List<Player> players = new List<Player>();
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                players.Add(player);
            }
            return players;

        }
        public Player MyLocalPlayer()
        {
            return PhotonNetwork.LocalPlayer;
        }
        public int PlayerCount()
        {
            return PhotonNetwork.CurrentRoom.PlayerCount;
        }
        public string RoomName()
        {
            return PhotonNetwork.CurrentRoom.Name;

        }
        public bool ArePrefabsInstantiated(string tag)
        {
            GameObject[] prefabs = GameObject.FindGameObjectsWithTag(tag);
            if (prefabs != null && prefabs.Length > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public int TotalPrefabsInstantiated(string tag)
        {
            GameObject[] prefabs = GameObject.FindGameObjectsWithTag(tag);
            if (prefabs != null )
            {
                return prefabs.Length;
            }
            else
            {
                return 0;
            }
        }
        public bool IsPrefabInstantiated(string name)
        {
            GameObject prefab = GameObject.Find(name);
            if (prefab)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public Hashtable PlayerProperties()
        {
            Player player = MyLocalPlayer();
            return player.CustomProperties;
        }
        #endregion

        #region Leave scenes
        public void LeaveRoom()
        {
            if (guide && !PhotonNetwork.IsMasterClient)
            {
                ChangeGuideToMasterClient();
            }
            PhotonNetwork.LeaveRoom(false);
        }
        #endregion

        #region Remove room's prefabs
        public void DeleteAllSpecificRoomPrefabs(string tag)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GameObject[] removePrefabs = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject prefab in removePrefabs)
                {
                    CheckAllPlacementChild(prefab.name);
                    prefab.transform.parent = null;
                    PhotonNetwork.Destroy(prefab);
                }
            }
            else 
            {
                this.photonView.RPC("DeleteAllPrefabsByMaster", RpcTarget.MasterClient, tag);
            }

        }
        [PunRPC]
        void DeleteAllPrefabsByMaster(string tag)
        {
            DeleteAllSpecificRoomPrefabs(tag);
        }
        public void DeleteSpecificRoomPrefab(string name)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GameObject removePrefab = GameObject.Find(name);
                if (removePrefab)
                {
                    CheckAllPlacementChild(name);
                    removePrefab.transform.parent = null;
                    PhotonNetwork.Destroy(removePrefab);
                }
            }
            else
            {
                this.photonView.RPC("DeletePrefabByMaster", RpcTarget.MasterClient, name);
            }

        }
        [PunRPC]
        void DeletePrefabByMaster(string name)
        {
            DeleteSpecificRoomPrefab(name);
        }
        void CheckAllPlacementChild(string name)
        {
            this.photonView.RPC("CheckPlacementChildRPC", RpcTarget.All, name);
        }
        [PunRPC]
        void CheckPlacementChildRPC(string name)
        {
            string child = (string)MyLocalPlayer().CustomProperties["PlacementChild"];
            bool existPlacement = PlacementManager.Instance.ActualPlacementIndicator();
            if (!string.IsNullOrEmpty(child) && child.Equals(name))
            {
                PlacementManager.Instance.UpdatePlacementProperties(existPlacement, null);
            }
                
        }
        #endregion

        #region Update the name of instantiated prefab
        string RenameLastPrefab(string tag)
        {
            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(tag);
            if (gameObjects.Length == 1)
            {
                gameObjects[0].name = tag;
                return tag;
            }
            else if (gameObjects.Length > 1)
            {
                GameObject newPrefab = GameObject.Find(tag + "(Clone)");
                int id = gameObjects.Length - 1;
                newPrefab.name = tag + " "+ id;
                return newPrefab.name;
            }
            return "";
        }
        #endregion

        #region Guide settings
        public bool IsGuide()
        {
            return guide;
        }
        public void ChangeGuideToCurrentPlayer()
        {
            guide = true;
            this.photonView.RPC("ChangedGuideRPC", RpcTarget.Others);
        }
        [PunRPC]
        void ChangedGuideRPC()
        {
            guide = false;
        }
        public void ChangeGuideToMasterClient()
        {
            this.photonView.RPC("ChangedGuideToMasterRPC", RpcTarget.MasterClient);
        }
        [PunRPC]
        void ChangedGuideToMasterRPC()
        {
            ChangeGuideToCurrentPlayer();
        }
        void CheckGuide()
        {
            found = false;
            confirmedPlayers = 0;
            if (!guide && PlayerCount()>1)
            {
                ++confirmedPlayers;
                this.photonView.RPC("CheckGuideRPC", RpcTarget.Others);
            }else if (!guide && PlayerCount() == 1)
            {
                ChangeGuideToCurrentPlayer();
            }
        }
        [PunRPC]
        void CheckGuideRPC()
        {
            this.photonView.RPC("ExistsGuideRPC", RpcTarget.MasterClient, guide);
        }
        [PunRPC]
        void ExistsGuideRPC(bool existsGuide)
        {
            ++confirmedPlayers;
            if (existsGuide && !found)
            {
                found = true;
            }
            else if (!found && confirmedPlayers == PlayerCount())
            {
                ChangeGuideToCurrentPlayer();
            }
        }
        #endregion
    }
}