namespace FCG.UsersAPI.Application.DTOs
{
    public class RegisterResultDto
    {
        public bool Success { get; private set; }
        public string? Message { get; private set; }
        public RegisterResponseDto? Data { get; private set; }

        public RegisterResultDto(bool success, RegisterResponseDto? data, string? message)
        {
            Success = success;
            Data = data;
            Message = message;
        }
    }
}
