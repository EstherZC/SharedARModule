using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
/// <summary>
///  Connecting to the Photon Master and preparing a new room (or an existing room) to join it.
///  Last update: 18/10/2021
/// </summary>
namespace com.TFTEstherZC.SharedARModuleV2
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        #region Definition
        [Header("Start Configuration")]
        public string sceneToLoad;
        public byte maxNameCharacters = 25;

        private string localPlayerName;
        private RoomSettings defaultRoomSettings;
        private bool isConnecting;
        private bool randomRoom;
        #endregion

        #region Initialization
        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

        }
        void Start()
        {
            localPlayerName = "Guest";
            defaultRoomSettings = new RoomSettings("", 2, true, true, true);
        }

        #endregion

        #region Pun callbacks
        public override void OnConnectedToMaster()
        {
            if (isConnecting)
            {
                JoinRoom();
                isConnecting = false;
            }
        }
        public override void OnDisconnected(DisconnectCause cause)
        {
            isConnecting = false;
        }

        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                PhotonNetwork.LoadLevel(sceneToLoad);
            }
        }
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            CreateRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            defaultRoomSettings.RoomName("");
            CreateRoom();
        }

        private void CreateRoom()
        {
            PhotonNetwork.CreateRoom(defaultRoomSettings.RoomName(), defaultRoomSettings.GenerateRoomOptions());
        }
        #endregion

        #region Checks names
        public bool CheckRoomName(string name)
        {
            return name.Length <= maxNameCharacters && !string.IsNullOrEmpty(name);
        }
        public bool CheckPlayerName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.Length <= maxNameCharacters;
        }
        #endregion

        #region Connecting and creating/joining the room
        public void JoinSpecificRoom(string roomName)
        {
            randomRoom = false;
            if (CheckRoomName(roomName))
            {
                defaultRoomSettings.RoomName(roomName);
                CheckConnectionAndJoin();
            }
        }
        public void JoinRandomRoom()
        {
            randomRoom = true;
            CheckConnectionAndJoin();
        }
        private void CheckConnectionAndJoin()
        {
            PhotonNetwork.NickName = localPlayerName;
            if (PhotonNetwork.IsConnected)
            {
                JoinRoom();
            }
            else
            {
                isConnecting = PhotonNetwork.ConnectUsingSettings();
            }
        }
        private void JoinRoom()
        {
            if (randomRoom)
            {
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                PhotonNetwork.JoinRoom(defaultRoomSettings.RoomName());
            }
        }
        public void CloseConnection()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }
        #endregion

        #region Room settings
        public bool IsConnected()
        {
            return PhotonNetwork.IsConnected;
        }
        public void UpdateSceneToLoad(string sceneToLoad)
        {
            this.sceneToLoad = sceneToLoad;
        }
        public void UpdateMaxNameCharacters(byte characteres)
        {
            if (characteres > 0)
            {
                maxNameCharacters = characteres;
            }
        }
        public void UpdateDefaultRoomSettings(RoomSettings roomSettings)
        {
            this.defaultRoomSettings = roomSettings;
        }
        public void UpdatePlayerName(string name)
        {
            if (CheckPlayerName(name))
            {
                localPlayerName = name;
            }
        }
        #endregion
    }
}