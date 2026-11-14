namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Хвост дороги — враг прошлой волны, которого быстрый враг новой волны мог бы догнать: его скорость и момент выхода
    // последнего из звена, в секундах от начала волны, относительно которой хвост сейчас записан.
    public class EndlessRoadTail
    {
        public EndlessRoadTail(float speed, float exitTime)
        {
            Speed = speed;
            ExitTime = exitTime;
        }

        public float Speed { get; }
        public float ExitTime { get; }
    }
}
