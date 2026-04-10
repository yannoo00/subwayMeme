using System;
using UnityEngine;
using System.Collections.Concurrent; 

public class MainThreadDispatcher : MonoBehaviour
{

    public static MainThreadDispatcher Instance {get; private set; }

    private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
            DontDestroyOnLoad(gameObject);
        }
    }
    
    void Update()
    {
        //매 프레임 큐에 쌓인 Action 전부 꺼내 실행
        // (TryDequeue는 큐가 비어있으면 false 반환)
        while(_queue.TryDequeue(out Action action))
            action?.Invoke();   
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;
        _queue.Enqueue(action);
    }

}
