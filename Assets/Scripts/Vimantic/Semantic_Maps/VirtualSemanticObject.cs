﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VirtualSemanticObject : MonoBehaviour
{

    public int verbose;
    public float joiningDistance = 1;
    public float erodeRate = 0.1f;
    public CanvasLabelClass canvasLabel;
    public LineRenderer lineRender;
    public Vector2 heightCanvas;
    public SemanticObject semanticObject;

    public DateTime dateTime { get; private set; }
    public List<SemanticObject> associatedDetections;
    
    private ObjectManager objectManager;
    private OntologyManager ontologyManager;
    private DateTime time;

    #region Unity Functions

    private void OnTriggerStay(Collider other) {
        VirtualSemanticObject vso = other.gameObject.GetComponent<VirtualSemanticObject>();

        if (vso != null && vso.semanticObject.type.Equals(semanticObject.type) && dateTime <= vso.dateTime && null == associatedDetections.Find(O=>O.ontologyID.Equals(vso.semanticObject.ontologyID)) && vso.semanticObject.semanticRoom == semanticObject.semanticRoom) {
            if(dateTime == vso.dateTime && semanticObject.confidenceScore < vso.semanticObject.confidenceScore) {
                dateTime.AddMilliseconds(1);
                return;
            }
            
            time = DateTime.Now;
            associatedDetections.Add(vso.semanticObject);
            ontologyManager.JoinSemanticObject(semanticObject, vso.semanticObject);
            semanticObject.nDetections++;

            double score = 0;
            foreach (SemanticObject so in associatedDetections) {
                score += so.confidenceScore;
            }
            semanticObject.confidenceScore = score / associatedDetections.Count;

            transform.parent.rotation = Quaternion.identity;
            vso.transform.parent.rotation = Quaternion.identity;

            Bounds bounds = GetComponent<MeshRenderer>().bounds;
            //Debug.Log(semanticObject.pose+"/"+semanticObject.size+"/"+bounds.center + "/" + bounds.size);
            bounds.Encapsulate(vso.GetComponent<MeshRenderer>().bounds);
            //bounds.Encapsulate(vso.semanticObject.pose);

            semanticObject.pose = bounds.center;
            semanticObject.size = bounds.size*(1-erodeRate);
            UpdateObject();
            Destroy(vso.transform.parent.gameObject);

            if (objectManager == null) {
                objectManager = FindObjectOfType<ObjectManager>();
            }
            objectManager.AddTimeUnion((DateTime.Now - time).Milliseconds);
        }
    }


    private void OnDestroy() {
        if (associatedDetections.Count > 1) {
            ontologyManager.UpdateObject(semanticObject, semanticObject.pose, semanticObject.rotation, semanticObject.size, semanticObject.confidenceScore, semanticObject.nDetections);
        }
    }

    #endregion

    #region Public Functions
    public void InitializeObject(SemanticObject _semanticObject, Transform _robot)
    {        
        ontologyManager = FindObjectOfType<OntologyManager>();
        dateTime = DateTime.Now;
        semanticObject = _semanticObject;
        associatedDetections.Add(semanticObject);
        GetComponent<BoxCollider>().size = Vector3.one * joiningDistance;
        transform.parent.name = semanticObject.ontologyID;

        //transform.parent.rotation = Quaternion.identity;

        UpdateObject();
        SemanticRoom sr = GetRoom(transform.position);
        if(sr != GetRoom(_robot.position)) {
            ObjectManager.instance.virtualSemanticMap.Remove(_semanticObject);
            Destroy(transform.parent.gameObject);
        }
        semanticObject.semanticRoom = sr;
        if (semanticObject.semanticRoom != null) {
            ontologyManager.ObjectInRoom(semanticObject);
        }

        GetComponent<BoxCollider>().enabled = true;
    }

    public void UpdateObject() {
        //Load Object
        transform.parent.position = semanticObject.pose;
        transform.localScale = semanticObject.size;
        Vector3 rotation = semanticObject.rotation.eulerAngles;
        transform.parent.rotation = Quaternion.Euler(0, rotation.y, 0);
        //transform.parent.rotation = semanticObject.rotation;

        //Load Canvas
        canvasLabel.transform.position = semanticObject.pose + new Vector3(0, UnityEngine.Random.Range(heightCanvas.x, heightCanvas.y), 0);
        canvasLabel.LoadLabel(semanticObject.type, semanticObject.confidenceScore);

        lineRender.SetPosition(0, canvasLabel.transform.position - new Vector3(0, 0.2f, 0));
        lineRender.SetPosition(1, transform.parent.position);
    }

    public void RemoveSemanticObject()
    {
        FindObjectOfType<OntologyManager>().RemoveSemanticObject(semanticObject);
        Destroy(gameObject);
    }

    public static SemanticRoom GetRoom(Vector3 position) {
        RaycastHit hit;
        position.y = -100;
        if (Physics.Raycast(position, Vector3.up, out hit)) {
            return hit.transform.GetComponent<SemanticRoom>();            
        }
        return null;
    }
    #endregion

    #region Private Functions
    private void Log(string _msg) {
        if (verbose > 1)
            Debug.Log("[Object Manager]: " + _msg);
    }

    private void LogWarning(string _msg) {
        if (verbose > 0)
            Debug.LogWarning("[Object Manager]: " + _msg);
    }
    #endregion

}