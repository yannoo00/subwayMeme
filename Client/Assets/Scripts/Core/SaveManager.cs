using System.IO;
using UnityEngine;

// 로컬 저장/로드 담당 싱글톤
// 씬 전환 시에도 유지되어야 하므로 DontDestroyOnLoad
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_FILE_NAME = "/save.json";

    public SaveData Current { get; private set; }

    // === Unity 생명주기 ===

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // === Public 메서드 ===

    public void Load()
    {
        string path = Application.persistentDataPath + SAVE_FILE_NAME;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Current = JsonUtility.FromJson<SaveData>(json);
            EnsureDataIntegrity();
        }
        else
        {
            Current = new SaveData();
        }
        Debug.Log($"[SaveManager] 로드 완료 - 경로: {path}, evolutionPoints: {Current.evolutionPoints}");
    }

    public void Save()
    {
        string path = Application.persistentDataPath + SAVE_FILE_NAME;
        string json = JsonUtility.ToJson(Current, true);
        File.WriteAllText(path, json);
        Debug.Log($"[SaveManager] 저장 완료 - evolutionPoints: {Current.evolutionPoints}");
    }

    // 개발/테스트용 저장 데이터 초기화
    public void DeleteSave()
    {
        string path = Application.persistentDataPath + SAVE_FILE_NAME;
        if (File.Exists(path)) File.Delete(path);
        Current = new SaveData();
        Debug.Log("[SaveManager] 저장 데이터 삭제");
    }

    // === Private 메서드 ===

    // 저장 파일 버전 불일치나 필드 누락 시 기본값으로 복구
    private void EnsureDataIntegrity()
    {
        if (Current == null) { Current = new SaveData(); return; }

        if (Current.characters == null || Current.characters.Length == 0)
        {
            Current.characters = new CharacterSaveData[]
            {
                new CharacterSaveData { characterId = 0 }
            };
            return;
        }

        foreach (var c in Current.characters)
        {
            int requiredLength = (int)UpgradeType.Count;
            if (c.upgradeLevels == null || c.upgradeLevels.Length < requiredLength)
            {
                var newLevels = new int[requiredLength];
                if (c.upgradeLevels != null)
                    System.Array.Copy(c.upgradeLevels, newLevels,
                        Mathf.Min(c.upgradeLevels.Length, requiredLength));
                c.upgradeLevels = newLevels;
            }
        }
    }
}
