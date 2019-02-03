using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wolf : MonoBehaviour
{
    public static Dictionary<Vector2, Wolf> wolf_db_ = new Dictionary<Vector2, Wolf>();

    private Vector2 pos_;
    private SpriteRenderer sprite_renderer_;
    
    private void Awake()
    {
        sprite_renderer_ = GetComponent<SpriteRenderer>();
        
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
