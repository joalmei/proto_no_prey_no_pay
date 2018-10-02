using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

	private Vector3 DirectionVector = Vector3.zero;
	private bool isMoving = true;
	[SerializeField] private float gravityScale = 1;
	private float speed = 1;
    private PlayerController.ePlayer origin;

	// Update is called once per frame
	void Update () {
		if(!isMoving){
			transform.Translate(speed * Vector3.down * Time.deltaTime);
			speed += gravityScale * Time.deltaTime; 
			return;
		}
		transform.Translate(DirectionVector * Time.deltaTime);
		//checar colisão também
	}

	public void SetDirection(Vector3 direction){
		DirectionVector = direction;
	}

    public void SetOrigin(PlayerController.ePlayer player)
    {
        origin = player;
    }

	// ideally direction == 1 is right, and direction == -1 is left
	// it can also be changed to a bool with 0 and 1
	public void MoveProjectile(Vector3 direction){
		SetDirection(direction);
		isMoving = true;
	}

	public void MoveProjectileAtAngle(){
		float randomAngle = Random.Range(-30f,30f);
		// throw
	}

	// overload for hitting a wall
	// choses random angle based on which side the wall was
	// if direction == 1, chooses angle between 0 and 30 degrees
	// if direction == -1, chooses angle between -30 and 0
	public void MoveProjectileAtAngle(int direction){
		float randomAngle = Random.Range((direction*15 +15), (direction*15 + 15));
		//throw
	}

	void OnTriggerEnter(Collider other){
		if(isMoving && other.tag == "Player"){
			if(tag == "Lethal"){
				other.GetComponent<PlayerController>().TakeDamage(origin);
				Destroy(gameObject);
			}
			else{
				other.GetComponent<PlayerController>().GetStunned();
				SetDirection(Vector3.zero);
				isMoving = false;
			}
		}
	}
}
