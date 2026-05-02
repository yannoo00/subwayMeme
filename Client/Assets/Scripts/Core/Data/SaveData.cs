using System;

[Serializable]
public class SaveData
{
    public int gold;
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
}

[Serializable]
public class CharacterSaveData
{
    public int characterId;
    public int[] upgradeLevels = new int[(int)UpgradeType.Count];
}
