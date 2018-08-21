using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coinpanic_airdrop.Services
{
    /// <summary>
    /// Manages payments over the lightning network
    /// </summary>
    public interface ILightningPayments
    {
        string Test();

        void RecordNodeWithdraw(string node);

        bool IsNodeBanned(string node, out string message);

        bool TryWithdrawal(string payreq);
    }
}
