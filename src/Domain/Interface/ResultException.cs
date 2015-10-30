using System;
using ProtoBuf;
using TypeAlias;

namespace Domain.Interfaced
{
    public enum ResultCodeType
    {
        None = 0,
        LoginFailedNoUser = 10,
        LoginFailedIncorrectPassword,
        LoginFailedAlreadyConnected,
        NeedToBeInGame = 20,
        NeedToBeOutOfGame,
        NotYourTurn,
        BadPosition,
        GameStarted,
        GamePlayerFull,
        GameNotFound,
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
