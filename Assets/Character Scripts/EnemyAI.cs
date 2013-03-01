using UnityEngine;
using System.Collections;
using System;
using System.Linq;

public class EnemyAI : MonoBehaviour {
	public Transform target;
	public int moveSpeed;
	public int rotateSpeed;
	public int maxDistance;
	public int aggroDistance;
	public int aggroTimeout;
	
	private Transform _myTransform;
	private bool _hasAggro;
	private DateTime _aggroTime;
	private DateTime _timeNow;
	private DateTime _aggroTimeoutTimer;
	private int _rayCastDistance;
	private Vector3 dir;
	
	private bool _stopMoving;
	
	private Transform[] _wayPoints;
	
	// Starts before anything else
	void Awake() {
	}

	// Use this for initialization
	void Start () {
		// Find the player by tag
		GameObject go = GameObject.FindGameObjectWithTag("Player");
		
		if(go == null) {
			Debug.LogError("Cannot find gameobject tagged with player, aborting");
			return;
		}
		
		// Set the target to the player's transform
		target = go.transform;
		_myTransform = transform;
		
		maxDistance = 4;
		moveSpeed = 2;
		rotateSpeed = 2;
		aggroDistance = 20;
		_hasAggro = false;
		_rayCastDistance = 10;
		_stopMoving = false;
	}
	
	// Update is called once per frame
	void Update () {
	}
	
	void FixedUpdate() {
		// If we dont have a target, abort this update sequence
		if(target == null)
			return;
		
		// Record the time when rendering this frame
		_timeNow = DateTime.Now;
		
		// Check if the enemy object is in aggrorange of the player, meaning the player has to be
		// a certain distance in order to get the attention of the enemy object
		if(Vector3.Distance(target.position, _myTransform.position) < aggroDistance && !_hasAggro) {
			_hasAggro = true;
			_aggroTimeoutTimer = _timeNow.AddSeconds(aggroTimeout);
		}
		
		// If we have aggro, lock on to the player and start move towards him
		
		dir = (new Vector3(target.position.x, 0, target.position.z) - new Vector3(_myTransform.position.x, 0, _myTransform.position.z)).normalized;
		
		if(_hasAggro) {
			Debug.DrawLine(target.position, _myTransform.position, Color.red);
			AnticipateCollision(dir, Vector3.Distance(_myTransform.position, target.position), 
				delegate(RaycastHit hit) {
					// Check to see if the obstacle is between the player and us
					if(Vector3.Distance(_myTransform.position, hit.transform.position) < Vector3.Distance(_myTransform.position, target.position)) {
						// If this is true, it indicates that something is between us and the player
					
						// Calculate Path to take around the object
						CalculatePath(hit);
					
					}
				});
			}
			
			// Move the enemy object			
			if(!_stopMoving && _hasAggro) {
				// rotation
				var rot = Quaternion.LookRotation (dir);
			
				transform.rotation = Quaternion.Slerp (transform.rotation, rot, Time.deltaTime);
				 
				//position
				if(Vector3.Distance(target.position, _myTransform.position) > maxDistance)
					transform.position += transform.forward * (2 * Time.deltaTime); // 20 is speed
				
				// Timeout the aggro if the mob hasn't hit us in a given amount of time
				// This has to be moved into attack script later
				if(_timeNow > _aggroTimeoutTimer)
					_hasAggro = false;
			}
		}
	
	/// <summary>
	/// Anticipates a collision at a certain angle and distance when persuing a target position
	/// </summary>
	/// <param name='angle'>
	/// The angle we want the ray to be cast at
	/// </param>
	/// <param name='distance'>
	/// The distance we want to raycast at
	/// </param>
	private void AnticipateCollision(Vector3 angle, float distance, Action<RaycastHit> onDetection) {
		RaycastHit hit = new RaycastHit();
		 
		// Cast a ray towards the angle given with the given distance
		if (Physics.Raycast(_myTransform.position, angle, out hit, distance)) {
			// If we hit something that is not at the same position we are
		   if (hit.transform != this.transform && hit.transform.gameObject.name != "Ground") {
				// Draw a line towards the player for debugging purposes
		     	Debug.DrawLine (transform.position, hit.point, Color.blue);
		 		
				// Fire the callback to perform an action if the raycast returns true
				onDetection(hit);
		   }
		}	
	}
	
	private void CalculatePath(RaycastHit hit) {
		Vector3 hitCenter = hit.transform.collider.bounds.center;
		Vector3 hitExtend = hit.transform.collider.bounds.extents;
		Vector3 corner1 = new Vector3(hitCenter.x + hitExtend.x, hitCenter.y - hitExtend.y, hitCenter.z + hitExtend.z) + new Vector3(_myTransform.lossyScale.x,0,_myTransform.lossyScale.z);
		Vector3 corner2 = new Vector3(hitCenter.x - hitExtend.x, hitCenter.y - hitExtend.y, hitCenter.z + hitExtend.z) + new Vector3(-_myTransform.lossyScale.x,0,_myTransform.lossyScale.z);
		Vector3 corner3 = new Vector3(hitCenter.x + hitExtend.x, hitCenter.y - hitExtend.y, hitCenter.z - hitExtend.z) + new Vector3(_myTransform.lossyScale.x,0,-_myTransform.lossyScale.z);
		Vector3 corner4 = new Vector3(hitCenter.x - hitExtend.x, hitCenter.y - hitExtend.y, hitCenter.z - hitExtend.z) + new Vector3(-_myTransform.lossyScale.x,0,-_myTransform.lossyScale.z);
	
		Vector3[] corners = new Vector3[4];
		corners[0] = corner1;
		corners[1] = corner2;
		corners[2] = corner3;
		corners[3] = corner4;
		
		Vector3[][] pairs = 
		{
			// Pair1 = corner1->corner3
			new Vector3[] {corner1, corner3},
			
			// Pair2 = corner4->corner4
			new Vector3[] {corner3, corner4},
			
			//pair3 = corner4->corner2
			new Vector3[] {corner4, corner2},
			
			// Pair 4 = corner2->corner1
			new Vector3[] {corner2, corner1}
		};
		
		int[] distances = new int[4];
		
		for(int i=0; i<pairs.Length; i++) {
			Vector3 point1;
			Vector3 point2;
			
			// Find closest point from the mob
			if(Vector3.Distance(_myTransform.position, pairs[i][0]) > Vector3.Distance (_myTransform.position, pairs[i][1])) {
				point1 = pairs[i][1];
				point2 = pairs[i][0];
			} else {
				point1 = pairs[i][0];
				point2 = pairs[i][1];
			}
			
			pairs[i][0] = point1;
			pairs[i][1] = point2;
			
			distances[i] = (int) (Vector3.Distance(_myTransform.position, pairs[i][0]) + 
								  Vector3.Distance(pairs[i][0], pairs[i][1]) + 
								  Vector3.Distance (pairs[i][1], target.position));
		}
		
		int minDistanceIndex = Array.IndexOf(distances, distances.Min());
		
		Vector3[] shortest = pairs[minDistanceIndex];
		
		Debug.DrawLine(_myTransform.position, shortest[0], Color.white);
		Debug.DrawLine(shortest[0], shortest[1], Color.white);
		Debug.DrawLine(shortest[1], target.position, Color.white);
	
		/*
		 * 
		 * FIX THIS!
		 * 
		 * 		|
		 * 		|
		 * 		v
		 * */
		
		if(Vector3.Distance (_myTransform.position, shortest[0]) < Vector3.Distance(_myTransform.position, shortest[1]))
			dir = (new Vector3(shortest[0].x, 0, shortest[0].z) - new Vector3(_myTransform.position.x, 0, _myTransform.position.z)).normalized;
		//else
		//	dir = (new Vector3(shortest[1].x, 0, shortest[1].z) - new Vector3(_myTransform.position.x, 0, _myTransform.position.z)).normalized;
		
//		float distanceAroundCorner1 = Vector3.Distance(_myTransform.position, corner1) + Vector3.Distance(corner1, target.position);
//		float distanceAroundCorner2 = Vector3.Distance(_myTransform.position, corner2) + Vector3.Distance(corner2, target.position);
//	
//		if(distanceAroundCorner1 < distanceAroundCorner2) {
//			// Around corner 1 is the shortest way, so lets go there
//			//Debug.DrawLine(_myTransform.position, corner1, Color.white);
//			dir = (new Vector3(corner1.x, 0, corner1.z) - new Vector3(_myTransform.position.x, 0, _myTransform.position.z)).normalized;
//		} else {
//			//Debug.DrawLine(_myTransform.position, corner2, Color.black);
//			dir = (new Vector3(corner2.x, 0, corner2.z) - new Vector3(_myTransform.position.x, 0, _myTransform.position.z)).normalized;
//		}
	}
}
