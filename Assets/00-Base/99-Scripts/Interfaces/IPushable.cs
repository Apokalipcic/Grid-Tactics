using UnityEngine;

public interface IPushable
{
    bool CanBePushed(Vector3 direction);
    void Push(Vector3 direction);

    void ReturnPushObjectOrigin();
    void UndoPushObject();
}