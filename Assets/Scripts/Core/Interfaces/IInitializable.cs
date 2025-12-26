using Cysharp.Threading.Tasks; // 비동기(UniTask) 사용

public interface IInitializable
{
    // "보급품 가방(context) 줄 테니까 일할 준비 해!"
    // 준비가 끝날 때까지 기다려줄게 (UniTask)
    UniTask Initialize(InitializationContext context);
}