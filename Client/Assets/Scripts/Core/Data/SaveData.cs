using System;

[Serializable]
public class SaveData
{
    public int evolutionPoints;
    // 마지막으로 선택했던 캐릭터 - 게임 재실행 시 복원
    // -1: 아직 한 번도 선택 안 함 (신규 세이브 또는 마이그레이션)
    public int lastSelectedCharacterId = -1;
    public CharacterSaveData[] characters = new CharacterSaveData[]
    {
        new CharacterSaveData { characterId = 0 }
    };

    public CharacterSaveData GetCharacter(int characterId)
    {
        if (characters == null) return null;
        foreach (var c in characters)
            if (c.characterId == characterId) return c;
        return null;
    }

    // 신규 캐릭터를 처음 선택하면 저장 슬롯이 없으므로 자동 생성
    // 멀티 캐릭터 확장 시 캐릭터 추가/삭제마다 EnsureDataIntegrity를 손대지 않기 위함
    public CharacterSaveData GetOrCreateCharacter(int characterId)
    {
        var existing = GetCharacter(characterId);
        if (existing != null) return existing;

        var newChar = new CharacterSaveData { characterId = characterId };
        int oldLen = characters != null ? characters.Length : 0;
        var newArr = new CharacterSaveData[oldLen + 1];
        if (characters != null)
            System.Array.Copy(characters, newArr, oldLen);
        newArr[oldLen] = newChar;
        characters = newArr;
        return newChar;
    }
}

[Serializable]
public class CharacterSaveData
{
    public int characterId;
    public int[] upgradeLevels = new int[(int)UpgradeType.Count];
}
