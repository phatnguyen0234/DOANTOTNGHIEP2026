using System;
using UnityEngine;

// Lớp điều khiển máy trạng thái câu cá (Finite State Machine)
// Quản lý việc chuyển đổi trạng thái an toàn và phát sự kiện thông báo
public class FishingStateMachine
{
    public FishingState CurrentState { get; private set; } = FishingState.Idle;
    public FishingState PreviousState { get; private set; } = FishingState.Idle;

    // Sự kiện phát ra khi trạng thái thay đổi: (trạng thái cũ, trạng thái mới)
    public event Action<FishingState, FishingState> OnStateChanged;

    public void ChangeState(FishingState newState)
    {
        if (CurrentState == newState) return;

        PreviousState = CurrentState;
        CurrentState = newState;

        OnStateChanged?.Invoke(PreviousState, CurrentState);
    }

    // Reset về trạng thái nghỉ Idle
    public void ResetToIdle()
    {
        ChangeState(FishingState.Idle);
    }

    public bool IsState(FishingState state)
    {
        return CurrentState == state;
    }
}
