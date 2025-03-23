using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public int NrPlayers;
    public GameObject[] Players;
    //private Vector3[] velocity;
    public GameObject rock;
    public Vector2 spawnValues;
    //float speed;
    
    private float maxSpeed = 2;
    private float wanderStreanght = 0;
    public float sampleSize = 30;
    [Range(0.0f, 180.0f)] public float detectionAngle;

    Vector2 position;
    Vector2[] velocity;
    Vector2[] desiredDirection;
    float epsilon = 0;

    float distance = 1f;
    float distanceCircle = .5f;


    // Sliders for controling variables
    private void OnGUI()
    {
        maxSpeed = GUI.HorizontalSlider(new Rect(0, 00, 100, 30), maxSpeed, 0, 4);
        wanderStreanght = GUI.HorizontalSlider(new Rect(0, 40, 100, 30), wanderStreanght, 0, .2f);
        
        epsilon = GUI.HorizontalSlider(new Rect(0, 80, 100, 30), epsilon, 0, 1);
    }

    float min( float a, float b ){
        return (a < b) ? a : b;
    }

    // Start is called before the first frame update
    void Start()
    {
        Players = new GameObject[NrPlayers];
        velocity = new Vector2[NrPlayers];
        desiredDirection = new Vector2[NrPlayers];
        //velocity = new Vector3[NrPlayers];
        int i;
        for( i=0; i<NrPlayers; i++ )
        {
            //Debug.Log(i);
            Vector3 spawnPosition = new Vector3(Random.Range(-spawnValues.x, spawnValues.x), Random.Range(-spawnValues.y, spawnValues.y), 0);
            Players[i] = Instantiate(rock, spawnPosition /*+ TransformPoint(0, 0, 0)*/, gameObject.transform.rotation);
            Players[i].SetActive(true);
            //velocity[i] = new Vector3( Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0 );
            Players[0].GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.1f, 0.1f, 1f);
        }
    }

    public void DrawLine(Vector3 startPoint, Vector3 endPoint, Color lineColor, float duration)
    {
        if( startPoint == endPoint ) return;
        GameObject lineObject = new GameObject("LineRendererObject");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        Destroy(lineObject, duration);
    }

    float f( float x )
    {
        float exponent = 4;
        return Mathf.Pow((distance - x) / distance * 3, exponent);
    }

    Vector2 CalculateColidingVector_Neighbours( int p )
    {
        int i;
        Vector2 awayFromNeighbours = new Vector2(0, 0);
        Vector2 position = Players[p].GetComponent<Transform>().position;

        for( i=0; i < NrPlayers; i++ )
        {
            Vector2 neighbourPosition = Players[i].GetComponent<Transform>().position;
            if( i == p ) continue;
            if( Mathf.Abs(Vector2.Distance( position, neighbourPosition )) > distance ) continue;
            if( Mathf.Abs(Vector2.Angle( velocity[i], neighbourPosition - position )) < 90 ) continue;
            
            if( p == 0 ) DrawLine( position, neighbourPosition, Color.red, .03f );

            awayFromNeighbours = f(Vector2.Distance( position, neighbourPosition )) * (position - neighbourPosition);
        }
        if( p == 0 ) DrawLine( position, position + awayFromNeighbours, Color.green, .03f );
        return awayFromNeighbours / 10 * epsilon;
    }

    // Update is called once per frame
    void Update()
    {
        int i;
        for( i=0; i < NrPlayers; i++ )
        {
            desiredDirection[i] = (desiredDirection[i] + Random.insideUnitCircle * wanderStreanght).normalized;

            Vector2 desiredVelocity = desiredDirection[i] * maxSpeed;
            Vector2 acceleration = desiredVelocity - velocity[i];

            velocity[i] = (velocity[i] + acceleration * Time.deltaTime).normalized * maxSpeed;
            velocity[i] = velocity[i] + CalculateColidingVector_Neighbours(i);
            //velocity[i] = velocity[i] + CalculateColidingVector_Obstacle(i);
            position = Players[i].GetComponent<Transform>().position;

            //Drawing direction
            //if( i == 0 )
            {
                Vector2 awayFromColliders = new Vector2(0f, 0f);
                float teta = (Players[i].GetComponent<Transform>().eulerAngles.z) * 3.1415f / 180;
                //if(i == 0) DrawLine( position, position + new Vector2( Mathf.Cos(teta), Mathf.Sin(teta)), Color.blue, 0.03f );

                //Draw circlar raycast
                for( int k=0; k < sampleSize; k++ ){
                    float alpha =  k * (2 * 3.1415f) / sampleSize;
                    Vector2 pointOnCircle = distanceCircle * (new Vector2( Mathf.Cos(alpha), Mathf.Sin(alpha)));
                    if( min( Mathf.Abs( teta - alpha ), (2 * 3.1415f) - Mathf.Abs(teta - alpha) ) <= detectionAngle * Mathf.Deg2Rad ){
                        
                        int ct = 0;
                        Color color = Color.yellow;

                        RaycastHit2D hit = Physics2D.Raycast(position, pointOnCircle.normalized, distanceCircle);
                        if( hit.collider != null && hit.collider.CompareTag("Obstacle") ){
                            Debug.Log("nice hit");
                            color = Color.white;

                            ct++;
                            awayFromColliders += (position - hit.point) * (distance - hit.distance);
                        }
                        //if( ct > 0 ) awayFromColliders /= ct;

                        //if(i == 0) DrawLine( position, position + pointOnCircle, color, 0.03f );
                    }
                }
                velocity[i] = velocity[i] + awayFromColliders / 3;
            }

            //Out of Bounds Catch
            if( position.x >= 10 ) position += new Vector2( -20, 0 );
            if( position.x <= -10 ) position += new Vector2( 20, 0 );
            if( position.y >= 5 ) position += new Vector2( 0, -10 );
            if( position.y <= -5 ) position += new Vector2( 0, 10 );

            float angle = Mathf.Atan2(velocity[i].y, velocity[i].x) * Mathf.Rad2Deg;
            position += velocity[i] * Time.deltaTime;
            Players[i].GetComponent<Transform>().SetPositionAndRotation(position, Quaternion.Euler(0, 0, angle));
        }
    }
}
