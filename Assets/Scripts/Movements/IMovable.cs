using UnityEngine;

public interface IMovable
{
    float GetCurrentSpeed();
    void PushBack(float distance);
    void SetMovementBlocked(bool blocked);
    void ApplyStunAnimation(float duration);
    bool IsPlayer { get; }
    bool IsStunned { get; }
    Transform MoverTransform { get; }
}
