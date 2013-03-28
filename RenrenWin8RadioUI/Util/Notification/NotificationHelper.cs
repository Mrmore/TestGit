using NotificationsExtensions.ToastContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace RenRenWin8Radio.Util
{
    public class NotificationHelper
    {
        /// <summary>
        /// Post弹出消息对话框系统APP图片
        /// </summary>
        /// <param name="title">主题</param>
        /// <param name="description">内容</param>
        /// <param name="notificationSound">声音(默认是成功的声音)</param>
        public static void DisplayTextTost(string title, string description, NotificationSound notificationSound = NotificationSound.Success)
        {
            IToastText02 toastContent = ToastContentFactory.CreateToastText02();
            toastContent.TextHeading.Text = title;
            toastContent.TextBodyWrap.Text = description;
            switch (notificationSound)
            {
                case NotificationSound.Success:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Reminder;
                        break;
                    }
                case NotificationSound.Failure:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Mail;
                        break;
                    }
                case NotificationSound.News:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Default;
                        break;
                    }
                default:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Default;
                        break;
                    }
            }
            ToastNotification toast = toastContent.CreateNotification();
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// Post弹出消息对话框可设置WEB的图片
        /// </summary>
        /// <param name="toastImageSrc">Web Uri</param>
        /// <param name="title">主题</param>
        /// <param name="description">内容</param>
        /// <param name="notificationSound">声音(默认是成功的声音)</param>
        public static void DisplayWebImageToast(string toastImageSrc, string title, string description, NotificationSound notificationSound = NotificationSound.Success)
        {
            IToastImageAndText02 toastContent = ToastContentFactory.CreateToastImageAndText02();
            toastContent.TextHeading.Text = title;
            toastContent.TextBodyWrap.Text = description;
            toastContent.Image.Src = toastImageSrc;
            toastContent.Image.Alt = toastImageSrc;
            switch (notificationSound)
            {
                case NotificationSound.Success:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Default;
                        break;
                    }
                case NotificationSound.Failure:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Mail;
                        break;
                    }
                case NotificationSound.News:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Reminder;
                        break;
                    }
                default:
                    {
                        toastContent.Audio.Content = ToastAudioContent.Default;
                        break;
                    }
            }
            ToastNotification toast = toastContent.CreateNotification();
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static void toast_Failed(ToastNotification sender, ToastFailedEventArgs args)
        {
            var errorCode = args.ErrorCode;
            return;
        }
    }
}
