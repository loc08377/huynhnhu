using duAn1.Models;

namespace duAn1.Repository
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public User UserByEmail(string email)
        {
            User user = _context.Users
                .Where(u => u.Email == email)
                .FirstOrDefault();

            return user;
        }

        public User GetUserById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }
    }
}
