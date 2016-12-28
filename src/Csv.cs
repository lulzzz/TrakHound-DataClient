// Copyright (c) 2017 TrakHound Inc, All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;

namespace TrakHound.DataClient
{
    static class Csv
    {
        public static string ToCsv(object obj)
        {
            var l = new List<object>();

            foreach (var property in obj.GetType().GetProperties())
            {
                l.Add(property.GetValue(obj, null));
            }

            return string.Join(",", l);
        }

        public static T FromCsv<T>(string line)
        {
            var fields = line.Split(',');
            var properties = typeof(T).GetProperties();

            if (fields.Length >= properties.Length)
            {
                object obj = Activator.CreateInstance(typeof(T));

                for (int i = 0; i < properties.Length; i++)
                {
                    var p = properties[i];
                    if (p.CanWrite) p.SetValue(obj, Convert.ChangeType(fields[i], p.PropertyType), null);
                }
            }

            return default(T);
        }
    }
}
