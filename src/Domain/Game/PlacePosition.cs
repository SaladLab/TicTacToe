using ProtoBuf;

namespace Domain.Game
{
    [ProtoContract]
    public class PlacePosition
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Y;

        public PlacePosition() : this(0, 0)
        {
        }

        public PlacePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return string.Format("X={0} Y={1}", X, Y);
        }
    }
}