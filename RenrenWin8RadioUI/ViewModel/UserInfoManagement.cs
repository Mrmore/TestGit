using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;

namespace RenRenWin8Radio.ViewModel
{
    public class UserInfoManagement
    {
        private static readonly UserInfoManagement instance = new UserInfoManagement();
        public static UserInfoManagement Instance
        {
            get { return instance; }
        }

        private bool isReset = true;
        private bool IsReset
        {
            get
            {
                return isReset;
            }
            set
            {
                isReset = value;
            }
        }

        //初始化
        private async void Init()
        {
            await CreateXmlFile();
        }

        //初始化方法
        private UserInfoManagement()
        {
            Init();
        }


        private const string FileKey = "FileKey";
        private string folderName = @"Xml\xmlUserInfo";
        private string fileName = "UserInfo.xml";
        private string xmlDocumentRoot = "UserInfo";
        private string filePath = @"Xml\xmlUserInfo\UserInfo.xml";

        /// <summary>
        /// 选取Xml文件
        /// </summary>
        public async Task<XmlDocument> LoadXmlFile(string folder, string file)
        {
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(folder);
            StorageFile storageFile = await storageFolder.GetFileAsync(file);
            XmlLoadSettings loadSettings = new XmlLoadSettings();
            loadSettings.ProhibitDtd = false;
            loadSettings.ResolveExternals = false;
            return await XmlDocument.LoadFromFileAsync(storageFile, loadSettings);
        }

        /// <summary>
        /// 创建Xml文件
        /// </summary>
        public async Task CreateXmlFile()
        {
            try
            {
                //验证是不是有UserInfo.Xml这个文件
                StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(folderName, CreationCollisionOption.FailIfExists);
                StorageFile storageFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

                XmlDocument doc = new XmlDocument();
                XmlElement xdRoot = doc.CreateElement(xmlDocumentRoot);
                XmlElement xmlElement = doc.CreateElement("IsReset");
                xmlElement.InnerText = bool.TrueString;
                xdRoot.AppendChild(xmlElement);
                doc.AppendChild(xdRoot);
                await doc.SaveToFileAsync(storageFile);
            }
            catch (Exception)
            {
                
            }
        }

        /// <summary>
        /// 查找是不是看过
        /// </summary>
        /// <returns>默认为True为注销，False为登录，True为注销</returns>
        public async Task<bool> SelectXmlFile()
        {
            var xpath = "/UserInfo";
            XmlDocument doc = await LoadXmlFile(folderName, fileName);
            var nodeList = doc.SelectNodes(xpath);
            return System.Convert.ToBoolean(nodeList[0].SelectSingleNode("IsReset").InnerText);
        }

        public void EditXmlFile()
        {
            EditXml(false);
        }

        private async void EditXml(bool isFirstApp = true)
        {
            var xpath = "/UserInfo";
            XmlDocument doc = await LoadXmlFile(folderName, fileName);
            var nodeList = doc.SelectNodes(xpath);
            nodeList[0].SelectSingleNode("IsReset").InnerText = System.Convert.ToString(isFirstApp);
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
            StorageFile storageFile = await storageFolder.GetFileAsync(fileName);
            await doc.SaveToFileAsync(storageFile);
        }

        public async void Reset()
        {
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
            StorageFile storageFile = await storageFolder.GetFileAsync(fileName);
            await storageFile.DeleteAsync();
            await storageFolder.DeleteAsync();
        }

        public void ResetEditXmlFile()
        {
            EditXml();
        }
    }
}
