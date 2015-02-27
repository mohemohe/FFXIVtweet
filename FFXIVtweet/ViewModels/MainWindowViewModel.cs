using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using FFXIVtweet.Models;
using Rhinemaidens;

namespace FFXIVtweet.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        /* コマンド、プロパティの定義にはそれぞれ 
         * 
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *  
         * を使用してください。
         * 
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         * 
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         * 
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         * 
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         * 
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         * 
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */

        private LoXIV _loxiv;

        public async void Initialize()
        {
            _LogList = new List<string>();

            _loxiv = new LoXIV();

            var loxivListener = new PropertyChangedEventListener(_loxiv);
            loxivListener.RegisterHandler(LoXIVUpdateHandler);
            CompositeDisposable.Add(loxivListener);

            await Task.Run(() => _loxiv.StartReadLogLoop());
        }


        System.Globalization.CompareInfo ci =
            System.Globalization.CultureInfo.CurrentCulture.CompareInfo;

        private void LoXIVUpdateHandler(object sender, PropertyChangedEventArgs e)
        {
            var worker = sender as LoXIV;
            if (worker != null)
            {
                foreach (var log in worker.FFXIVlog)
                {
                    if (String.IsNullOrEmpty(Name) || String.IsNullOrEmpty(Prefix))
                    {
                        continue;
                    }

                    if (log.StartsWith(Name))
                    {
                        string prefix;
                        if (Prefix == "{{ss}}")
                        {
                            prefix = " ";
                        }
                        else
                        {
                            prefix = Prefix;
                        }

                        if (ci.IndexOf(log.Substring(Name.Length + 1), prefix, CompareOptions.IgnoreWidth) == 0)
                        {
                            var lorelei = new Lorelei(
                                "",
                                "",
                                "",
                                "");
                            try
                            {
                                lorelei.PostTweet(log.Substring(Name.Length + prefix.Length + 1));
                            }
                            catch { }
                        }
                    }
                }

                var dispatcher = Application.Current.Dispatcher;

                if (dispatcher.CheckAccess())
                {
                    LogList = worker.FFXIVlog;
                }
                else
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        LogList = worker.FFXIVlog;
                    }));
                }

            }
        }

        #region Name変更通知プロパティ
        private string _Name;

        public string Name
        {
            get
            { return _Name; }
            set
            { 
                if (_Name == value)
                    return;
                _Name = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Prefix変更通知プロパティ
        private string _Prefix;

        public string Prefix
        {
            get
            { return _Prefix; }
            set
            { 
                if (_Prefix == value)
                    return;
                _Prefix = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region DeferredLogList変更通知プロパティ
        private ListCollectionView _DeferredLogList;

        public ListCollectionView DeferredLogList
        {
            get
            { return _DeferredLogList; }
            set
            { 
                if (_DeferredLogList == value)
                    return;
                _DeferredLogList = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region LogList変更通知プロパティ
        private List<string> _LogList;

        public List<string> LogList
        {
            get
            { return _LogList; }
            set
            {
                if (value.Count == 0)
                    return;
                _LogList.AddRange(value);

                if (_LogList.Count > 1000)
                {
                    _LogList.RemoveRange(0, _LogList.Count - 1000);
                }

                DeferredLogList = new ListCollectionView(_LogList);
            }
        }
        #endregion
    }
}
