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
        NeedToBeInGame = 4,
        NeedToBeOutOfGame = 5,
        GameNotFound = 6,
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
            return string.Format("!{0}!", ResultCode);
        }
    }
}
