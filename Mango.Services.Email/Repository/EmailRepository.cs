using Mango.Services.Email.DbContexts;
using Mango.Services.Email.Messages;
using Mango.Services.Email.Models;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.Email.Repository
{
    public class EmailRepository : IEmailRepository
    {
        private readonly DbContextOptions<AppDbContext> _dbContext;

        public EmailRepository(DbContextOptions<AppDbContext> dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SendAndLogEmail(UpdatePaymentResultMessage message)
        {
            //TODO: Implement an email sender or call some third party library to send email
            EmailLog emailLog = new()
            {
                Email = message.Email,
                Log = $"OrderId-{message.OrderId} has been created successfully",
                EmailSent = DateTime.Now,
            };
            await using var _db = new AppDbContext(_dbContext);
            _db.EmailLogs.Add(emailLog);
            await _db.SaveChangesAsync();
        }
    }
}
