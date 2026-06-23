using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.UsersAPI.Application.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public int ExpiresIn { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
