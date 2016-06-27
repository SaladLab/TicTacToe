using System;
using ProtoBuf;
using TypeAlias;

namespace Domain
{
    public enum ResultCodeType
    {
        None = 0,
        ArgumentError = 1,
        InternalError = 2,
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
        AlreadyPairingRegistered,
    }

    [ProtoContract, TypeAlias]
    public class ResultException : Exception
    {
        [ProtoMember(1)] public ResultCodeType ResultCode;
        [ProtoMember(2)] public string AdditionalMessage;

        public ResultException()
        {
        }

        public ResultException(ResultCodeType resultCode, string additionalMessage = null)
        {
            ResultCode = resultCode;
            AdditionalMessage = additionalMessage;
        }

        public override string ToString()
        {
            return string.Format("!{0}!", ResultCode);
        }
    }
}
