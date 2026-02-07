using UnityEngine;

public class PlayerStats: MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _maxHealth = 100;


    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0;



    public void TakeDamage(int damage)
    {
        if(!IsAlive) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        if (!IsAlive)
        {
            Die();
        }        
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
