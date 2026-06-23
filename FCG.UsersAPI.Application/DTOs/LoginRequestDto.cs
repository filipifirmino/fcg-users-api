using FCG.UsersAPI.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.UsersAPI.Application.DTOs
{
    public class LoginRequestDto
    {
        public required Email Email { get; set; }
        public required Password Password { get; set; }

        public bool IsValid()
        {
            return Email.IsValid() && Password.IsValid();
        }
    }
}
