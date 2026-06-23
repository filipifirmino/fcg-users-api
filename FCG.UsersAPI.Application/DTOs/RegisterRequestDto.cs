using FCG.UsersAPI.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.UsersAPI.Application.DTOs
{
    public class RegisterRequestDto
    {
        public required string Name { get; set; }
        public required Email Email { get; set; }
        public required Password Password { get; set; }

        public bool IsValid()
            => !string.IsNullOrWhiteSpace(Name) && Email.IsValid() && Password.IsValid();
    }
}
