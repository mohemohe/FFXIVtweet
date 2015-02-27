using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Livet;

namespace FFXIVtweet.Models
{
    public class LoXIV : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        private readonly LoXIVsharp.LoXIV _loxiv;

        public LoXIV()
        {
            _FFXIVlog = new List<string>();
            _loxiv = new LoXIVsharp.LoXIV();
        }

        public void StartReadLogLoop()
        {
            var flag = false;

            while (true)
            {
                Thread.Sleep(100);

                var process = Process.GetProcessesByName("ffxiv");
                if (process.Length != 0)
                {
                    if (!flag)
                    {
                        _loxiv.SearchProcess();
                        _loxiv.SearchAddress();
                        flag = true;
                    }

                    var tmpLog = new List<string>();
                    var result = _loxiv.ReadPreformattedLog(out tmpLog);
                    if (result > 0)
                    {
                        FFXIVlog = new List<string>(tmpLog);
                    }
                }
                else
                {
                    flag = false;
                }
            }
        }

        #region FFXIVlog変更通知プロパティ
        private List<string> _FFXIVlog;

        public List<string> FFXIVlog
        {
            get
            {
                return _FFXIVlog;
            }
            set
            { 
                if (_FFXIVlog == value)
                    return;
                _FFXIVlog = value;
                RaisePropertyChanged();
            }
        }
        #endregion
    }
}
