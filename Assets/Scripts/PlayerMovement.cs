using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public int visionDistance = 5;

    public float gridSize = 1f;

    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        gridManager = GameObject.FindGameObjectWithTag("GridManager").GetComponent<GridManager>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Move(Vector2.up);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            Move(Vector2.down);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            Move(Vector2.left);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            Move(Vector2.right);
        }
    }
    private void Move(Vector2 direction)
    {
        //takes current position
        Vector2 startPosition = transform.position;
        Vector2 target = startPosition + (direction * gridSize);
        Debug.Log("target:" + target);
        Vector2Int endPosition = gridManager.GetCellPosition(target);
        Debug.Log("endPosition:" + endPosition);
        //sets player position to the center of the target tile
       //transform.position = gridManager.GetTileCenter(endPosition);
       transform.position = new Vector3(endPosition.x, endPosition.y, 0);
       gridManager.PlayerDijkstras();
    }
    
    
}
