﻿using System.Collections.Generic;
using Runtime.ScriptableObjects;
using Runtime.StageDataObjects;
using Runtime.StageEditingObjects;
using Runtime.SubfunctionObject;
using Runtime.Testing;
using Runtime.UserInterface;
using UnityEngine;
using UnityEngine.Serialization;

namespace Runtime.Managers
{
    public enum LayerZ
    {
        Background = -10,
        Scenery = -9,

        StageBack = -1,
        StageCenter = 0,
        StageFront = 1,
        
        Foreground = 9,
    }
    
    /// <summary>
    /// Stage Editing Functionalities:
    /// - Instantiate stage prop objects from the Storage (and delete the Storage's Item)
    /// - Delete Stage prop objects from the stage (and add the object to the Storage, the decorators should be kept)
    /// - Manage the sub-buttons of stage prop objects
    /// </summary>
    public class StageEditingManager : MonoBehaviour
    {
        #region Singleton

        public static StageEditingManager Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<StageEditingManager>();
                }

                if (!_instance)
                {
                    Debug.LogError("No Stage Editing Manager Exist, Please check the scene.");
                }

                return _instance;
            }
        }
        private static StageEditingManager _instance;

        #endregion

        // Template Prefabs
        [FormerlySerializedAs("defaultStageObjectPrefab")] 
        [SerializeField] private GameObject defaultStagePropObjectPrefab;
        [SerializeField] private GameObject defaultStageEffectObjectPrefab;
        [SerializeField] private GameObject defaultStageSceneryObjectPrefab;
        
        [SerializeField] private List<GameObject> functionButtonPrefabs;
        private List<BaseStageObject> _stageObjectsInstantiated = new List<BaseStageObject>();

        private List<BaseStagePropSubfunctionButtonObject> _stagePropfunctionButtons = new List<BaseStagePropSubfunctionButtonObject>();
        [SerializeField] private StageEditingUI stageEditingUI;

        private bool _canEditStageProp = true;
        
        private bool _shouldOpenStageEditingUI = true;
        private bool _shouldOpenPropProducingUI = true;

        #region Unity Events

        private void Awake()
        {
            _instance = this;
        }

        private void OnEnable()
        {
            if (_stagePropfunctionButtons.Count == 0)
            {
                for (int i = 0; i < functionButtonPrefabs.Count; i++)
                {
                    _stagePropfunctionButtons.Add( 
                        Instantiate(functionButtonPrefabs[i], Vector3.zero, functionButtonPrefabs[i].transform.rotation).
                            GetComponent<BaseStagePropSubfunctionButtonObject>());
                    // disable this for now
                    _stagePropfunctionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        #endregion

        public void ChangeStageEditPermission(bool canEdit)
        {
            this._canEditStageProp = canEdit;
        }

        // todo:: instantiate props, actors and orchestras should be different.
        public void InstantiateNewPropToStage(BaseStageObjectData objectData, Vector2 positionXY)
        {
            var stageObj = Instantiate(defaultStagePropObjectPrefab,
                new Vector3(positionXY.x, positionXY.y, (int)LayerZ.StageCenter),
                defaultStagePropObjectPrefab.transform.rotation).GetComponent<StagePropObject>();
            stageObj.InitializeFromStageObjectData(objectData);
            _stageObjectsInstantiated.Add(stageObj);
        }

        public void InstantiateNewActorToStage(BaseStageObjectData objectData, Vector2 positionXY)
        {

        }
        
        public void InstantiateNewOrchestraToStage(BaseStageObjectData objectData, Vector2 positionXY)
        {
            
        }

        public void InstantiateNewEffectToStage(BaseStageObjectData objectData, Vector2 positionXY)
        {
            var stageObj = Instantiate(defaultStageEffectObjectPrefab,
                new Vector3(positionXY.x, positionXY.y, (int)LayerZ.StageCenter),
                defaultStageEffectObjectPrefab.transform.rotation).GetComponent<StageEffectObject>();
            stageObj.InitializeFromStageObjectData(objectData);
            _stageObjectsInstantiated.Add(stageObj);
        }

        public void InstantiateNewSceneryToStage(BaseStageObjectData objectData, Vector2 positionXY)
        {
            var stageObj = Instantiate(defaultStageSceneryObjectPrefab,
                new Vector3(positionXY.x, positionXY.y, (int)LayerZ.StageCenter),
                defaultStageSceneryObjectPrefab.transform.rotation).GetComponent<StageSceneryObject>();
            stageObj.InitializeFromStageObjectData(objectData);
            _stageObjectsInstantiated.Add(stageObj);
        }

        public void PutPropFromStageToStorage(BaseStageObject propObject)
        {
            _stageObjectsInstantiated.Remove(propObject);
            StorageManager.Instance.AddStageObjectData(propObject.StageObjectData);
            Destroy(propObject.gameObject);
        }

        public void EditingStageObject(BaseStageObject propObject, float buttonSeparateRadius)
        {
            if (_canEditStageProp)
            {
                foreach (var button in _stagePropfunctionButtons)
                {
                    button.gameObject.SetActive(true);
                    // first clear the events
                    button.ClearButtonDownEvent();
                    // deactivate buttons
                    button.DeactivateButton();
                    // make sure other buttons will be toggled off
                    button.SubscribeOnObjectButtonDown(DeactivateEditingButtons);
                    // inject individual events to the buttons.
                    button.InitializeButtonObject(propObject.transform, propObject);
                }

                // get the current mouse position
                var desiredPosition = propObject.transform.position;
                if (SceneCameraReference.Instance.SceneMainCamera != null)
                {
                    desiredPosition = SceneCameraReference.Instance.SceneMainCamera.ScreenToWorldPoint(Input.mousePosition);
                }   
            
                // re-animate the buttons
                float degreeInterval = 360.0f / (float)_stagePropfunctionButtons.Count;
                for (int i = 0; i < _stagePropfunctionButtons.Count; i++)
                {
                    float degreeOffset = 90.0f - degreeInterval;
                    float degree = i * degreeInterval + degreeOffset;
                    float y = Mathf.Sin(degree * Mathf.Deg2Rad) * buttonSeparateRadius;
                    float x = Mathf.Cos(degree * Mathf.Deg2Rad) * buttonSeparateRadius;
                    Vector3 position = new Vector3(x + desiredPosition.x, y + desiredPosition.y,
                        (int)LayerZ.Foreground + 0.5f);
                
                    _stagePropfunctionButtons[i].gameObject.transform.position = position;
                    _stagePropfunctionButtons[i].ReactivateButton();
                }   
            }
        }
        
        private void DeactivateEditingButtons()
        {
            foreach (var button in _stagePropfunctionButtons)
            {
                // deactivate current button game objects
                button.DeactivateButton();
            }
        }
    }
    
}