﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace coinpanic_airdrop.Services
{
    public class NotificationHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }

        public void NotifyInvoicePaid(string invoice)
        {
            Clients.All.notifyInvoicePaid(invoice);
        }
    }
}