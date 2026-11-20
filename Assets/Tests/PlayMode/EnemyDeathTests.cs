using System.Collections;
using NUnit.Framework;
using Platformer.Core;
using Platformer.Gameplay;
using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.TestTools;

// Тестовая сборка PlayModeTests — индивидуальный вариант №14:
// «EnemyDeath PlayMode: прыжок на врага сверху генерирует событие EnemyDeath»
public class EnemyDeathTests
{
    GameObject enemyObj;
    GameObject playerObj;
    int enemyCollisionCount;
    int enemyDeathCount;

    // PLAYMODE ТЕСТ (вариант №14): игрок прыгает на врага сверху —
    // враг должен погибнуть через событие PlayerEnemyCollision -> EnemyDeath
    [UnityTest]
    public IEnumerator PlayMode_JumpOnEnemyFromAbove_TriggersEnemyDeath()
    {
        // Сбрасываем очередь событий симуляции, накопленную другими тестами
        Simulation.Clear();

        // --- Враг: BoxCollider2D + Rigidbody2D + EnemyController (как Enemy.prefab, без Health) ---
        enemyObj = new GameObject("TestEnemy");
        var enemyCollider = enemyObj.AddComponent<BoxCollider2D>();
        enemyCollider.size = new Vector2(1f, 1f);
        enemyObj.AddComponent<Rigidbody2D>(); // обязателен для KinematicObject (OnEnable)
        // RequireComponent сам добавит AnimationController (и его SpriteRenderer + Animator)
        EnemyController enemy = enemyObj.AddComponent<EnemyController>();
        enemyObj.transform.position = Vector3.zero; // верх коллайдера: Bounds.max.y = 0.5

        // --- Игрок: расположен НАД врагом, как в момент приземления после прыжка ---
        playerObj = new GameObject("TestPlayer");
        var playerCollider = playerObj.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(1f, 1f);
        playerObj.AddComponent<Rigidbody2D>(); // обязателен для KinematicObject (OnEnable)
        playerObj.AddComponent<Animator>();    // ComputeVelocity() игрока пишет в animator
        PlayerController player = playerObj.AddComponent<PlayerController>();
        // Отключаем скриптовую гравитацию, чтобы игрок «завис» в точке прыжка:
        // иначе паразитное касание вызовет второе PlayerEnemyCollision
        player.gravityModifier = 0f;
        playerObj.transform.position = new Vector3(0f, 1.5f, 0f); // Bounds.center.y = 1.5

        yield return null; // даём Awake/OnEnable компонентов отработать один кадр
        Physics2D.SyncTransforms(); // синхронизируем позиции с физикой: Bounds уже актуальны

        // Счётчики выполнения событий симуляции (хуки Simulation.Event<T>.OnExecute)
        enemyCollisionCount = 0;
        enemyDeathCount = 0;
        PlayerEnemyCollision.OnExecute += OnPlayerEnemyCollision;
        EnemyDeath.OnExecute += OnEnemyDeath;

        try
        {
            Assert.IsNull(enemy.GetComponent<Health>(),
                "Проверка сцены: у стандартного Enemy.prefab нет компонента Health");

            // Проверяем геометрию «прыжка сверху»: центр игрока выше верхней грани врага —
            // именно это условие в PlayerEnemyCollision.Execute означает прыжок на врага
            Assert.GreaterOrEqual(player.Bounds.center.y, enemy.Bounds.max.y,
                "Игрок должен находиться над врагом (условие willHurtEnemy)!");

            // При касании физика вызывает EnemyController.OnCollisionEnter2D, который
            // шедулит PlayerEnemyCollision. В тесте шедулим событие так же, как игра
            PlayerEnemyCollision ev = Simulation.Schedule<PlayerEnemyCollision>();
            ev.player = player;
            ev.enemy = enemy;

            // Прогоняем тик симуляции — в игре его делает GameController.Update
            yield return new WaitForSeconds(0.05f);
            Simulation.Tick();

            // --- Проверки индивидуального варианта №14 ---
            Assert.AreEqual(1, enemyCollisionCount,
                "Событие PlayerEnemyCollision должно выполниться ровно один раз!");
            Assert.AreEqual(1, enemyDeathCount,
                "Прыжок на врага сверху НЕ сгенерировал событие EnemyDeath!");
            Assert.IsFalse(enemyCollider.enabled,
                "EnemyDeath должен отключать коллайдер врага!");
            Assert.IsFalse(enemy.GetComponent<AnimationController>().enabled,
                "EnemyDeath должен отключать EnemyController врага!");
            Assert.AreEqual(1, 2, "DEMO: intentionally broken test");
            Assert.Greater(player.velocity.y, 0f,
                "Игрок должен отскочить вверх после прыжка на врага (Bounce)!");
        }
        finally
        {
            PlayerEnemyCollision.OnExecute -= OnPlayerEnemyCollision;
            EnemyDeath.OnExecute -= OnEnemyDeath;
        }

        // Удаляем временные объекты и пропускаем кадр
        Object.Destroy(playerObj);
        Object.Destroy(enemyObj);
        playerObj = null;
        enemyObj = null;
        yield return null;
    }

    void OnPlayerEnemyCollision(PlayerEnemyCollision ev) => enemyCollisionCount++;
    void OnEnemyDeath(EnemyDeath ev) => enemyDeathCount++;

    [TearDown]
    public void TearDown()
    {
        PlayerEnemyCollision.OnExecute -= OnPlayerEnemyCollision;
        EnemyDeath.OnExecute -= OnEnemyDeath;
        if (playerObj != null) Object.DestroyImmediate(playerObj);
        if (enemyObj != null) Object.DestroyImmediate(enemyObj);
    }
}
