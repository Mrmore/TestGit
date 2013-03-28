﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
//
//
// Use of this source code is subject to the terms of the Microsoft
// license agreement under which you licensed this source code.
// If you did not accept the terms of the license agreement,
// you are not authorized to use this source code.
// For the terms of the license, please see the license agreement
// signed by you and Microsoft.
// THE SOURCE CODE IS PROVIDED "AS IS", WITH NO WARRANTIES OR INDEMNITIES.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage;


namespace Microsoft.Phone.Applications.Common
{
    public static class ApplicationSettingHelper
    {
        /// <summary>
        /// Attempt to get a value from the application settings
        /// if not successful (no present, wrong type), returns provided default value
        /// </summary>
        /// <typeparam name="TValue">Expected type of the setting</typeparam>
        /// <param name="key">Name of the key</param>
        /// <param name="defaultValue">Default value to be returned if fails</param>
        /// <returns></returns>
        public static TValue TryGetValueWithDefault<TValue>(string key, TValue defaultValue)
        {
            TValue retval = defaultValue;
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(key))
            {
                object value = localSettings.Values[key];//IsolatedStorageSettings.ApplicationSettings[key];
                if (value is TValue)
                {
                    retval = (TValue) value;
                }
            }
            return retval;
        }


        /// <summary>
        /// Add key/value or Update existing key with new value to the application settings
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <param name="value">New or updated value</param>
        /// <returns></returns>
        public static bool AddOrUpdateValue(string key, Object value)
        {
            bool valueChanged = false;
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(key))
            {
                if (localSettings.Values[key] != value)
                { // set to the value if it is different
                    localSettings.Values[key] = value;
                    valueChanged = true;
                }
            }
            else
            { // key is not in dictionary, create it
                localSettings.Values.Add(key, value);
                valueChanged = true;
            }
            return valueChanged;
        }


        /// <summary>
        /// Explicit Save. Not needed as Save will automatically be called at appInstance exit
        /// </summary>
        public static void Save()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            //IsolatedStorageSettings.ApplicationSettings.Save();
        }
    }
}
