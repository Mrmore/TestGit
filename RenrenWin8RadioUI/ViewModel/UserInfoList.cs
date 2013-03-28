using System;
using System.Net;
using System.IO;
using System.Collections.ObjectModel;
using RenRenWin8Radio.Model;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.Foundation.Collections;
using System.Diagnostics;

namespace RenRenWin8Radio.ViewModel
{
    public class UserInfoList
    {
        private ObservableCollection<UserInfo> userInfoOb = new ObservableCollection<UserInfo>();
        private string _USER_INFO_LIST_KEY = "USER_INFO_LIST_KEY";
        private IPropertySet _DataSet = ApplicationData.Current.LocalSettings.Values;

        private static UserInfoList _instance = new UserInfoList();
        public static UserInfoList Instance { get { return _instance; } }

        /// <summary>
        /// 私有化构造函数
        /// </summary>
        private UserInfoList()
        {
            Init();
        }

        /// <summary>
        /// Notice!!!!!
        /// Hi man, you should always call this method before use this class
        /// </summary>
        /// <returns></returns>
        private void Init()
        {
            Restore();
        }

        public void Reset()
        {
            this.userInfoOb.Clear();
            _DataSet.Remove(_USER_INFO_LIST_KEY);
        }

        /// <summary>
        /// 查找所有的用户信息
        /// </summary>
        /// <returns>返回所有的用户信息(用户名和密码)</returns>
        public ObservableCollection<UserInfo> UserList()
        {
            Restore();
            return userInfoOb;
        }

        private void Restore()
        {
            userInfoOb.Clear();
            try
            {
                if (_DataSet.ContainsKey(_USER_INFO_LIST_KEY))
                {
                    string infoList = (string)_DataSet[_USER_INFO_LIST_KEY];
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(infoList)))
                    {
                        DataContractSerializer deserializer = new DataContractSerializer(typeof(ObservableCollection<UserInfo>));
                        userInfoOb = (ObservableCollection<UserInfo>)deserializer.ReadObject(stream);
                    }
                }
            }
            catch (Exception)
            { }
        }

        private void SaveData()
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<UserInfo>));
                    serializer.WriteObject(stream, userInfoOb);
                    stream.Position = 0;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        _DataSet[_USER_INFO_LIST_KEY] = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 添加的方法
        /// </summary>
        /// <param name="userInfo"></param>
        public void AddXml(UserInfo userInfo)
        {
            userInfoOb.Add(userInfo);
            SaveData();
        }
    }
}
