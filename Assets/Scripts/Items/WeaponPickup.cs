using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour {

	private enum WeaponType{
		SABER,
		PISTOL
	};

	WeaponType weaponType;

	void OnTriggerEnter2D(Collider2D other){
		if(other.CompareTag("Player")){
			other.GetComponent<PlayerController>().WeaponList.Add(this.gameObject);
		}
	}
}
