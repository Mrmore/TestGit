using RenRenWin8Radio.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace RenrenWin8RadioUI.ViewModel
{
    public class UserSettingListSave
    {
        private ObservableCollection<UserSetting> userSettingOb = new ObservableCollection<UserSetting>();
        private const string UserSetting = "UserSetting";
        private IPropertySet dataSet = ApplicationData.Current.LocalSettings.Values;

        private static UserSettingListSave instance = new UserSettingListSave();
        public static UserSettingListSave Instance { get { return instance; } }

        /// <summary>
        /// 私有化构造函数
        /// </summary>
        private UserSettingListSave()
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

        //重置的方法
        public void Reset()
        {
            this.userSettingOb.Clear();
            dataSet.Remove(UserSetting);
        }

        /// <summary>
        /// 查找所有的用户设置信息
        /// </summary>
        /// <returns>返回所有的用户设置信息(默认电台ID，重力感应开关，光感应开关，音量)</returns>
        public ObservableCollection<UserSetting> UserList()
        {
            Restore();
            return userSettingOb;
        }

        //读取保存在硬盘的方法(反序列)
        private void Restore()
        {
            userSettingOb.Clear();
            try
            {
                if (dataSet.ContainsKey(UserSetting))
                {
                    string userSettingList = (string)dataSet[UserSetting];
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(userSettingList)))
                    {
                        DataContractSerializer deserializer = new DataContractSerializer(typeof(ObservableCollection<UserSetting>));
                        userSettingOb = (ObservableCollection<UserSetting>)deserializer.ReadObject(stream);
                    }
                }
            }
            catch (Exception)
            { }
        }

        //保存序列化的方法
        private void SaveData()
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<UserSetting>));
                    serializer.WriteObject(stream, userSettingOb);
                    stream.Position = 0;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        this.dataSet[UserSetting] = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 添加用户设置信息的方法
        /// </summary>
        /// <param name="userSetting"></param>
        public void AddXml(UserSetting userSetting)
        {
            userSettingOb.Add(userSetting);
            SaveData();
        }

        /// <summary>
        /// 修改用户设置信息的方法(对于单用户来说，应该一直调用这个方法)
        /// </summary>
        /// <param name="userSetting"></param>
        public void EditXml(UserSetting userSetting)
        {
            if (userSettingOb.Count > 0)
            {
                userSettingOb.Clear();
            }
            userSettingOb.Add(userSetting);
            SaveData();
        }
    }
}
