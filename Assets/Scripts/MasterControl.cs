﻿using System.Collections;
using UnityEngine;
using System;

/* Master (Main) control wrapper of character behavior: 
 *		[offset ctrl] + [mecanim pose ctrl] + [legIK ctrl] + [facial expression ctrl] + [hand shape ctrl]
 * Create GUI and allow control
 * Allow BML reader to acess all the control */

public class MasterControl : MonoBehaviour
{
	public bool Interface = false;

	private GameObject character;       // assigned character
	private MainOffset mainOffsetCtrl;
	private SpineOffset spineOffsetCtrl;
	private MecanimControl mecanimControl;
	private LegIKControl legControl;
	private FacialBlendShape facialControl;
	private SignalRequester requester;
	private ChangeSlide slideControl;

	float animSpeed = 1.0f;
	float currentRotation = 0.0f;
	float blendDuration = 0.15f;
	bool emitorFlag = false;
	bool ikStatus = false;
	int strength = 100;

	void Start()
    {
		character = GameObject.FindGameObjectWithTag("Player");

		// Simple components added during runtime
		mecanimControl = character.AddComponent<MecanimControl>();
		legControl = character.AddComponent<LegIKControl>();
		mainOffsetCtrl = character.AddComponent<MainOffset>();
		spineOffsetCtrl = character.AddComponent<SpineOffset>();
		character.AddComponent<HandLayerControl>();		// static function, called directly
		facialControl = character.AddComponent<FacialBlendShape>();
		slideControl = gameObject.AddComponent<ChangeSlide>();
	}

	void OnGUI()
	{
		if (Interface)
		{	
			// mecanim control
			mecanimGUI();

			// ik lock
			if (GUI.Button(new Rect(500f, 20f, 100f, 20f), "Foot IK")){
				footLock(ikStatus);
				ikStatus = !ikStatus;
			}

			// facial expression preset
			strength = (int)GUI.HorizontalSlider(new Rect(800, 0, 100, 20), strength, 0f, 100f);
			if (GUI.Button(new Rect(800, 20, 100, 20), "Angry"))
				setFacialExpression("Angry");
			if (GUI.Button(new Rect(800, 40, 100, 20), "Bored"))
				setFacialExpression("Bored");
			if (GUI.Button(new Rect(800, 60, 100, 20), "Content"))
				setFacialExpression("Content");
			if (GUI.Button(new Rect(800, 80, 100, 20), "Happy"))
				setFacialExpression("Happy", strength);

			// hand shape
			Array handPose = Enum.GetValues(typeof(Global.HandPose));
			for (int i = 0; i < Setting.handShape; i++)
			{
				if (GUI.Button(new Rect(250, 20 + 40 * i, 100, 20), handPose.GetValue(Convert.ToInt32(i)).ToString()))
					setHandShape("L", handPose.GetValue(Convert.ToInt32(i)).ToString());
				if (GUI.Button(new Rect(350, 20 + 40 * i, 100, 20), handPose.GetValue(Convert.ToInt32(i)).ToString()))
					setHandShape("R", handPose.GetValue(Convert.ToInt32(i)).ToString());
			}
		}
	}

	private void OnDestory()
	{
		requester.Stop();
	}

	// avoid rapid transitioning
	IEnumerator emitor()
	{
		emitorFlag = true;
		yield return new WaitForSeconds(Setting.emitTime);
		emitorFlag = false;
	}

	public void changePose(int poseIndex, float speed=1.0f, float blend=0.15f)
	{
		if (!emitorFlag) { 
			mecanimControl.Play(mecanimControl.animations[poseIndex], blend);
			mecanimControl.SetSpeed(speed);
			StartCoroutine(emitor());  // avoid frequent transition
		}
	}

	public void randomBeat()
	{
		int[] beatList;
		switch (character.name)
		{
			case Global.David:
				beatList = Global.DavidBeatGestures;
				break;
			case Global.Luna:
				beatList = Global.LunaBeatGestures;
				break;
			default:
				beatList = null;
				break;
		}

		System.Random rnd = new System.Random();
		int beatIndex = rnd.Next(0, beatList.Length);
		changePose(beatList[beatIndex], 0.9f, 0.1f);
	}

	public void randomIdle() {
		int[] idleList;
		switch (character.name)
		{
			case Global.David:
				idleList = Global.DavidIdleGestures;
				break;
			case Global.Luna:
				idleList = Global.LunaIdleGestures;
				break;
			default:
				idleList = null;
				break;
		}

		System.Random rnd = new System.Random();
		int idleIndex = rnd.Next(0, idleList.Length);
		changePose(idleList[idleIndex], 0.9f, 0.15f);
	}

	public void footLock(bool status) {
		if (status)
			legControl.footLock();
		else
			legControl.footUnlock();
	}

	public void setFacialExpression(string emotion, int strength=100) {
		Debug.Assert(strength >= 0 && strength <= 100, "strength should be in range 0 to 100");
		if (emotion == "Angry")
			facialControl.setAngry(strength);
		else if (emotion == "Bored")
			facialControl.setBored(strength);
		else if (emotion == "Content")
			facialControl.setContent(strength);
		else if (emotion == "Happy")
			facialControl.setHappy(strength);
	}

	public void raiseBrow()
	{
		StartCoroutine(facialControl.browRaise());
	}

	public void setHandShape(string side, string shape) {
		Global.HandPose handType = Global.HandPose.Relax;
		if (shape == "Relax")
			handType = Global.HandPose.Relax;
		else if (shape == "Palm")
			handType = Global.HandPose.Palm;
		else if (shape == "Fist")
			handType = Global.HandPose.Fist;

		if (side == "L")
			HandLayerControl.setLeftHand((int)handType);
		else if (side == "R")
			HandLayerControl.setRightHand((int)handType);
	}

	public void characterOffset(string type, int strength) {
		Global.BodyOffset offsetType = Global.BodyOffset.Neutral;
		if (type == "forward") offsetType = Global.BodyOffset.Forward;
		else if (type == "backward") offsetType = Global.BodyOffset.Backward;
		else if (type == "inward") offsetType = Global.BodyOffset.Inward;
		else if (type == "outward") offsetType = Global.BodyOffset.Outward;

		StartCoroutine(spineOffsetCtrl.spineOffset(offsetType, strength));
		StartCoroutine(mainOffsetCtrl.bodyOffset(offsetType, strength));
	}

	public void requestSignal(string message)
	{
		ViewerEmotion.lastEmotion = ViewerEmotion.currentEmotion;
		ViewerEmotion.reset = true;
		requester = new SignalRequester();
		requester.message = message;
		requester.Start();
	}

	public void changeSlide(string direction, int step)
	{
		if (direction == "next")
			slideControl.NextSlide(step);
		else if(direction == "back")
			slideControl.PreviousSlide(step);
	}

	private void mecanimGUI()
	{
		GUILayout.BeginVertical();

		GUILayout.Label("Speed (" + animSpeed.ToString("0.00") + ")");
		animSpeed = GUILayout.HorizontalSlider(animSpeed, 0.0f, 2.0f);
		mecanimControl.SetSpeed(animSpeed);

		GUILayout.Label("Rotation (" + currentRotation.ToString("000") + ")");
		currentRotation = GUILayout.HorizontalSlider(currentRotation, 0.0f, 360.0f);
		character.transform.localEulerAngles = new Vector3(0.0f, currentRotation, 0.0f);

		GUILayout.Label("Blending (" + blendDuration.ToString("0.00") + ")");
		blendDuration = GUILayout.HorizontalSlider(blendDuration, 0.0f, 1.0f);
		mecanimControl.defaultTransitionDuration = blendDuration;

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();

		int index = 0;
		foreach (AnimationData animationData in mecanimControl.animations)
		{
			GUIStyle style = new GUIStyle(GUI.skin.button);
			style.fixedHeight = 20;
			style.fixedWidth = 40;
			if (GUILayout.Button(animationData.clipName, style))
			{
				if (index < mecanimControl.animations.Length)
					changePose(index);

				else print("index out of range");
			}
			index++;
			GUILayout.Space(5);
			if (index % 10 == 0)
			{
				GUILayout.EndVertical();
				GUILayout.BeginVertical();
			}
		}

		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
}
