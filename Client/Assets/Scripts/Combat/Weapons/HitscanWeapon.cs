using System.Collections.Generic;
using UnityEngine;

// 레이캐스트 기반 원거리 무기 (총기)
// 카메라 forward를 조준 기준선으로 사용. 트레이서/머즐 플래시는 _muzzle 위치에서 그림
public class HitscanWeapon : WeaponBase
{
    [Header("Hitscan Settings")]
    [SerializeField] private Transform _muzzle;       // 트레이서/이펙트 시작점 (조준선과 분리)
    [SerializeField] private LayerMask _hitMask;      // 피격 대상 레이어

    [Header("Debug")]
    [SerializeField] private bool _showDebugRay = false;


    private HitscanWeaponData Data => (HitscanWeaponData)_weaponData;

    private Camera _aimCamera;
    private int _currentAmmo;
    private float _reloadEndTime;       // Time.time 기준 재장전 완료 시각. 0이면 비재장전 상태

    private bool IsReloading => _reloadEndTime > 0f && Time.time < _reloadEndTime;
    private bool IsAmmoEmpty => _currentAmmo <= 0;

    public int CurrentAmmo => _currentAmmo;
    public int MagazineSize => Data != null ? Data.magazineSize : 0;


    public override void Equip()
    {
        base.Equip();

        // 로컬 플레이어 카메라 1개만 MainCamera 태그를 갖는다고 가정
        // 멀티플레이 시 자기 카메라만 태그하면 그대로 동작
        if (_aimCamera == null) _aimCamera = Camera.main;

        // 처음 장착 시 만탄으로 시작. 재줍기 후 탄약 정책은 추후 변경 가능
        if (_currentAmmo == 0 && !IsReloading)
            _currentAmmo = Data.magazineSize;

        // HUD 동기화 - 장착 시점 탄약/재장전 상태 알림
        PlayerEvents.AmmoChanged(_currentAmmo, Data.magazineSize);

        // 재장전 도중 다른 슬롯으로 갔다가 돌아온 경우 - 진행률 바 복구
        // 남은 시간만큼을 새 duration으로 보냄. 바는 0%부터 다시 차오르지만 완료 타이밍은 정확
        if (IsReloading)
            PlayerEvents.ReloadStarted(_reloadEndTime - Time.time);
    }


    private void Update()
    {
        // 재장전 완료 시점 체크 - 코루틴 대신 타이머 비교로 처리
        // Unequip(GameObject 비활성)되면 Update 자체가 안 돌아 자연스럽게 일시정지된다
        if (_reloadEndTime > 0f && Time.time >= _reloadEndTime)
        {
            _currentAmmo = Data.magazineSize;
            _reloadEndTime = 0f;

            // 순서 중요: ReloadFinished 먼저 → AmmoChanged 나중
            // HUD가 "RELOADING" → "12/12"로 자연스럽게 갱신되도록
            PlayerEvents.ReloadFinished();
            PlayerEvents.AmmoChanged(_currentAmmo, Data.magazineSize);
        }
    }


    protected override void PerformAttack()
    {
        if (IsReloading) return;
        if (IsAmmoEmpty)
        {
            // 빈 탄창 상태에서 발사 시 자동 재장전 진입
            TryReload();
            return;
        }

        _currentAmmo--;
        PlayerEvents.AmmoChanged(_currentAmmo, Data.magazineSize);

        Vector3 origin = _aimCamera.transform.position;
        Vector3 direction = ApplySpread(_aimCamera.transform.forward, Data.spread);

        var hitEnemyIds = new List<int>();

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Data.range, _hitMask))
        {
            var enemy = hit.collider.GetComponentInParent<Enemy>();
            bool isNetworkEnemy = enemy != null && enemy.NetworkId > 0;

            if (!isNetworkEnemy && hit.collider.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(Data.damage);

            if (isNetworkEnemy)
                hitEnemyIds.Add(enemy.NetworkId);
        }

        // C_Attack 재사용 - 트레이서는 다른 플레이어에 동기화하지 않음(개인 아이템 정책)
        var pkt = new GameProto.C_Attack { WeaponId = 0, Damage = Data.damage };
        pkt.HitEnemyIds.AddRange(hitEnemyIds);
        NetworkManager.Instance.SendGame(GameProto.GamePacketId.CAttack, pkt);

        if (_showDebugRay)
            Debug.DrawRay(origin, direction * Data.range, Color.yellow, 0.5f);
    }


    public override void TryReload()
    {
        if (IsReloading) return;
        if (_currentAmmo == Data.magazineSize) return;

        _reloadEndTime = Time.time + Data.reloadTime;
        PlayerEvents.ReloadStarted(Data.reloadTime);
    }


    // 카메라 로컬 축(up/right) 기준으로 forward를 흩뿌림. 카메라 회전과 무관하게 cone 균등 분포
    private Vector3 ApplySpread(Vector3 forward, float spreadDegrees)
    {
        if (spreadDegrees <= 0f) return forward;

        float halfSpread = spreadDegrees * 0.5f;
        float yaw = Random.Range(-halfSpread, halfSpread);
        float pitch = Random.Range(-halfSpread, halfSpread);

        Quaternion rot = Quaternion.AngleAxis(yaw, _aimCamera.transform.up)
                       * Quaternion.AngleAxis(pitch, _aimCamera.transform.right);

        return rot * forward;
    }
}
