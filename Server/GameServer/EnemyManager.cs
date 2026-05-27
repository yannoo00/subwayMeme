using System;
using System.Collections.Generic;

namespace GameServer
{
    public class EnemyManager
    {
        // 싱글톤 제거: GameRoom 이 인스턴스로 소유. 룸별 독립 상태 관리.
        readonly object _lock = new();
        readonly Dictionary<int, int> _enemyHp = new(); // enemyId → currentHp

        public void RegisterEnemy(int enemyId, int maxHp)
        {
            lock (_lock)
                _enemyHp[enemyId] = maxHp;
        }

        // 데미지 적용. 등록되지 않은 id면 (-1, false) 반환
        public (int currentHp, bool isDead) ApplyDamage(int enemyId, int damage)
        {
            lock (_lock)
            {
                if (!_enemyHp.TryGetValue(enemyId, out int hp))
                    return (-1, false);

                hp = Math.Max(0, hp - damage);
                if (hp <= 0)
                    _enemyHp.Remove(enemyId);
                else
                    _enemyHp[enemyId] = hp;

                return (hp, hp <= 0);
            }
        }

        public void RemoveEnemy(int enemyId)
        {
            lock (_lock) _enemyHp.Remove(enemyId);
        }
    }
}
