using UnityEngine;

public interface IMovable
{
    float GetCurrentSpeed();
    void PushBack(float distance);
    void SetMovementBlocked(bool blocked);
    bool IsPlayer { get; }
    Transform MoverTransform { get; }
}
