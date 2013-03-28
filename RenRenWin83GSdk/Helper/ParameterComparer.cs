using System;
using System.Collections.Generic;
using System.Net;
using RenRenAPI.Entity;

namespace RenRenAPI.Helper
{
    public class ParameterComparer : IComparer<RequestParameterEntity>
    {

        public int Compare(RequestParameterEntity x, RequestParameterEntity y)
        {
            if (x.Name.CompareTo(y.Name) > 0)
            {
                return 1;
            }
            else if (x.Name.CompareTo(y.Name) < 0)
            {
                return -1;
            }
            else if (x.Name.CompareTo(y.Name) == 0)
            {
                return 0;
            }
            return 0;
        }
    }
}
