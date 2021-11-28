using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
///  Connecting to the Photon Master and preparing a new room (or an existing room) to join it.
///  Last update: 18/10/2021
/// </summary>
namespace com.TFTEstherZC.SharedARModuleV2
{
    public class RoomSettings 
    {
        #region Definition
        string roomName;
        byte maxPlayers;
        bool isVisible;
        bool isOpen;
        bool publishUserID;
        #endregion

        public RoomSettings(string roomName, byte maxPlayers, bool isVisible, bool isOpen, bool publishUserID)
        {
            this.roomName = roomName;
            this.maxPlayers = maxPlayers;
            this.isVisible = isVisible;
            this.isOpen = isOpen;
            this.publishUserID = publishUserID;
        }


        #region Room information
        public string RoomName()
        {
            return roomName;
        }
        public byte MaxPlayers()
        {
            return maxPlayers;
        }
        public bool IsVisible()
        {
            return isVisible;
        }
        public bool IsOpen()
        {
            return isOpen;
        }
        public bool IsEnablePublishUserID()
        {
            return publishUserID;
        }
        public void RoomName(string roomName)
        {
            this.roomName = roomName;
        }
        public void MaxPlayers(byte maxPlayers)
        {
            this.maxPlayers = maxPlayers;
        }
        public void VisibleRoom(bool isVisible)
        {
            this.isVisible = isVisible;
        }
        public void OpenRoom(bool isOpen)
        {
            this.isOpen = isOpen;
        }
        public void EnablePublishUserID(bool publishUserID)
        {
            this.publishUserID = publishUserID;
        }
        #endregion

        public RoomOptions GenerateRoomOptions()
        {
            return new RoomOptions() { MaxPlayers = maxPlayers, IsOpen = isOpen, IsVisible = isVisible, PublishUserId = publishUserID };
        }
    }
}