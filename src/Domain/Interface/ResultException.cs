using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;

namespace Domain.Interfaced
{
    public enum ResultCodeType
    {
        None = 0,
        LoginFailedNoUser = 1,
        LoginFailedIncorrectPassword = 2,
        LoginFailedAlreadyConnected = 3,
        NeedToBeInRoom = 4,
        NeedToBeOutOfRoom = 5,
        RoomRemoved = 6,
        UserNotMyself = 7,
        UserNotOnline = 8,
        UserAlreadyHere = 9
    }

    [ProtoContract, TypeAlias]
    public class ResultException : Exception
    {
        [ProtoMember(1)] public ResultCodeType ResultCode;

        public ResultException()
        {
        }

        public ResultException(ResultCodeType resultCode)
        {
            ResultCode = resultCode;
        }

        public override string ToString()
        {
            return string.Format("ResultException({0})", ResultCode);
        }
    }
}
