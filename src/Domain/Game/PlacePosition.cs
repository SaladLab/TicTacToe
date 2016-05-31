using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class PlacePosition
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Y;

        public PlacePosition()
            : this(0, 0)
        {
        }

        public PlacePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var p = obj as PlacePosition;
            if (p == null)
                return false;

            return (X == p.X) && (Y == p.Y);
        }

        public bool Equals(PlacePosition p)
        {
            if (p == null)
                return false;

            return (X == p.X) && (Y == p.Y);
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public override string ToString()
        {
            return string.Format("X={0} Y={1}", X, Y);
        }
    }
}
