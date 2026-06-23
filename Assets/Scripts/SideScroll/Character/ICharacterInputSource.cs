namespace GameCreate3
{
    public interface ICharacterInputSource
    {
        float MoveX { get; }
        bool JumpPressed { get; }
        bool InteractPressed { get; }
        void Tick();
    }
}
