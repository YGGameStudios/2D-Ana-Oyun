using UnityEngine;
using System.Collections.Generic;

public class WallMinionSkill : Skill
{
    [Header("Spawn")]
    public Transform firePoint;
    public GameObject minionPrefab; // WallStickyMinion
    public LayerMask wallMask;
    public GameObject fallbackBulletPrefab; // used if minion has no bullet prefab set

    [Header("Scan")]
    public float scanRadius = 12f;
    public int scanRays = 48;
    public float minDotToForward = -1f; // -1: any direction

    [Header("Launch Bias")]
    public float launchUpwardBias = 0.35f; // raises destination slightly by biasing the launch direction

    [Header("Burst Spawn")]
    public int spawnCount = 3;
    public float angleSpreadDeg = 60f; // total spread between outer shots (wider)
    public float randomAngleJitterDeg = 20f; // extra randomization per shot (wider)
    public float randomUpBiasJitter = 0.25f; // randomize upward bias per shot (slightly more)

    [Header("Destination Memory")]
    public int memorySize = 12; // how many last destinations to remember
    public float minSeparation = 3.5f; // minimum spacing between destinations in the same burst
    public float fartherBias = 1.0f; // weight to push away from remembered destinations

    // runtime memory of last destinations
    private readonly List<Vector2> _recentDestinations = new List<Vector2>();

    protected override bool Use()
    {
        return LaunchTowardBestWall();
    }

    protected override bool UseOn(Transform target)
    {
        // We still just scan walls; target is not required
        return LaunchTowardBestWall();
    }

    private bool LaunchTowardBestWall()
    {
        if (minionPrefab == null) return false;
        if (firePoint == null) firePoint = transform;

        Vector2 origin = firePoint.position;
        Vector2 baseDir;
        if (!ScanForWallDirection(origin, out baseDir)) return false;

        // Build candidate wall hits (denser)
        var candidates = BuildCandidates(origin, baseDir);
        // Choose destinations considering memory (push farther than previously used)
        var chosen = ChooseDestinations(candidates, Mathf.Max(1, spawnCount));

        bool any = false;
        var spawnedColliders = new List<Collider2D>(chosen.Count * 4);
        for (int i = 0; i < chosen.Count; i++)
        {
            Vector2 dest = chosen[i].point;
            Vector2 dir = (dest - origin).normalized;
            // apply upward bias with randomization
            float upBias = launchUpwardBias + (randomUpBiasJitter != 0f ? Random.Range(-randomUpBiasJitter, randomUpBiasJitter) : 0f);
            if (Mathf.Abs(upBias) > 0.0001f)
            {
                dir = (dir + Vector2.up * upBias).normalized;
            }

            GameObject go = Object.Instantiate(minionPrefab, origin, Quaternion.identity);
            WallStickyMinion m = go.GetComponent<WallStickyMinion>();
            if (m != null)
            {
                m.wallMask = wallMask;
                if (m.bulletPrefab == null && fallbackBulletPrefab != null)
                {
                    m.bulletPrefab = fallbackBulletPrefab;
                }
                m.Launch(dir, transform);
                any = true;
            }

            // Ensure minions don't collide with each other: ignore collisions between this and previously spawned ones
            var cols = go.GetComponentsInChildren<Collider2D>(includeInactive: true);
            if (cols != null && cols.Length > 0)
            {
                for (int c = 0; c < cols.Length; c++)
                {
                    Collider2D col = cols[c];
                    if (col == null) continue;
                    for (int p = 0; p < spawnedColliders.Count; p++)
                    {
                        Collider2D prev = spawnedColliders[p];
                        if (prev == null) continue;
                        Physics2D.IgnoreCollision(col, prev, true);
                    }
                    spawnedColliders.Add(col);
                }
            }
        }

        // Remember chosen destinations for future biasing (dedupe close points)
        for (int i = 0; i < chosen.Count; i++)
        {
            Vector2 p = chosen[i].point;
            bool exists = false;
            for (int r = 0; r < _recentDestinations.Count; r++)
            {
                if (Vector2.Distance(_recentDestinations[r], p) < 0.75f) { exists = true; break; }
            }
            if (!exists) _recentDestinations.Add(p);
        }
        // Cap memory size
        int overflow = _recentDestinations.Count - Mathf.Max(0, memorySize);
        if (overflow > 0)
        {
            _recentDestinations.RemoveRange(0, overflow);
        }

        return any;
    }

    private bool ScanForWallDirection(Vector2 origin, out Vector2 chosenDir)
    {
        float bestDist = float.MaxValue;
        Vector2 bestDir = Vector2.right;
        for (int i = 0; i < scanRays; i++)
        {
            float angle = (360f / scanRays) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, scanRadius, wallMask);
            if (hit.collider != null)
            {
                if (minDotToForward > -0.99f)
                {
                    Vector2 forward = transform.right;
                    float dot = Vector2.Dot(dir, forward);
                    if (dot < minDotToForward) continue;
                }
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestDir = dir;
                }
            }
        }
        chosenDir = bestDir;
        return bestDist < float.MaxValue;
    }

    private struct Candidate
    {
        public Vector2 dir;
        public Vector2 point;
        public float dist;
        public float score;
    }

    private List<Candidate> BuildCandidates(Vector2 origin, Vector2 baseDir)
    {
        var list = new List<Candidate>(scanRays);
        for (int i = 0; i < scanRays; i++)
        {
            float angle = (360f / scanRays) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (minDotToForward > -0.99f)
            {
                float dot = Vector2.Dot(dir, (Vector2)transform.right);
                if (dot < minDotToForward) continue;
            }
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, scanRadius, wallMask);
            if (hit.collider == null) continue;
            Candidate c;
            c.dir = dir;
            c.point = hit.point;
            c.dist = hit.distance;
            c.score = c.dist + fartherBias * MinDistanceToRecent(c.point);
            list.Add(c);
        }
        // Prefer farther and away from recent
        list.Sort((a, b) => b.score.CompareTo(a.score));
        return list;
    }

    private float MinDistanceToRecent(Vector2 p)
    {
        float min = float.MaxValue;
        for (int i = 0; i < _recentDestinations.Count; i++)
        {
            float d = Vector2.Distance(p, _recentDestinations[i]);
            if (d < min) min = d;
        }
        return min == float.MaxValue ? 0f : min;
    }

    private List<Candidate> ChooseDestinations(List<Candidate> candidates, int count)
    {
        var chosen = new List<Candidate>(count);
        float required = Mathf.Max(0.1f, minSeparation);
        for (int i = 0; i < candidates.Count && chosen.Count < count; i++)
        {
            Candidate c = candidates[i];
            bool tooClose = false;
            for (int k = 0; k < chosen.Count; k++)
            {
                if (Vector2.Distance(c.point, chosen[k].point) < required)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                chosen.Add(c);
            }
        }

        // If not enough, relax spacing requirement once
        if (chosen.Count < count)
        {
            for (int i = 0; i < candidates.Count && chosen.Count < count; i++)
            {
                Candidate c = candidates[i];
                bool exists = false;
                for (int k = 0; k < chosen.Count; k++)
                {
                    if (Vector2.Distance(c.point, chosen[k].point) < 0.25f) { exists = true; break; }
                }
                if (!exists) chosen.Add(c);
            }
        }
        return chosen;
    }

    // --- Gizmos ---
    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoScanColor = new Color(1f, 0.92f, 0.16f, 0.85f); // yellow
    public Color gizmoRayHitColor = new Color(1f, 0.4f, 0.2f, 0.95f); // orange-red
    public Color gizmoRayMissColor = new Color(1f, 1f, 1f, 0.2f); // white transparent
    public Color gizmoBestColor = new Color(0.1f, 1f, 0.3f, 1f); // green
    public Color gizmoForwardColor = new Color(0.2f, 0.8f, 1f, 0.9f); // cyan

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform fp = firePoint != null ? firePoint : transform;
        Vector3 origin = fp.position;

        // Scan radius
        Gizmos.color = gizmoScanColor;
        Gizmos.DrawWireSphere(origin, scanRadius);

        // Forward indicator
        Gizmos.color = gizmoForwardColor;
        Vector3 fwd = transform.right;
        Gizmos.DrawLine(origin, origin + fwd * 1.5f);

        // Cast rays as in ScanForWallDirection and visualize
        float bestDist = float.MaxValue;
        Vector2 bestDir = Vector2.right;
        for (int i = 0; i < Mathf.Max(1, scanRays); i++)
        {
            float angle = (360f / Mathf.Max(1, scanRays)) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            bool withinCone = true;
            if (minDotToForward > -0.99f)
            {
                float dot = Vector2.Dot(dir, (Vector2)transform.right);
                withinCone = dot >= minDotToForward;
            }

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, scanRadius, wallMask);
            if (hit.collider != null && withinCone)
            {
                Gizmos.color = gizmoRayHitColor;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.08f);
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestDir = dir;
                }
            }
            else
            {
                Gizmos.color = gizmoRayMissColor;
                Vector3 end = (Vector3)(dir.normalized * scanRadius) + origin;
                Gizmos.DrawLine(origin, end);
            }
        }

        // Chosen best direction
        if (bestDist < float.MaxValue)
        {
            Gizmos.color = gizmoBestColor;
            Vector3 end = origin + (Vector3)(bestDir.normalized * Mathf.Max(0.75f, Mathf.Min(bestDist, scanRadius)));
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawSphere(end, 0.1f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        // Duplicate the selected gizmo drawing so you can see it without selecting
        Transform fp = firePoint != null ? firePoint : transform;
        Vector3 origin = fp.position;

        Gizmos.color = gizmoScanColor;
        Gizmos.DrawWireSphere(origin, scanRadius);

        Gizmos.color = gizmoForwardColor;
        Vector3 fwd = transform.right;
        Gizmos.DrawLine(origin, origin + fwd * 1.5f);

        float bestDist = float.MaxValue;
        Vector2 bestDir = Vector2.right;
        for (int i = 0; i < Mathf.Max(1, scanRays); i++)
        {
            float angle = (360f / Mathf.Max(1, scanRays)) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            bool withinCone = true;
            if (minDotToForward > -0.99f)
            {
                float dot = Vector2.Dot(dir, (Vector2)transform.right);
                withinCone = dot >= minDotToForward;
            }

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, scanRadius, wallMask);
            if (hit.collider != null && withinCone)
            {
                Gizmos.color = gizmoRayHitColor;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.08f);
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestDir = dir;
                }
            }
            else
            {
                Gizmos.color = gizmoRayMissColor;
                Vector3 end = (Vector3)(dir.normalized * scanRadius) + origin;
                Gizmos.DrawLine(origin, end);
            }
        }

        if (bestDist < float.MaxValue)
        {
            Gizmos.color = gizmoBestColor;
            Vector3 end = origin + (Vector3)(bestDir.normalized * Mathf.Max(0.75f, Mathf.Min(bestDist, scanRadius)));
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawSphere(end, 0.1f);
        }
    }
}

static class WallMinionSkillMath
{
    public static Vector2 Rotate2D(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos).normalized;
    }
}


