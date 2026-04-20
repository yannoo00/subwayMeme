using UnityEngine;
using UnityEngine.InputSystem;




public enum GameState
{
    UI,
    Playing,
}


public class GameManager : MonoBehaviour
{
    //singleton
    public static GameManager Instance { get; private set; } 

    //state
    private GameState _currentState;
    public GameState CurrentState => _currentState; 

    
    // 생명주기
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    private void Start()
    {
        ChangeState(GameState.UI);
    }

    private void Update()
    {


    } 



    //public methods 

    public void ChangeState(GameState newState)
    {
        _currentState = newState;

    
        switch (newState)
        {
            case GameState.UI:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
        }
    }



    //게임 진행 관련
    public void StartGame(int seed)
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Playing);

        StageManager.Instance._TryGame(seed);
    }

    public void PauseGame()
    {
        if (_currentState == GameState.Playing)
        {
            ChangeState(GameState.UI);
        }
    }


    public void ResumeGame()
    {
        if(_currentState == GameState.UI)
        {
            Time.timeScale = 1f;
            ChangeState(GameState.Playing);
        }
    }

    
    public void EndGame()
    {
        ChangeState(GameState.UI);
    }

}

