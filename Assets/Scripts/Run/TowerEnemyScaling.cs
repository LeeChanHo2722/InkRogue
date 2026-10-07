using UnityEngine;

public static class TowerEnemyScaling
{
    public static float Health(int floor) => 1f + Mathf.Max(0, floor-1) * 0.06f;
    public static float Damage(int floor) => 1f + Mathf.Max(0, floor-1) * 0.025f;
    // Speed approaches 1.6x so every attack remains dodgeable at high floors.
    public static float Speed(int floor) => 1f + 0.6f * (1f - Mathf.Exp(-Mathf.Max(0,floor-1)/40f));

    public static void Apply(GameObject enemy, RunManager run)
    {
        if (enemy == null || run == null || run.CurrentMode != RunMode.Endless) return;
        float speed=Speed(run.CurrentFloor), damage=Damage(run.CurrentFloor);
        if (enemy.TryGetComponent<EnemyMovement>(out var movement)) movement.moveSpeed *= speed;
        if (enemy.TryGetComponent<EnemyShooterMovement>(out var shooter)) shooter.moveSpeed *= speed;
        if (enemy.TryGetComponent<EnemyBomberMovement>(out var bomber)) bomber.moveSpeed *= speed;
        if (enemy.TryGetComponent<EnemySprinklerMovement>(out var sprinkler)) sprinkler.moveSpeed *= speed;
        if (enemy.TryGetComponent<EnemyContactDamage>(out var contact)) contact.damage = Mathf.Max(1,Mathf.RoundToInt(contact.damage*damage));
        if (enemy.TryGetComponent<EnemyBomberAttack>(out var bomb)) bomb.damage *= damage;
        if (enemy.TryGetComponent<EnemyTankAttack>(out var tank)) tank.damage = Mathf.Max(1,Mathf.RoundToInt(tank.damage*damage));
    }
}
