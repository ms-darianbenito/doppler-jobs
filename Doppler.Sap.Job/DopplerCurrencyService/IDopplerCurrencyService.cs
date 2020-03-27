﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.Sap.Job.Service.Dtos;

namespace Doppler.Sap.Job.Service.DopplerCurrencyService
{
    public interface IDopplerCurrencyService
    {
        public Task<IList<CurrencyDto>> GetCurrencyByCode();
    }
}