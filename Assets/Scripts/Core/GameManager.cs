using UnityEngine;
using UnityEngine.InputSystem;




public enum GameState
{
    Menu,
    Playing,
    Station,
    Paused,
    GameOver,
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
        ChangeState(GameState.Menu);
    }

    private void Update()
    {



        //디버그용으로 Enter를 누르면 넘어가게 해놧음
        if (_currentState == GameState.Menu && Keyboard.current.enterKey.isPressed)
        {
            StartGame();
        }
    } 



    //public methods 

    public void ChangeState(GameState newState)
    {
        _currentState = newState;

    
        switch (newState)
        {
            case GameState.Menu:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case GameState.Station:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.GameOver:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }



    //게임 진행 관련
    public void StartGame()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
        
        StageManager.Instance._TryGame();
    }

    public void PauseGame()
    {
        if (_currentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
    }


    public void ResumeGame()
    {
        if(_currentState == GameState.Paused)
        {
            Time.timeScale = 1f;
            ChangeState(GameState.Playing);
        }
    }

    
    public void EndGame()
    {
        ChangeState(GameState.GameOver);
    }

}

