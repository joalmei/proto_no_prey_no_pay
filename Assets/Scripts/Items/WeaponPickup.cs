using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour {

	private enum WeaponType{
		SABER,
		PISTOL
	};

	WeaponType weaponType;

	//void OnTriggerEnter2D(Collider2D other){
	void OnTriggerEnter(Collider other){	
		if(other.CompareTag("Player")){
			other.GetComponent<PlayerController>().WeaponList.Add(this.gameObject);
		}
	}

//	void OnTriggerExit2D(Collider2D other){
	void OnTriggerExit(Collider other){
		if(other.CompareTag("Player")){
			other.GetComponent<PlayerController>().WeaponList.Remove(this.gameObject);
		}
	}
}
