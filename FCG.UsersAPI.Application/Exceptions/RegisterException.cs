namespace FCG.UsersAPI.Application.Exceptions
{
    public class RegisterException : Exception
    {
        public RegisterException() : base("Erro durante registro do usuário")
        {
        }
        public RegisterException(string message) : base(message)
        {
        }

        public RegisterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
