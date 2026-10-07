using Fusion;

public enum InputButton { Jump = 0, Dash = 1 }

public struct PlayerInputData : INetworkInput
{
    public float Move;
    public NetworkButtons Buttons;
}