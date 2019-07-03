using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field_Of_View : MonoBehaviour
{
    public float viewRadius;
    [Range(0,360)]
    public float viewAngle;
    [Range(-180, 180)]
    [SerializeField] float view_direction;
    [SerializeField] private Vector3 target_dir;

    public LayerMask targetMask;
    [SerializeField] GameObject target_object;
    public LayerMask obstacleMask;

    public List<Transform> visibleTargets = new List<Transform>();

    [Range(0, 1)]
    public float meshResolution;
    public int edgeResolveIterations;
    public float edgeDistanceThreshold;

    public MeshFilter viewMeshFilter;
    Mesh viewMesh;



    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }
    
    public void Set_View_Dir(Vector3 i_dir)
    {
        target_dir = i_dir;
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider2D[] targetsInViewRadius;
        targetsInViewRadius = Physics2D.OverlapCircleAll(new Vector2(transform.position.x, transform.position.y), viewRadius, targetMask);


        // This function will cycle through all objects that are allowed targets (generally just the player for this)
        // It will calculate if they are within view of the object this is attached to
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float angle = -Vector3.SignedAngle(Vector3.up, dirToTarget,Vector3.forward);

            float max_angle = view_direction + (viewAngle / 2);
            float min_angle = view_direction - (viewAngle / 2);

            if ((min_angle > 0) && (max_angle > 0) && (angle < 0))
            {
                angle += 360;
            }
            else if ((min_angle < 0) && (max_angle < 0) && (angle > 0))
            {
                angle -= 360;
            }





            if ((angle <= max_angle) && (angle >= min_angle))
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics2D.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
                }
            }
        }
    }

    void DrawFoV()
    {
        // Draws the FoV for the enemy
        // Uses a procedural mesh to draw it out
        // Calculates the points by using raycasting to scan the edges of objects the owning gameobject can see

        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;

        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = (transform.eulerAngles.x - viewAngle / 2 + stepAngleSize * i) + view_direction;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i  > 0)
            {
                bool edgeDistanceThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDistanceThreshold; 
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDistanceThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);

                    if (edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();

    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        // When the raycasting finds an edge, it narrows down the location by halving between 2 points
        // This makes the edge detection more precise

        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;

        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDistanceThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDistanceThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDistanceThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit2D hit;
        hit = Physics2D.Raycast(transform.position, dir, viewRadius, obstacleMask);
        if (hit.collider != null)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
        }
    }

    public Vector3 DirFromAngle(float angleinDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleinDegrees += transform.eulerAngles.x;
        }
        return new Vector3(Mathf.Sin(angleinDegrees * Mathf.Deg2Rad),Mathf.Cos(angleinDegrees * Mathf.Deg2Rad), 0.0f );
        //return new Vector3(Mathf.Sin(angleinDegrees * Mathf.Deg2Rad), 0.0f, Mathf.Cos(angleinDegrees * Mathf.Deg2Rad));
    }

	// Use this for initialization
	void Start ()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;
        StartCoroutine("FindTargetsWithDelay", 0.1f);	
	}
	
	// Update is called once per frame
	void LateUpdate ()
    {
        target_dir = target_dir.normalized;
        view_direction = -Vector3.SignedAngle(Vector3.up, target_dir,Vector3.forward);

        DrawFoV();
	}

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _PointB)
        {
            pointA = _pointA;
            pointB = _PointB;
        }
    }
}
