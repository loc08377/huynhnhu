using duAn1.Models;
using duAn1.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace duAn1.Services
{
    public class UserService
    {
        private readonly Repository.UserRepository _userRepository;
        public UserService(Repository.UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User userByEmail(string email)
        {
            try
            {
                return _userRepository.UserByEmail(email);
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return null;
            }
        }
    }
}
