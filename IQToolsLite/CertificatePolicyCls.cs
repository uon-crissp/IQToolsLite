﻿using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace IQToolsLite
{
    class CertificatePolicyCls : System.Net.ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem)
        {
            return true;
        }
    }
}
