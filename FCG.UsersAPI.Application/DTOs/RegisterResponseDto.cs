using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.UsersAPI.Application.DTOs
{
    public class RegisterResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
