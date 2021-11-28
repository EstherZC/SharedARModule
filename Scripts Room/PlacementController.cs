using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Last update: 28/10/2021
/// </summary>
namespace com.TFTEstherZC.SharedARModuleV2 
{
    public enum TypeOfAction
    {
        Rotate, Move, Scale
    }

    public class PlacementController : MonoBehaviour, GuideOrientation
    {
        #region Definition
        [Header("Configuration parameters")]
        public float speedOfRotation = 5;
        public float speedOfMovement = 0.0004f;
        public TypeOfAction initialAction;
        [Header("Scale configuration")]
        public float scaleUnit = 0.1f;
        public float maxScale = 5;
        public float minScale = 0.3f;

        private bool movePrefab;
        private bool scalePrefab;
        private float initialDistance;
        private readonly Dictionary<TouchPhase, Action<Touch>> phasesOfAction = new Dictionary<TouchPhase, Action<Touch>>();
        #endregion

        #region Initialization
        void Awake()
        {
            phasesOfAction.Add(TouchPhase.Began, GetHitPrefab);
            phasesOfAction.Add(TouchPhase.Moved, StartAction);
        }
        void Start()
        {
            gameObject.name = gameObject.name.Replace("(Clone)", "");
            movePrefab = false;
            scalePrefab = false;
        }

        #endregion

        #region Checking UI actions
        void Update()
        {
            if (Input.touchCount == 1)
            {
                MovePlacement();
            }
            else if (Input.touchCount == 2)
            {
                RotatePlacement();
            }
        }
        void MovePlacement()
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                movePrefab = false;
            }
            else
            {
                phasesOfAction[touch.phase](touch);
            }
        }
        void RotatePlacement()
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);
            if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
            {
                scalePrefab = false;
            }
            else
            {
                CheckTouchesForScale(touch1, touch2);

            }
        }
        void CheckTouchesForScale(Touch touch1, Touch touch2)
        {
            if (touch1.phase == TouchPhase.Began && touch2.phase == TouchPhase.Began)
            {
                if (TouchMyPrefab(touch1.position) && TouchMyPrefab(touch2.position) && CheckPlacementSettings())
                {
                    scalePrefab = true;
                    initialDistance = DistanceBetweenTwoPoints(touch1.position, touch2.position);
                }
            }
            else if (scalePrefab && touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                StartScale(touch1.position, touch2.position);
            }
        }
        bool CheckPlacementSettings()
        {
            return (PlacementManager.Instance.ExistsChild() && PlacementManager.Instance.ConfirmAllChild(PlacementManager.Instance.ActualChild().name) && RoomController.Instance.IsGuide())
            || (PlacementManager.Instance.ExistsChild() && !RoomController.Instance.sharedPhysicalSpace);
        }
        #endregion

        #region Check UI touches 
        void GetHitPrefab(Touch actualTouch)
        {
            if (TouchMyPrefab(actualTouch.position))
            {
                movePrefab = true;
            }
        }
        bool TouchMyPrefab(Vector2 touchPosition)
        {
            Ray ray = Camera.current.ScreenPointToRay(touchPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.name.Equals(gameObject.name))
                {
                    return true;
                }
            }
            return false;
        }
        void StartAction(Touch actualTouch)
        {
            if (movePrefab && initialAction.Equals(TypeOfAction.Rotate))
            {
                StartRotation(actualTouch.deltaPosition);
            }
            else if (movePrefab && initialAction.Equals(TypeOfAction.Move))
            {
                StartMovement(actualTouch.deltaPosition);
            }
        }
        #endregion

        #region Placement rotation
        void StartRotation(Vector2 screenPosition)
        {
            if (PlacementManager.Instance.ExistsChild() && !RoomController.Instance.sharedPhysicalSpace || !PlacementManager.Instance.ExistsChild())
            {
                float angle = screenPosition.x * speedOfRotation * Mathf.Deg2Rad;
                transform.RotateAround(transform.position, Vector3.up, angle);
            }
        }
        #endregion

        #region Placement movement
        void StartMovement(Vector2 screenPosition)
        {
            Vector2 position = RoomController.Instance.RealDirectionUsingCameraReference(screenPosition);
            transform.position = new Vector3(transform.position.x + position.x * speedOfMovement, transform.position.y, transform.position.z + position.y * speedOfMovement);
        }
        #endregion

        #region Placement scale
        void StartScale(Vector2 touch1, Vector2 touch2)
        {
            float actualDistance = DistanceBetweenTwoPoints(touch1, touch2);
            float factor = scaleUnit;
            if (actualDistance < initialDistance)
            {
                factor = -scaleUnit;
            }
            Vector3 actualScale = new Vector3(transform.localScale.x + factor, transform.localScale.y + factor, transform.localScale.z + factor);
            if (ValidScale(actualScale))
            {
                UpdateScale(actualScale);
            }

        }
        void UpdateScale(Vector3 actualScale)
        {
            transform.localScale = actualScale;
            if (RoomController.Instance.sharedPhysicalSpace)
            {
                PlacementManager.Instance.UpdatePlacementScale(transform.localScale);
            }
        }
        bool ValidScale(Vector3 actualScale)
        {
            return actualScale.x >= minScale && actualScale.y >= minScale && actualScale.z >= minScale && actualScale.x <= maxScale && actualScale.y <= maxScale && actualScale.z <= maxScale;
        }
        float DistanceBetweenTwoPoints(Vector2 point1, Vector2 point2)
        {
            float a = point2.x - point1.x;
            float b = point2.y - point1.y;
            float res = (a * a) + (b * b);
            return (float)Math.Sqrt(res);
        }
        #endregion

        #region Local orientation for the guide
        public object AngleOfOrientation()
        {
            return transform.rotation;
        }
        #endregion
    }
}
