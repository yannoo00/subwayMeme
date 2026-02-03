using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.XR;

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



    //public methods 

    public void ChangeState(GameState newState)
    {
        _currentState = newState;

    
        switch (newState)
        {
            case GameState.Menu:
                
                break;
                
            case GameState.Playing:
                
                break;

            case GameState.Station:
                
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                
                break;
        }
    }




    public void StartGame()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
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

