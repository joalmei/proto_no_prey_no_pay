using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMovement : MonoBehaviour {

	private Vector3 DirectionVector;
	private bool isAirborne;

	// Update is called once per frame
	void Update () {
		if(!isAirborne){
			return;
		}
		transform.Translate(DirectionVector);

		//checar colisão também
	}

	// ideally direction == 1 is right, and direction == -1 is left
	// it can also be changed to a bool with 0 and 1
	public void ThrowWeapon(Vector3 direction){
		DirectionVector = direction;
		isAirborne = true;
	}

	public void ThrowWeaponAtAngle(){
		float randomAngle = Random.Range(-30f,30f);
		// throw
	}

	// overload for hitting a wall
	// choses random angle based on which side the wall was
	// if direction == 1, chooses angle between 0 and 30 degrees
	// if direction == -1, chooses angle between -30 and 0
	public void ThrowWeaponAtAngle(int direction){
		float randomAngle = Random.Range((direction*15 +15), (direction*15 + 15));
		//throw
	}
}
