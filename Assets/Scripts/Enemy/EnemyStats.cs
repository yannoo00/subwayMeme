using System;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _maxHealth = 100;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0;

    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0); 
        Debug.Log($"[EnemyStats] {gameObject.name} 피격! HP: {_currentHealth}/{_maxHealth}");

        if (!IsAlive)
        {
            Die();
        }
    }

    public void Die()
    {
        CombatEvents.EnemyDied(gameObject);
        Destroy(gameObject);
    }
}