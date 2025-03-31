using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IPasswordReminderService
    {
        Task SendRemindersAsync();
    }
}
