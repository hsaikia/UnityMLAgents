﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocialWalkerAgent : Agent {
    public int sw_id;
    private SocialWalker agent_;
    private SocialWalker neighbor_;
    private SocialWalkerCrowd cr_;
    public GameObject target_;

    //parameters
    const float rewardTargetReached = 1.0f;
    const float rewardCollision = -0.5f;
    const float rewardOutOfBounds = -0.5f; 
    const float orientationWeight= 0.005f;
    const float distanceGainedWeight = 0.2f;
    const float rewardEachStep = -0.01f;

    const int obsSpaceSize = 6;

    const float agentFOV = 60.0f;
    const float agentSensorLength = 20.0f;

    private int step;
    private bool agentDone = false;

    void Awake(){

    }

    void Start()
    {
        step = 0;
        cr_ = GameObject.Find("Crowd").GetComponent<SocialWalkerCrowd>();
        cr_.getAgent(ref agent_, sw_id);
    }

    public override void CollectObservations()
    {
        // Agent only remains on the 2D XZ plane
        // AddVectorObs(agent_.target.x);
        // AddVectorObs(agent_.target.z);

        //Debug.Log("I am Agent " + sw_id);

        // AddVectorObs(agent_.pos.x);
		// AddVectorObs(agent_.pos.z);

        // AddVectorObs(agent_.vel.x);
		// AddVectorObs(agent_.vel.z);

        // Debug.Log("My Position " + agent_.pos.x + " " + agent_.pos.z);
        // Debug.Log("My Velocity " + agent_.vel.x + " " + agent_.vel.z);

        // for(int i = 0; i < crowd_.numAgents_; i++){
        //     if(i == sw_id){
        //         continue;
        //     }
        //     AddVectorObs(crowd_.getAgent(i).pos.x);
    	// 	AddVectorObs(crowd_.getAgent(i).pos.z);
        //     AddVectorObs(crowd_.getAgent(i).vel.x);
    	// 	AddVectorObs(crowd_.getAgent(i).vel.z);

        //     Debug.Log("I see agent " + i + " like this : ");
        //     Debug.Log("Position " + crowd_.getAgent(i).pos.x + " " + crowd_.getAgent(i).pos.z);
        //     Debug.Log("Velocity " + crowd_.getAgent(i).vel.x + " " + crowd_.getAgent(i).vel.z);

        // }

        var sensorData = cr_.getSensors(sw_id, obsSpaceSize, agentFOV, agentSensorLength);
        for(int i = 0; i < sensorData.Count; i++){
            //Debug.Log("Sensor " + i + ":" + sensorData[i]);
            AddVectorObs(sensorData[i]);
        }
    }

    public override void AgentReset()
    {
        step = 0;
        Vector3 pos = new Vector3(Random.Range(cr_.minBound_.x, cr_.maxBound_.x), 0.5f, Random.Range(cr_.minBound_.z, cr_.maxBound_.z));
        Vector3 tar = new Vector3(Random.Range(cr_.minBound_.x, cr_.maxBound_.x), 0.5f, Random.Range(cr_.minBound_.z, cr_.maxBound_.z));
        agent_.init(pos, tar);
        cr_.setAgent(ref agent_, sw_id);
        gameObject.transform.position = pos;
        target_.transform.position = tar;
    }

    public override void AgentAction(float[] act, string textAction)
    {
        if(agentDone){
            if(!cr_.allDone_){
                return;
            } else {
                agentDone = false;
                cr_.setAgentActiveStatus(sw_id, true);
                return;
                //Done();
            }
        } 
        //Debug.Log("Actions act[0] " + act[0] + "act[1] " + act[1]);
        step++;
        // 0 -> move forward
        // 1 -> turn left
        // 2 -> turn right

        float distToTargetOld = (target_.transform.position - agent_.pos).magnitude;

        if (brain.brainParameters.vectorActionSpaceType == SpaceType.continuous)
        {
            gameObject.transform.position += gameObject.transform.forward.normalized * Mathf.Clamp(act[0], 0f, 1f);

            float rotAngle = 90.0f * Mathf.Clamp(act[1], -1f, 1f); // opposite ends, reachable from [-90, 90] rotation 

            gameObject.transform.Rotate(0f, rotAngle, 0f);
        }
        else
        {
            int action = (int)act[0];
            //Debug.Log(action);
            if (action == 0) // accelerate forward
            {
                agent_.accelerateForward(0.001f);
            }
            if (action == 1) // accelerate backward
            {
                agent_.accelerateForward(-0.005f);
            }
            else if (action == 2) // accelerate right
            {
                agent_.accelerateRight(0.001f);
            }
            else if (action == 3) // accelerate left
            {
                agent_.accelerateRight(-0.001f);
            }
            else if (action == 4)
            {
                agent_.maintainSpeed();
            }

            gameObject.transform.position = agent_.pos;
            gameObject.transform.forward = agent_.forward;

        }

        cr_.setAgent(ref agent_, sw_id);

        float distToTarget = (target_.transform.position - agent_.pos).magnitude;

        if (agent_.targetReached())
        {
            //Debug.Log("Reached Target! Agent Done.");
            AddReward(rewardTargetReached);
            AgentDoneStuff();            
            return;
        }
        
        if (!agent_.withinBounds(cr_.minBound_, cr_.maxBound_))
        {
            //Debug.Log("Went out of Arena! Agent Done.");
            AddReward(rewardOutOfBounds);
            AgentDoneStuff();
            return;
        }

        if(cr_.doesCollide(sw_id)){
            Debug.Log("Collision!");
            AddReward(rewardCollision);
        }

        //reward for gaining distance towards the target
        AddReward(distanceGainedWeight * (distToTargetOld - distToTarget));
        //reward for orienting towards the target
        AddReward(orientationWeight * agent_.cosineOrientation()); // a reward between [-0.005, 0.005]
        //reward for each step (usually negative)
        AddReward(rewardEachStep);

    }

    void AgentDoneStuff(){
        Done();
        gameObject.SetActive(false);
        target_.SetActive(false);
        cr_.setAgentActiveStatus(sw_id, false);
        agentDone = true;
    }

}
