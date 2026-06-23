namespace GameCreate3
{
    public sealed class DisabledInputSource : ICharacterInputSource
    {
        public float MoveX => 0f;
        public bool JumpPressed => false;
        public bool InteractPressed => false;

        public void Tick()
        {
        }
    }
}
