using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharingApp.Helpers.Mail
{
    public interface IMailHelper
    {
        void SendMail(InputEmailMessage model);
    }
}
