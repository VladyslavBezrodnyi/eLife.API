﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eLifeWEB.Models
{
    public class LiqPayP2P
    {
        // --- Обов'язкові параметри:
        
        public int version { get; set; }
        public string public_key { get; set; }
        public string phone { get; set; }
        public string action { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string description { get; set; }
        public string order_id { get; set; }
        //public string card { get; set; }
        //public int card_exp_month { get; set; }
        //public int card_exp_year { get; set; }
        //public string card_cvv { get; set; }
        public string receiver_card { get; set; }
        public string ip { get; set; }
        public string result_url { get; set; }
        //public string server_url { get; set; }
        /// <summary>
        /// Тестовий режим: 1 - так, 0 - ні. (в тестовому режимі гроші не знімаються)
        /// </summary>
        public int sandbox { get; set; }
    }
}