using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wolf : MonoBehaviour
{
    public static Dictionary<Vector2, Wolf> wolf_db_ = new Dictionary<Vector2, Wolf>();

    private Vector2 pos_;
    
    public delegate void OnWolfAddedToGame(Wolf wolf);
    public static event OnWolfAddedToGame onWolfAddedToGame;

    public delegate void OnWolfDeletedFromGame(Wolf wolf);
    public static event OnWolfDeletedFromGame onWolfDeletedFromGame;
    
    
    
    private void Awake()
    {
        pos_ = new Vector2(transform.position.x, transform.position.y);
        wolf_db_.Add(pos_, this);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
