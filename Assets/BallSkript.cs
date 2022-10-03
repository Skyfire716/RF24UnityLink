using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSkript : MonoBehaviour
{
    
    public Transform droneCenter;
    private Transform trans;
    private Rigidbody rig;
    
    // Start is called before the first frame update
    void Start()
    {
        trans = gameObject.GetComponent<Transform>();
        rig = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(droneCenter.position.y + 10 < trans.position.y){
            trans.position = new Vector3(droneCenter.position.x, trans.position.y, droneCenter.position.z);
        }
        if(trans.position.y < -300){
            trans.position = new Vector3(20, 150, -8);
            rig.velocity = new Vector3(0, 0, 0);
            
        }
    }
}
