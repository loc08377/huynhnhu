using duAn1.Models;

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
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return null;
            }
        }
    }
}
