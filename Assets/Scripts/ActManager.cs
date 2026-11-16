using System;
using System.Collections.Generic;
using UnityEngine;

public class ActManager : MonoBehaviour
{
    //TODO: bien co de check luc new game
    //  [SerializeField] private List<ActData> actList = new();
    [SerializeField] private ActData data;
    public static ActManager instance;
    public static event Action<ActData> OnActStarted;
    //TODO: [SerializeField] private Vector2 position;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        OnActStarted?.Invoke(data);
        //TODO: PlayerMovement.instance.transform.position = postion;
    }
}
