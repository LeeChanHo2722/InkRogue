using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class BossAttackController
{
    private Tilemap arenaGround;
    public int CompletedPatternCycles { get; private set; }
    public string CurrentPatternAction { get; private set; }

    private IEnumerator TowerPattern(int phase, int cycle)
    {
        if (player == null) yield break;
        if (arenaGround == null)
            foreach (var refs in FindObjectsByType<BossSceneReferences>(FindObjectsSortMode.None))
                if (refs.bossArenaRoot != null && refs.bossArenaRoot.activeInHierarchy) { arenaGround = refs.bossGround; break; }
        switch (towerPattern)
        {
            case TowerBossPattern.Sludge:
                // Locked footprints punish backtracking, not movement after the warning.
                CurrentPatternAction = "Sludge footprints";
                for (int i = 0; i < 2 + phase; i++)
                {
                    Vector2 mark = ArenaPoint(player.position);
                    TowerBossZone.Create(transform, mark, mark, 1.05f, 1f, bombDamage);
                    yield return MoveBoss(player.position, .55f, .65f);
                }
                yield return new WaitForSeconds(1.1f);
                break;
            case TowerBossPattern.Duct:
                CurrentPatternAction = "Duct flank";
                Vector2 offset = (Vector2)(player.position - transform.position);
                Vector2 side = new Vector2(-offset.y, offset.x).normalized * (cycle % 2 == 0 ? 2.5f : -2.5f);
                yield return MoveBoss((Vector2)transform.position + side, 1f, 3f);
                float aim = GetAngleToPlayer();
                CurrentPatternAction = "Duct locked fan";
                yield return WarnAim(aim, .85f);
                for (int burst = 0; burst < 2 + phase; burst++)
                {
                    for (int lane = -1; lane <= 1; lane++) FireProjectile(aim + lane * 17f);
                    yield return new WaitForSeconds(.22f);
                }
                HideAimLine();
                break;
            case TowerBossPattern.Compactor:
                CurrentPatternAction = "Compactor charge warning";
                Vector2 from = transform.position;
                Vector2 delta = (Vector2)player.position - from;
                Vector2 direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? new Vector2(Mathf.Sign(delta.x), 0) : new Vector2(0, Mathf.Sign(delta.y));
                Vector2 end = SafeMove(from, ArenaPoint(from + direction * 7f));
                TowerBossZone.Create(transform, from, end, .8f, 1.1f, bombDamage);
                yield return new WaitForSeconds(1.1f);
                CurrentPatternAction = "Compactor charge";
                yield return MoveBoss(end, .6f, 13f);
                // The recovery window leaves a stationary target after the committed charge.
                CurrentPatternAction = "Compactor recovery";
                yield return new WaitForSeconds(1.2f);
                break;
            case TowerBossPattern.Core:
                CurrentPatternAction = "Core safe-gap ring";
                float gap = GetAngleToPlayer() + (cycle % 2 == 0 ? 0 : 90f);
                // Green ray names the safe sector; red rays remain reserved for danger.
                yield return WarnAim(gap, 1f, true);
                for (int ring = 0; ring < 2 + phase; ring++)
                {
                    for (int lane = 0; lane < 16; lane++)
                    {
                        float angle = gap + lane * 22.5f;
                        if (Mathf.Abs(Mathf.DeltaAngle(gap, angle)) > 34f) FireProjectile(angle);
                    }
                    yield return new WaitForSeconds(.32f);
                }
                HideAimLine();
                CurrentPatternAction = "Core cross warning";
                Vector2 center = ArenaPoint(transform.position);
                Vector2 axis = cycle % 2 == 0 ? Vector2.right : new Vector2(1, 1).normalized;
                Vector2 other = new Vector2(-axis.y, axis.x);
                TowerBossZone.Create(transform, ArenaPoint(center - axis * 8), ArenaPoint(center + axis * 8), .55f, 1.2f, bombDamage);
                TowerBossZone.Create(transform, ArenaPoint(center - other * 8), ArenaPoint(center + other * 8), .55f, 1.2f, bombDamage);
                yield return new WaitForSeconds(1.45f);
                break;
        }
        CurrentPatternAction = "Recovery";
        CompletedPatternCycles++;
    }

    private IEnumerator WarnAim(float angle, float duration, bool safe = false)
    {
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            DrawAimLine(angle, .9f);
            if (aimLine != null)
            {
                float spread = safe ? 34f : 17f;
                Vector3 origin = firePoint.position;
                float left = (angle - spread) * Mathf.Deg2Rad, right = (angle + spread) * Mathf.Deg2Rad;
                aimLine.positionCount = 4;
                aimLine.SetPositions(new[] { origin,
                    origin + new Vector3(Mathf.Cos(left), Mathf.Sin(left)) * aimLineLength,
                    origin + new Vector3(Mathf.Cos(right), Mathf.Sin(right)) * aimLineLength, origin });
                aimLine.startColor = aimLine.endColor = safe ? Color.green : new Color(1f, .85f, .15f);
            }
            yield return null;
        }
    }

    private Vector2 ArenaPoint(Vector2 point)
    {
        if (arenaGround == null) return point;
        Bounds bounds = arenaGround.localBounds;
        Vector3 min = arenaGround.transform.TransformPoint(bounds.min);
        Vector3 max = arenaGround.transform.TransformPoint(bounds.max);
        return new Vector2(Mathf.Clamp(point.x, min.x + .9f, max.x - .9f), Mathf.Clamp(point.y, min.y + .9f, max.y - .9f));
    }

    private Vector2 SafeMove(Vector2 from, Vector2 destination)
    {
        Vector2 delta = destination - from;
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .25f));
        Vector2 last = from;
        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, destination, (float)i / steps);
            if (arenaGround != null && !arenaGround.HasTile(arenaGround.WorldToCell(point))) break;
            bool blocked = false;
            foreach (var hit in Physics2D.OverlapCircleAll(point, .65f))
                if (!hit.isTrigger && hit.CompareTag("Obstacle")) { blocked = true; break; }
            if (blocked) break;
            last = point;
        }
        return last;
    }

    private IEnumerator MoveBoss(Vector2 destination, float duration, float speed)
    {
        destination = ArenaPoint(destination);
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            Vector2 from = transform.position;
            transform.position = SafeMove(from, Vector2.MoveTowards(from, destination, speed * Time.deltaTime));
            yield return null;
        }
    }

    private void ClearTowerWarnings()
    {
        foreach (var zone in GetComponentsInChildren<TowerBossZone>())
        { zone.gameObject.SetActive(false); Destroy(zone.gameObject); }
        CurrentPatternAction = "Stopped";
    }
}

// One warning describes exactly the capsule that activates once. Owned by the boss,
// so death, phase transitions and stopping combat cancel every pending zone.
public sealed class TowerBossZone : MonoBehaviour
{
    private Vector2 from, to;
    private float radius, warning, damage, elapsed;
    private LineRenderer outline;
    public bool Activated { get; private set; }

    public static TowerBossZone Create(Transform owner, Vector2 from, Vector2 to, float radius, float warning, float damage)
    {
        var zone = new GameObject("Boss danger zone").AddComponent<TowerBossZone>();
        zone.transform.SetParent(owner, false);
        zone.from = from; zone.to = to; zone.radius = radius; zone.warning = warning; zone.damage = damage;
        zone.outline = zone.gameObject.AddComponent<LineRenderer>();
        zone.outline.useWorldSpace = true; zone.outline.loop = true; zone.outline.positionCount = 26;
        zone.outline.widthMultiplier = .14f; zone.outline.sortingOrder = 115;
        var renderer = owner.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) zone.outline.sortingLayerID = renderer.sortingLayerID;
        zone.outline.material = new Material(Shader.Find("Sprites/Default"));
        float angle = Mathf.Atan2((to - from).y, (to - from).x);
        for (int i = 0; i < 26; i++)
        {
            bool first = i < 13;
            float radians = angle + (first ? -.5f : .5f) * Mathf.PI + (i % 13) * Mathf.PI / 12;
            zone.outline.SetPosition(i, (first ? to : from) + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius);
        }
        zone.outline.startColor = zone.outline.endColor = new Color(1, .85f, .15f, .9f);
        return zone;
    }

    private void Update()
    {
        if (Time.timeScale <= 0) return;
        elapsed += Time.deltaTime;
        if (elapsed < warning)
        {
            outline.startColor = outline.endColor = new Color(1, .85f, .15f, .65f + .35f * Mathf.PingPong(elapsed * 4, 1));
            return;
        }
        if (Activated) return;
        Activated = true;
        outline.startColor = outline.endColor = Color.white;
        outline.widthMultiplier = .22f;
        var shield = FindAnyObjectByType<PlayerShield>();
        if (shield != null)
        {
            Vector2 delta = to - from;
            float t = delta.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot((Vector2)shield.transform.position - from, delta) / delta.sqrMagnitude) : 0;
            Vector2 closest = from + delta * t;
            var collider = shield.GetComponent<Collider2D>();
            Vector2 body = collider != null ? collider.ClosestPoint(closest) : (Vector2)shield.transform.position;
            if (Vector2.Distance(body, closest) <= radius && TowerPowers.ClearLine(closest, shield.transform.position)) shield.TakeDamage(damage, closest);
        }
        int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .5f));
        for (int i = 0; i <= count; i++) InkMap.Instance?.PaintCircle(Vector2.Lerp(from, to, (float)i / count), radius, InkTeam.Enemy);
        Destroy(gameObject, .2f);
    }
    private void OnDestroy() { if (outline != null) Destroy(outline.material); }
}
