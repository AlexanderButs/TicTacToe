using System;

namespace TicTacToe.Exceptions
{
    public class IncorrectMoveException : Exception
    {
        public IncorrectMoveException(string message)
            : base(message)
        {
        }
    }
}