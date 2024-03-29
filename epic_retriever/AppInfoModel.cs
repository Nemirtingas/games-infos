﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace epic_retriever
{
    internal class DlcInfoModel
    {
        public string Name { get; set; }
        public string EntitlementId { get; set; }
        public string ItemId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public CatalogDataCatalogOfferElementOfferType? Type { get; set; }
    }

    internal class AppInfoModel
    {
        public string Name { get; set; }
        public string AppId { get; set; }
        public string Namespace { get; set; }
        public string ItemId { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Releases { get; set; }
        public List<DlcInfoModel> Dlcs { get; set; }
    }
}
