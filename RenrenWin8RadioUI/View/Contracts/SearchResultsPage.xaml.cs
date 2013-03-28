using RenRenWin8Radio;
using RenrenWin8RadioUI.DataModel;
using RenrenWin8RadioUI.ViewModel.ForUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “搜索合同”项模板在 http://go.microsoft.com/fwlink/?LinkId=234240 上提供

namespace RenrenWin8RadioUI.View.Contracts
{
    /// <summary>
    /// 此页显示全局搜索定向到此应用程序时的搜索结果。
    /// </summary>
    public sealed partial class SearchResultsPage : RenrenWin8RadioUI.Common.LayoutAwarePage
    {
        SearchVideoViewModel _searchVideoViewModel;

        public SearchResultsPage()
        {
            this.InitializeComponent();
            _searchVideoViewModel = new SearchVideoViewModel();
        }

        /// <summary>
        /// 使用在导航过程中传递的内容填充页。在从以前的会话
        /// 重新创建页时，也会提供任何已保存状态。
        /// </summary>
        /// <param name="navigationParameter">最初请求此页时传递给
        /// <see cref="Frame.Navigate(Type, Object)"/> 的参数值。
        /// </param>
        /// <param name="pageState">此页在以前会话期间保留的状态
        /// 字典。首次访问页面时为 null。</param>
        async protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            var queryText = navigationParameter as String;

            await _searchVideoViewModel.GetAllRadioData(queryText);

            this.DefaultViewModel["Results"] = this._searchVideoViewModel.ResultRadioDataList;

            // 通过视图模型沟通结果
            this.DefaultViewModel["QueryText"] = '\u201c' + queryText + '\u201d';

            // 确保找到结果
            object results;
            ICollection resultsCollection;
            if (this.DefaultViewModel.TryGetValue("Results", out results) &&
                (resultsCollection = results as ICollection) != null &&
                resultsCollection.Count != 0)
            {
                VisualStateManager.GoToState(this, "ResultsFound", true);
                return;
            }


            // 无搜索结果时显示信息性文本。
            VisualStateManager.GoToState(this, "NoResultsFound", true);


        }

        private void resultsGridView_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            RadioItem item = e.ClickedItem as RadioItem;
            if (item != null)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage), item.Id);
            }
        }
    }
}
