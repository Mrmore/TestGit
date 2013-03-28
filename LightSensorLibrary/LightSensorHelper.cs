using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace LightSensorLibrary
{
    public class LightSensorHelper : IDisposable
    {
        private static volatile LightSensorHelper lightSensorHelper;

        private static Object syncRoot = new Object();

        public Action<LightSensorReading> IlluminanceInLuxChange = null;
        public Func<string,LightSensorReading> IlluminanceInLuxChangeRe = null;

        private float measuredValue = 50;
        //设置感光的差值 默认是50
        public float MeasuredValue
        {
            get
            {
                return measuredValue;
            }
            set
            {
                measuredValue = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private LightSensor lightSensor;

        /// <summary>
        /// 
        /// </summary>
        public static LightSensorHelper Instance
        {
            get
            {
                if (lightSensorHelper == null)
                {
                    lock (syncRoot)
                    {
                        if (lightSensorHelper == null)
                        {
                            lightSensorHelper = new LightSensorHelper();
                        }
                    }
                }
                return lightSensorHelper;
            }
        }

        private LightSensorHelper()
        {
            lightSensor = LightSensor.GetDefault();
            //如果光感应设备没有或者损毁会 == null
            //如果是Wp7 会有状态属性，可以判断出当前设备的状态
            if (lightSensor == null)
            {
                return;
            }
            else
            {
                //For Mat
                uint minReportInterval = lightSensor.MinimumReportInterval;
                lightSensor.ReportInterval = minReportInterval > 100 ? minReportInterval : 100;
                lightSensor.ReadingChanged += new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(ReadingChanged);
            }
        }

        //async
        private void ReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
        {
            //await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //{
                LightSensorReading reading = args.Reading;
                if (reading.IlluminanceInLux <= MeasuredValue)
                {
                    if (IlluminanceInLuxChange != null)
                    {
                        this.IlluminanceInLuxChange(reading);
                    }
                }
            //});
        }

        public void Dispose()
        {
            IlluminanceInLuxChangeRe = null;
            //如果lightSensor == null 表示设备没有初始化成功
            if (lightSensor != null)
            {
                lightSensor.ReportInterval = 0;
                lightSensor.ReadingChanged -= new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(ReadingChanged);
            }
        }
    }
}
