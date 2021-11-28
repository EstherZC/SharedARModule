using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
/// <summary>
/// Last update: 12/11/2021
/// </summary>

namespace com.TFTEstherZC.SharedARModuleV2
{
    [RequireComponent(typeof(PhotonView))]
    public class PrefabManager : MonoBehaviourPun
    {
        #region Definition
        [Header("Highlight  Reference")]
        public GameObject selectionSignal;
        public float positionAxisY;

        private bool isSelectedByAnother;
        private bool myPrefabSelected;
        private int playersTouch;
        private GameObject currentSelected;
        #endregion

        #region Initialization
        void Start()
        {
            isSelectedByAnother = false;
            myPrefabSelected = false;
            playersTouch = 0;
        }
        #endregion

        private void Update()
        {
            if (currentSelected)
            {
                UpdateHighlightPosition();
            }
        }

        #region Highlight settings
        public void EnableSelection()
        {
            if (!myPrefabSelected)
            {
                myPrefabSelected = true;
                this.photonView.RPC("PrefabSelectedRPC", RpcTarget.Others, myPrefabSelected);
            }
        }
        public void DisableSelection()
        {
            if (myPrefabSelected)
            {
                myPrefabSelected = false;
                this.photonView.RPC("PrefabSelectedRPC", RpcTarget.Others, myPrefabSelected);
            }
        }
        [PunRPC]
        void PrefabSelectedRPC(bool isSelected)
        {
            isSelectedByAnother = isSelected;
        }
        public bool IsSelectedByAnother()
        {
            return isSelectedByAnother;
        }
        public bool IsSelected()
        {
            return myPrefabSelected;
        }
        void UpdateHighlightPosition()
        {
            Vector3 actualPosition = transform.position;
            actualPosition.y += positionAxisY;
            currentSelected.transform.localPosition = actualPosition;
        }
        public void EnableSelectionWithHighlight()
        {
            if (!myPrefabSelected)
            {
                myPrefabSelected = true;
                ++playersTouch;
                CheckAddHighlight();
                this.photonView.RPC("UpdatedAddHighlightRPC", RpcTarget.Others);
            }

        }
        public void DisableSelectionWithHighlight()
        {
            if (myPrefabSelected)
            {
                myPrefabSelected = false;
                --playersTouch;
                CheckRemoveHighlight();
                this.photonView.RPC("UpdateRemoveHighlightRPC", RpcTarget.Others);
            }

        }
        void CheckAddHighlight()
        {
            if (!currentSelected)
            {
                currentSelected = Instantiate(selectionSignal, Vector3.zero, Quaternion.identity);
                currentSelected.transform.localPosition = new Vector3(transform.position.x, positionAxisY + transform.position.y, transform.position.z);
                currentSelected.GetComponent<HighlighterManager>().SetPrefabSelected(this.gameObject);
            }
        }
        void CheckRemoveHighlight()
        {
            if (playersTouch == 0)
            {
                Destroy(currentSelected);
            }
        }
        [PunRPC]
        void UpdatedAddHighlightRPC()
        {
            ++playersTouch;
            isSelectedByAnother = true;
            CheckAddHighlight();
        }
        [PunRPC]
        void UpdateRemoveHighlightRPC()
        {
            --playersTouch;
            if (playersTouch == 0 || playersTouch == 1 && myPrefabSelected) isSelectedByAnother = false;
            CheckRemoveHighlight();
        }
        #endregion

        #region Prefab synchronization 
        public void ChangePrefabTransform(Transform localTransform)
        {
            if (PlacementManager.Instance.ConfirmAllDropChildByName(gameObject.name))
            {
                object[] localTRansform = new object[] { localTransform.localPosition, localTransform.localRotation };
                this.photonView.RPC("UpdateTransformPrefabRPC", RpcTarget.Others, localTRansform);
            }
        }
        public void ChangePrefabPosition(Vector3 localPosition)
        {
            if (PlacementManager.Instance.ConfirmAllDropChildByName(gameObject.name))
            {
                this.photonView.RPC("MovePrefabRPC", RpcTarget.All, localPosition);
            }
        }
        public void ChangePrefabRotation(Quaternion localRotation)
        {
            if (PlacementManager.Instance.ConfirmAllDropChildByName(gameObject.name))
            {
                this.photonView.RPC("RotatePrefabRPC", RpcTarget.All, localRotation);
            }
        }
        public void ChangePrefabScale(float factor)
        {
            if (PlacementManager.Instance.ConfirmAllDropChildByName(gameObject.name))
            {
                this.photonView.RPC("ScalePrefabRPC", RpcTarget.All, factor);
            }
        }
        #endregion

        #region Synchronization RPC
        [PunRPC]
        void UpdateTransformPrefabRPC(object[] localTransform)
        {
            GetComponent<TransformSynchronization>().UpdateTransform(localTransform);
        }
        [PunRPC]
        void MovePrefabRPC(Vector3 localPosition)
        {
            GetComponent<PositionSynchronization>().UpdatePosition(localPosition);
        }
        [PunRPC]
        void RotatePrefabRPC(Quaternion localRotation)
        {
            GetComponent<RotationSynchronization>().UpdateRotation(localRotation);
        }
        [PunRPC]
        void ScalePrefabRPC(float factor)
        {
            GetComponent<ScaleSynchronization>().UpdateScale(factor);
        }
        #endregion
    }
}